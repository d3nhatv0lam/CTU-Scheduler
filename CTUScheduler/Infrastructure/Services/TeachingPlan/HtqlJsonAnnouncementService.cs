using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Academic;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Sites.CTU.Helpers;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.TeachingPlan;

public class HtqlJsonAnnouncementService : ISchoolAnnouncementService
{
    private const string ApiUrl = "https://htql.ctu.edu.vn/htql/thongbao.php";
    private readonly HttpClient _httpClient;
    private readonly ILogger<HtqlJsonAnnouncementService> _logger;

    public HtqlJsonAnnouncementService(HttpClient httpClient, ILogger<HtqlJsonAnnouncementService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
    }

    public const string CaptchaErrorMessage = "Kẹt Captcha, hãy bật VPN hoặc đổi mạng";
    public const string CaptchaErrorCode = "TeachingPlan.CaptchaBlocked";

    public virtual async Task<OperationResult<IReadOnlyList<SchoolAnnouncement>>> FetchAnnouncementsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
            request.Headers.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            request.Headers.Add("Referer", "https://accounts.ctu.edu.vn/");
            request.Headers.Add("Origin", "https://accounts.ctu.edu.vn");
            request.Headers.Add("Accept", "application/json, text/javascript, */*; q=0.01");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (IsCaptchaResponse(json))
            {
                _logger.LogWarning("Phát hiện form Captcha CTU trên endpoint JSON thongbao.php");
                return OperationResult<IReadOnlyList<SchoolAnnouncement>>.Failed(
                    new OperationError(CaptchaErrorCode, CaptchaErrorMessage),
                    OperationFailureReason.Network);
            }

            var data = ParseAnnouncementsFromJson(json);

            return OperationResult<IReadOnlyList<SchoolAnnouncement>>.Success(data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return OperationResult<IReadOnlyList<SchoolAnnouncement>>.Failed("Tác vụ lấy thông báo JSON đã bị hủy.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông báo từ HTQL thongbao.php");
            return OperationResult<IReadOnlyList<SchoolAnnouncement>>.FromException(ex, "Không thể lấy thông báo từ nguồn JSON HTQL.");
        }
    }

    public static bool IsCaptchaResponse(string? responseContent) =>
        CtuCaptchaDetector.IsCaptchaResponse(responseContent);

    public static IReadOnlyList<SchoolAnnouncement> ParseAnnouncementsFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<SchoolAnnouncement>>(json) ?? [];
    }
}
