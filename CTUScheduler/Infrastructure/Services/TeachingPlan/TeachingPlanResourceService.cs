using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Models.TeachingPlan;
using HtmlAgilityPack;
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

    private readonly HttpClient _httpClient = new();

    public async Task<IReadOnlyList<NotificationItem>> GetNotificationsAsync()
    {
        var html = await _httpClient.GetStringAsync(NotificationsUrl);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var links = doc.DocumentNode.SelectNodes("//a[@href]") ?? new HtmlNodeCollection(null);
        var items = new List<NotificationItem>();

        foreach (var link in links)
        {
            var href = link.GetAttributeValue("href", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(href) || !PdfHrefRegex.IsMatch(href)) continue;

            var title = HtmlEntity.DeEntitize(link.InnerText ?? string.Empty).Trim();
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

        var fileName = $"teaching-plan-{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        var bytes = await _httpClient.GetByteArrayAsync(pdfUrl);
        await File.WriteAllBytesAsync(filePath, bytes);

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
            var firstPage = document.GetPage(1);
            var text = firstPage.Text ?? string.Empty;

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
            foreach (var line in lines.Select(l => l.Trim()))
            {
                var rangeMatch = DateRangeRegex.Match(line);
                if (!rangeMatch.Success) continue;

                if (TryParseDate(rangeMatch.Groups[1].Value, out var start) &&
                    TryParseDate(rangeMatch.Groups[2].Value, out var end))
                {
                    data.RegistrationTimeline.Add(new RegistrationTimelineItem
                    {
                        Description = line,
                        StartDate = start,
                        EndDate = end
                    });
                }
            }

            return data;
        });
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

