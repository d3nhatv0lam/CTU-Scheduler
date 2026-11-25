using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Academic;
using CTUScheduler.Core.Models.Shared.Results;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.TeachingPlan;

public class HtqlHtmlAnnouncementService : ISchoolAnnouncementService
{
    private const string LoginUrl = "https://htql.ctu.edu.vn/login.php";
    private static readonly Uri BaseUri = new("https://htql.ctu.edu.vn/");

    private readonly HttpClient _httpClient;
    private readonly ILogger<HtqlHtmlAnnouncementService> _logger;

    public HtqlHtmlAnnouncementService(HttpClient httpClient, ILogger<HtqlHtmlAnnouncementService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
    }

    public const string CaptchaXPath = "//form[@action='/captcha_resp'] | //input[@name='captcha_resp_txt'] | //img[@src='/captcha.gif']";
    public const string CaptchaErrorMessage = "Kẹt Captcha, hãy bật VPN hoặc đổi mạng";
    public const string CaptchaErrorCode = "TeachingPlan.CaptchaBlocked";

    public virtual async Task<OperationResult<IReadOnlyList<SchoolAnnouncement>>> FetchAnnouncementsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, LoginUrl);
            request.Headers.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            if (IsCaptchaHtml(html))
            {
                _logger.LogWarning("Phát hiện form Captcha CTU trên trang bảng tin login.php");
                return OperationResult<IReadOnlyList<SchoolAnnouncement>>.Failed(
                    new OperationError(CaptchaErrorCode, CaptchaErrorMessage),
                    OperationFailureReason.Network);
            }

            var announcements = ParseAnnouncementsFromHtml(html);
            return OperationResult<IReadOnlyList<SchoolAnnouncement>>.Success(announcements);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return OperationResult<IReadOnlyList<SchoolAnnouncement>>.Failed("Tác vụ lấy thông báo HTML đã bị hủy.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông báo từ HTQL login.php");
            return OperationResult<IReadOnlyList<SchoolAnnouncement>>.FromException(ex, "Không thể lấy thông báo từ bảng tin HTQL.");
        }
    }

    public static bool IsCaptchaPage(HtmlDocument? doc)
    {
        if (doc?.DocumentNode is null)
            return false;

        return doc.DocumentNode.SelectSingleNode(CaptchaXPath) is not null;
    }

    public static bool IsCaptchaHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return false;

        if (html.Contains("/captcha_resp", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("/captcha.gif", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return IsCaptchaPage(doc);
    }

    public static IReadOnlyList<SchoolAnnouncement> ParseAnnouncementsFromHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var nodes = doc.DocumentNode.SelectNodes("//td[@id='module-thongbao']//ul[@class='thongbao']//li/a");
        if (nodes is null || nodes.Count == 0)
            return [];

        var list = new List<SchoolAnnouncement>(nodes.Count);
        foreach (var node in nodes)
        {
            var title = node.InnerText?.Trim();
            var href = node.GetAttributeValue("href", string.Empty)?.Trim();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(href) || href == "#")
                continue;

            var absoluteUrl = Uri.TryCreate(BaseUri, href, out var uri) ? uri.ToString() : href;
            list.Add(new SchoolAnnouncement(title, absoluteUrl));
        }

        return list;
    }
}
