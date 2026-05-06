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
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using UglyToad.PdfPig;

namespace CTUScheduler.Infrastructure.Services.TeachingPlan;

public class TeachingPlanResourceService : ITeachingPlanResourceService
{
    private const string NotificationsUrl = "https://htql.ctu.edu.vn/";
    private static readonly Regex PdfHrefRegex = new("\\.pdf(\\?|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SemesterRegex = new(@"h\s*ọc\s*k\s*ỳ\s*(\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SchoolYearRegex = new(@"n\s*ăm\s*h\s*ọc\s*(\d{4})\s*[-–]\s*(\d{4})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DateRangeRegex = new(@"(\d{1,2}/\d{1,2}/\d{4})\s*[-–]\s*(\d{1,2}/\d{1,2}/\d{4})",
        RegexOptions.Compiled);
    private static readonly Regex SingleDateRegex = new(@"(\d{1,2}/\d{1,2}/\d{4})", RegexOptions.Compiled);
    private static readonly Regex ItemStartRegex = new(@"\b\d{1,2}\s+(?=\p{L})", RegexOptions.Compiled);

    private readonly IWebDriverService _webDriverService;
    private readonly ILogger<TeachingPlanResourceService> _logger;

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
        IWebDriverService webDriverService,
        ILogger<TeachingPlanResourceService> logger)
    {
        _webDriverService = webDriverService ?? throw new ArgumentNullException(nameof(webDriverService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<NotificationItem>> GetNotificationsAsync()
    {
        _logger.LogInformation("TeachingPlan: opening notifications page {Url}", NotificationsUrl);
        await _webDriverService.InitBrowserAsync();

        await using var tab = await _webDriverService.CreateTabAsync();
        var page = tab.NativePage;

        var response = await page.GotoAsync(NotificationsUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        _logger.LogInformation(
            "TeachingPlan: navigated to {Url} (status={Status})",
            response?.Url ?? page.Url,
            response?.Status);

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

        _logger.LogInformation(
            "TeachingPlan: found {Count} PDF link(s)",
            items.Count);

        return items;
    }

    public async Task<string> DownloadPdfAsync(string pdfUrl)
    {
        if (string.IsNullOrWhiteSpace(pdfUrl)) return string.Empty;

        var fileName = $"teaching-plan-{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        _logger.LogInformation("TeachingPlan: downloading PDF {Url}", pdfUrl);
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
            _logger.LogInformation(
                "TeachingPlan: saved PDF via download to {Path} ({Bytes} bytes)",
                filePath,
                downloadedBytes);

            if (downloadedBytes >= 1024)
            {
                return filePath;
            }

            _logger.LogWarning("TeachingPlan: download file too small, using fetch fallback");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("TeachingPlan: download event not triggered ({Message}), using fetch fallback", ex.Message);
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
            _logger.LogWarning("TeachingPlan: fetch download failed (no result)");
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
            _logger.LogWarning(
                "TeachingPlan: fetch not a PDF (status={Status}, content-type={ContentType}, bytes={Bytes})",
                fetchResult.Status,
                fetchResult.ContentType,
                fetchedBytes.Length);
            return string.Empty;
        }

        await File.WriteAllBytesAsync(filePath, fetchedBytes);
        _logger.LogInformation(
            "TeachingPlan: saved PDF via fetch to {Path} ({Bytes} bytes, status={Status})",
            filePath,
            fetchedBytes.Length,
            fetchResult.Status);

        return filePath;
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
            var text = string.Join("\n", document.GetPages().Select(p => p.Text ?? string.Empty));

            var data = new TeachingPlanData();

            var semesterMatch = SemesterRegex.Match(text);
            if (semesterMatch.Success && int.TryParse(semesterMatch.Groups[1].Value, out var semester))
            {
                data.Semester = semester;
            }

            var yearMatch = SchoolYearRegex.Match(text);
            if (yearMatch.Success)
            {
                data.SchoolYear = $"{yearMatch.Groups[1].Value}-{yearMatch.Groups[2].Value}";
            }

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines.Select(l => l.Trim()).Where(l => l.Length > 0))
            {
                var rangeMatches = DateRangeRegex.Matches(line);
                if (rangeMatches.Count > 0)
                {
                    foreach (Match match in rangeMatches)
                    {
                        if (TryParseDate(match.Groups[1].Value, out var start) &&
                            TryParseDate(match.Groups[2].Value, out var end))
                        {
                            data.RegistrationTimeline.Add(new RegistrationTimelineItem
                            {
                                Description = BuildDescription(line, match.Index, match.Length),
                                StartDate = start,
                                EndDate = end
                            });
                        }
                    }

                    continue;
                }

                var singleMatches = SingleDateRegex.Matches(line);
                if (singleMatches.Count == 1 && TryParseDate(singleMatches[0].Value, out var singleDate))
                {
                    var match = singleMatches[0];
                    data.RegistrationTimeline.Add(new RegistrationTimelineItem
                    {
                        Description = BuildDescription(line, match.Index, match.Length),
                        StartDate = singleDate,
                        EndDate = singleDate
                    });
                }
            }

            return data;
        });
    }

    private static string BuildDescription(string line, int matchIndex, int matchLength)
    {
        if (string.IsNullOrWhiteSpace(line)) return string.Empty;

        var segment = TryExtractItemSegment(line, matchIndex) ?? line;
        var normalized = NormalizeWhitespace(segment);

        if (normalized.Length > 240)
        {
            normalized = normalized.Substring(0, 240).Trim() + " ...";
        }

        return normalized;
    }

    private static string? TryExtractItemSegment(string line, int matchIndex)
    {
        var matches = ItemStartRegex.Matches(line);
        if (matches.Count == 0) return null;

        var startIndex = -1;
        var endIndex = line.Length;

        foreach (Match match in matches)
        {
            if (match.Index <= matchIndex)
            {
                startIndex = match.Index;
                continue;
            }

            endIndex = match.Index;
            break;
        }

        if (startIndex < 0) return null;

        var length = Math.Max(0, endIndex - startIndex);
        if (length == 0) return null;

        return line.Substring(startIndex, length);
    }

    private static string NormalizeWhitespace(string input)
    {
        return Regex.Replace(input, "\\s+", " ").Trim();
    }

    private static bool TryParseDate(string value, out DateTimeOffset? date)
    {
        if (DateTime.TryParseExact(value, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ||
            DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
        {
            date = new DateTimeOffset(dt);
            return true;
        }

        date = null;
        return false;
    }
}
