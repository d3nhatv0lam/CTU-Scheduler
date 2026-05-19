using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Models.TeachingPlan;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using CTUScheduler.Presentation.Shared.Controls.Timeline;
using Microsoft.Playwright;
using UglyToad.PdfPig;
using System.Security.Cryptography;
using System.Text;

namespace CTUScheduler.Infrastructure.Services.TeachingPlan;

public class TeachingPlanResourceService : ITeachingPlanResourceService
{
    private const string NotificationsUrl = "https://htql.ctu.edu.vn/";
    private const string TeachingPlanCacheFolder = "CTUScheduler\\TeachingPlans";
    private static readonly Regex PdfHrefRegex = new("\\.pdf(\\?|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DateRangeRegex = new(@"(\d{1,2}/\d{1,2}/\d{4})\s*[-–]\s*(\d{1,2}/\d{1,2}/\d{4})",
        RegexOptions.Compiled);
    private static readonly Regex SingleDateRegex = new(@"(\d{1,2}/\d{1,2}/\d{4})", RegexOptions.Compiled);

    private readonly IWebDriverService _webDriverService;

    private sealed class AnchorInfo
    {
        public string? Href { get; set; }
        public string? Text { get; set; }
    }

    private sealed class FetchResult
    {
        public int Status { get; set; }
        public string? ContentType { get; set; }
        public string? Base64 { get; set; }
    }

    public TeachingPlanResourceService(
        IWebDriverService webDriverService)
    {
        _webDriverService = webDriverService ?? throw new ArgumentNullException(nameof(webDriverService));
    }

    public async Task<IReadOnlyList<NotificationItem>> GetNotificationsAsync()
    {
        await _webDriverService.InitBrowserAsync();

        await using var tab = await _webDriverService.CreateTabAsync();
        var page = tab.NativePage;

        await page.GotoAsync(NotificationsUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var anchors = await page.EvalOnSelectorAllAsync<AnchorInfo[]>(
            "a[href]",
            "els => els.map(e => ({ href: e.getAttribute('href'), text: e.textContent }))");

        var items = new List<NotificationItem>();

        foreach (var anchor in anchors ?? Array.Empty<AnchorInfo>())
        {
            var href = anchor.Href?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(href) || !PdfHrefRegex.IsMatch(href)) continue;

            var title = (anchor.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                title = Path.GetFileName(href);
            }

            var pdfUrl = new Uri(new Uri(NotificationsUrl), href).ToString();
            items.Add(new NotificationItem { Title = title, PdfUrl = pdfUrl });
        }

        return items;
    }

    public async Task<string> DownloadPdfAsync(string pdfUrl)
    {
        if (string.IsNullOrWhiteSpace(pdfUrl)) return string.Empty;

        var filePath = GetCachedPdfPath(pdfUrl);
        if (File.Exists(filePath))
        {
            var existingBytes = new FileInfo(filePath).Length;
            if (existingBytes >= 1024)
            {
                return filePath;
            }
        }

        await _webDriverService.InitBrowserAsync();
        await using var tab = await _webDriverService.CreateTabAsync();
        var page = tab.NativePage;

        try
        {
            var download = await page.RunAndWaitForDownloadAsync(
                () => page.GotoAsync(pdfUrl),
                new PageRunAndWaitForDownloadOptions { Timeout = 5000 });
            await download.SaveAsAsync(filePath);

            var downloadedBytes = new FileInfo(filePath).Length;
            if (downloadedBytes >= 1024)
            {
                return filePath;
            }
        }
        catch (Exception)
        {
            // fall back to fetch
        }

        var fetchResult = await page.EvaluateAsync<FetchResult>(@"async url => {
            const res = await fetch(url, { credentials: 'include' });
            const buf = await res.arrayBuffer();
            const bytes = new Uint8Array(buf);
            let binary = '';
            for (let i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i]);
            return {
                status: res.status,
                contentType: res.headers.get('content-type'),
                base64: btoa(binary)
            };
        }", pdfUrl);

        if (fetchResult is null)
        {
            return string.Empty;
        }

        var fetchedBytes = string.IsNullOrWhiteSpace(fetchResult.Base64)
            ? Array.Empty<byte>()
            : Convert.FromBase64String(fetchResult.Base64);

        if (fetchResult.Status is < 200 or >= 300 ||
            string.IsNullOrWhiteSpace(fetchResult.ContentType) ||
            !fetchResult.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase) ||
            fetchedBytes.Length < 1024)
        {
            return string.Empty;
        }

        await File.WriteAllBytesAsync(filePath, fetchedBytes);
        return filePath;
    }

    private static string GetCachedPdfPath(string pdfUrl)
    {
        var cacheRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            TeachingPlanCacheFolder);
        Directory.CreateDirectory(cacheRoot);

        var hash = ComputeSha256(pdfUrl);
        var fileName = $"teaching-plan-{hash}.pdf";
        return Path.Combine(cacheRoot, fileName);
    }

    private static string ComputeSha256(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hashBytes = SHA256.HashData(bytes);
        var builder = new StringBuilder(hashBytes.Length * 2);
        foreach (var b in hashBytes)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }

    public async Task<TeachingPlanData> ExtractTeachingPlanAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return new TeachingPlanData();
        }

        return await Task.Run(() =>
        {
            using var document = PdfDocument.Open(filePath);
            var text = string.Join(" ", document.GetPages().Select(p => p.Text ?? string.Empty));
            var data = new TeachingPlanData();

            var startTag = "NỘI DUNG CÔNG VIỆC THỜI GIAN THỰC HIỆN";
            var endTag = "Lưu ý:";
            
            var startIndex = text.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
            var endIndex = text.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);

            if (startIndex == -1) return data;
            if (endIndex == -1) endIndex = text.Length;

            var tableContent = text.Substring(startIndex + startTag.Length, endIndex - (startIndex + startTag.Length)).Trim();
            
            var rowRegex = new Regex(@"(?<id>\d{1,2})\s+(?<content>.+?)\s+(?<date>\d{1,2}/\d{1,2}/\d{4}(?:\s*[-–]\s*\d{1,2}/\d{1,2}/\d{4})?)", RegexOptions.Singleline);
            
            var matches = rowRegex.Matches(tableContent);
            
            foreach (Match match in matches)
            {
                var content = match.Groups["content"].Value;
                var dateStr = match.Groups["date"].Value;

                var rangeMatches = DateRangeRegex.Matches(dateStr);
                if (rangeMatches.Count > 0)
                {
                    if (TryParseDate(rangeMatches[0].Groups[1].Value, out var start) &&
                        TryParseDate(rangeMatches[0].Groups[2].Value, out var end))
                    {
                        data.RegistrationTimeline.Add(new TimelineNode(
                            NormalizeWhitespace(content),
                            start,
                            end));
                    }
                }
                else
                {
                    var singleMatches = SingleDateRegex.Matches(dateStr);
                    if (singleMatches.Count > 0 && TryParseDate(singleMatches[0].Groups[1].Value, out var singleDate))
                    {
                        data.RegistrationTimeline.Add(new TimelineNode(
                            NormalizeWhitespace(content),
                            singleDate,
                            singleDate));
                    }
                }
            }

            return data;
        });
    }

    private static string NormalizeWhitespace(string input)
    {
        return Regex.Replace(input, "\\s+", " ").Trim();
    }

    private static bool TryParseDate(string value, out DateOnly date)
    {
        if (DateOnly.TryParseExact(value, new[] { "d/M/yyyy", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            date = dt;
            return true;
        }

        date = default;
        return false;
    }
}
