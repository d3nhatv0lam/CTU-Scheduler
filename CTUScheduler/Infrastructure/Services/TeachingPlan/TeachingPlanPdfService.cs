using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Core.Models.TeachingPlan;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace CTUScheduler.Infrastructure.Services.TeachingPlan;

public partial class TeachingPlanPdfService : ITeachingPlanPdfService
{
    private const string CacheSubFolder = "TeachingPlans";

    [GeneratedRegex(@"(?<hour>\d{1,2})\s*giờ\s*(?<minute>\d{2})\s*(?:ngày)?\s*(?<day>\d{1,2})\s*tháng\s*(?<month>\d{1,2})\s*năm\s*(?<year>\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex ClosingPattern1Regex();

    [GeneratedRegex(@"(?<hour>\d{1,2})[h:g](?<minute>\d{2})\s*(?:ngày\s+)?(?<day>\d{1,2})/(?<month>\d{1,2})/(?<year>\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex ClosingPattern2Regex();

    [GeneratedRegex(@"diễn\s+ra\s+từ\s+(?:ngày\s+)?(?<start>\d{1,2}/\d{1,2}/\d{4})\s+đến\s+(?:ngày\s+)?(?<end>\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex SemesterRegex();

    [GeneratedRegex(@"(?<start_time>\d{1,2}[h:g]\d{2}|\d{1,2}:\d{2})?\s*(?:ngày\s+)?(?<start_date>\d{1,2}/\d{1,2}/\d{4})\s*(?:đến|[-–])\s*(?<end_time>\d{1,2}[h:g]\d{2}|\d{1,2}:\d{2})?\s*(?:ngày\s+)?(?<end_date>\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex ComplexRangeRegex();

    [GeneratedRegex(@"(?<time>\d{1,2}[h:g]\d{2}|\d{1,2}:\d{2})?\s*(?:ngày\s+)?(?<date>\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex SingleDateDetailRegex();

    [GeneratedRegex(@"(?<id>\d{1,2})\s+(?<content>.+?)\s+(?<date>(?:(?:\d{1,2}[h:g]\d{2}|\d{1,2}:\d{2})\s+(?:ngày\s+)?)?\d{1,2}/\d{1,2}/\d{4}(?:\s*(?:đến|[-–])\s*(?:(?:\d{1,2}[h:g]\d{2}|\d{1,2}:\d{2})\s+(?:ngày\s+)?)?\d{1,2}/\d{1,2}/\d{4})?)", RegexOptions.Singleline)]
    private static partial Regex RowRegex();

    [GeneratedRegex(@"(?<time>\d{2}:\d{2})\s+ngày\s+(?<date>\d{1,2}/\d{1,2}/\d{4})\s+(?<group>\d)")]
    private static partial Regex SubGroupRegex();

    [GeneratedRegex(@"(\d{1,2})[h:g](\d{2})|(\d{1,2}):(\d{2})")]
    private static partial Regex TimeRegex();

    private readonly HttpClient _httpClient;
    private readonly ILogger<TeachingPlanPdfService> _logger;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);

    public TeachingPlanPdfService(
        HttpClient httpClient,
        ILogger<TeachingPlanPdfService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperationResult<string>> DownloadPdfAsync(
        string pdfUrl,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(pdfUrl))
        {
            return OperationResult<string>.Failed("Đường dẫn PDF tải về không hợp lệ.", kind: OperationFailureReason.System);
        }

        var filePath = GetCachedPdfPath(pdfUrl);
        var finalTimeout = timeout ?? _defaultTimeout;

        using var timeoutCts = new CancellationTokenSource(finalTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // --- CƠ CHẾ CACHE THÔNG MINH 2 TẦNG ---
        try
        {
            _logger.LogInformation("Checking remote PDF headers via HTTP HEAD for {Url}", pdfUrl);

            using var headRequest = new HttpRequestMessage(HttpMethod.Head, pdfUrl);
            headRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            using var headResponse = await _httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
            if (headResponse.IsSuccessStatusCode)
            {
                var serverLength = headResponse.Content.Headers.ContentLength;
                var serverLastModified = headResponse.Content.Headers.LastModified;

                if (File.Exists(filePath))
                {
                    var localInfo = new FileInfo(filePath);

                    // 1. Kiểm tra kích thước file khớp nhau
                    if (serverLength.HasValue && localInfo.Length == serverLength.Value)
                    {
                        // 2. Kiểm tra thời gian sửa đổi (nếu server có trả về Last-Modified)
                        if (!serverLastModified.HasValue || localInfo.LastWriteTimeUtc >= serverLastModified.Value.UtcDateTime)
                        {
                            _logger.LogInformation("PDF cache hit and validated via HTTP HEAD. Reusing cached file at {Path}", filePath);
                            return OperationResult<string>.Success(filePath);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("HTTP HEAD cache validation request timed out after {Timeout}s. Falling back to local cache.", finalTimeout.TotalSeconds);
            if (File.Exists(filePath) && new FileInfo(filePath).Length >= 1024)
            {
                return OperationResult<string>.Success(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to perform HTTP HEAD caching check on {Url}. Falling back to local basic cache validation.", pdfUrl);
            
            if (File.Exists(filePath))
            {
                var existingBytes = new FileInfo(filePath).Length;
                if (existingBytes >= 1024)
                {
                    _logger.LogInformation("Using cached PDF (fallback check) at {Path}", filePath);
                    return OperationResult<string>.Success(filePath);
                }
            }
        }

        // --- TIẾN HÀNH TẢI FILE NẾU CACHE CHƯA VALID/CHƯA TỒN TẠI ---
        try
        {
            _logger.LogInformation("Downloading PDF teaching plan from {Url}", pdfUrl);

            using var request = new HttpRequestMessage(HttpMethod.Get, pdfUrl);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(linkedCts.Token);
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream, 81920, linkedCts.Token);

            _logger.LogInformation("PDF downloaded and cached at {Path}", filePath);
            return OperationResult<string>.Success(filePath);
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return OperationResult<string>.Failed("Yêu cầu tải PDF đã bị hủy bởi người dùng.", kind: OperationFailureReason.System);
            }

            _logger.LogWarning("Download PDF request timed out after {Timeout}s", finalTimeout.TotalSeconds);
            return OperationResult<string>.Failed($"Yêu cầu tải file PDF bị quá thời gian chờ (Timeout {finalTimeout.TotalSeconds} giây).", kind: OperationFailureReason.Network);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error downloading PDF from {Url}", pdfUrl);
            return OperationResult<string>.Failed($"Lỗi kết nối mạng khi tải PDF: {ex.Message}", kind: OperationFailureReason.Network);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "System error downloading PDF from {Url}", pdfUrl);
            return OperationResult<string>.FromException(ex, "Không thể lưu hoặc ghi file PDF kế hoạch giảng dạy.");
        }
    }

    public string GetCachedPdfPath(string pdfUrl)
    {
        var cacheRoot = Path.Combine(AppConstants.Paths.BaseLocalPath, CacheSubFolder);
        Directory.CreateDirectory(cacheRoot);
        return Path.Combine(cacheRoot, ComputeSha256(pdfUrl) + ".pdf");
    }

    private static string ComputeSha256(string value)
    {
        int maxByteCount = Encoding.UTF8.GetByteCount(value);
        Span<byte> sourceBytes = maxByteCount <= 512 ? stackalloc byte[512] : new byte[maxByteCount];
        int written = Encoding.UTF8.GetBytes(value, sourceBytes);
        
        Span<byte> hashBytes = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(sourceBytes[..written], hashBytes);
        
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public async Task<DateTime?> ExtractClosingNoticeDateTimeAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        return await Task.Run<DateTime?>(() =>
        {
            try
            {
                using var document = PdfDocument.Open(filePath);
                _logger.LogInformation("Analyzing closing notice PDF (streaming)...");

                var patterns = new[]
                {
                    ClosingPattern1Regex(),
                    ClosingPattern2Regex()
                };

                // Duyệt cuốn chiếu từng trang để dọn dẹp bộ nhớ trang tức thì và cho phép thoát sớm
                foreach (var page in document.GetPages())
                {
                    var pageText = page.Text ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(pageText)) continue;

                    foreach (var regex in patterns)
                    {
                        var match = regex.Match(pageText);
                        if (match.Success)
                        {
                            var hour = int.Parse(match.Groups["hour"].Value);
                            var minute = int.Parse(match.Groups["minute"].Value);
                            var day = int.Parse(match.Groups["day"].Value);
                            var month = int.Parse(match.Groups["month"].Value);
                            var year = int.Parse(match.Groups["year"].Value);

                            return new DateTime(year, month, day, hour, minute, 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract closing notice date time from PDF at {Path}", filePath);
            }
            return null;
        });
    }

    public async Task<TeachingPlanData> ExtractTeachingPlanAsync(string filePath, DateTime? preciseClosingDateTime = null)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return new TeachingPlanData();
        }

        return await Task.Run(() =>
        {
            try
            {
                using var document = PdfDocument.Open(filePath);
                
                // Ước lượng kích thước StringBuilder để hạn chế tối đa việc tái cấp phát mảng ký tự
                var pageCount = document.NumberOfPages;
                var fullTextBuilder = new StringBuilder(pageCount * 1000);
                foreach (var page in document.GetPages())
                {
                    fullTextBuilder.AppendLine(page.Text ?? string.Empty);
                }
                var fullText = fullTextBuilder.ToString();

                // ngày bắt đầu/kết thúc học kỳ
                var (semesterStartDate, semesterEndDate) = ParseSemesterDates(fullText);

                // Bóc tách các mốc thời gian chính (Bảng 1)
                var timelineNodes = ParseTimelineNodes(
                    fullText, 
                    semesterStartDate, 
                    semesterEndDate, 
                    preciseClosingDateTime, 
                    out var fallbackAdjustmentEndDateTime);

                // ngày đóng cổng cuối cùng để dùng cho Bảng cuối
                var finalAdjustmentEndDateTime = preciseClosingDateTime ?? fallbackAdjustmentEndDateTime;

                // tách lịch điều chỉnh kế hoạch học tập chi tiết từng khóa (Bảng cuối)
                var adjustmentDetails = ParseAdjustmentDetails(fullText, finalAdjustmentEndDateTime);

                return new TeachingPlanData(
                    RegistrationTimeline: timelineNodes.AsReadOnly(),
                    SemesterStartDate: semesterStartDate,
                    SemesterEndDate: semesterEndDate,
                    AdjustmentDetails: adjustmentDetails.AsReadOnly()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse PDF teaching plan at {Path}", filePath);
                return new TeachingPlanData();
            }
        });
    }

    // --- CÁC PHƯƠNG THỨC TRỢ GIÚP ĐÃ PHÂN RÃ (MODULAR PRIVATE HELPERS) ---

    private static (DateTime? Start, DateTime? End) ParseSemesterDates(string fullText)
    {
        DateTime? semesterStartDate = null;
        DateTime? semesterEndDate = null;

        var semesterMatch = SemesterRegex().Match(fullText);
        if (semesterMatch.Success)
        {
            if (TryParseDate(semesterMatch.Groups["start"].Value, out var start))
                semesterStartDate = start;
            if (TryParseDate(semesterMatch.Groups["end"].Value, out var end))
                semesterEndDate = end;
        }

        return (semesterStartDate, semesterEndDate);
    }

    private List<TimelineNode> ParseTimelineNodes(
        string fullText, 
        DateTime? semesterStartDate, 
        DateTime? semesterEndDate, 
        DateTime? preciseClosingDateTime, 
        out DateTime fallbackAdjustmentEndDateTime)
    {
        var timelineNodes = new List<TimelineNode>();
        fallbackAdjustmentEndDateTime = new DateTime(2026, 4, 19, 17, 0, 0); // Dự phòng mặc định

        var startTag = "NỘI DUNG CÔNG VIỆC THỜI GIAN THỰC HIỆN";
        var endTag = "Lưu ý:";
        var startIndex = fullText.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
        var endIndex = fullText.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);

        if (startIndex == -1 || endIndex == -1)
        {
            return timelineNodes;
        }

        var table1Content = fullText.Substring(startIndex + startTag.Length, endIndex - (startIndex + startTag.Length)).Trim();
        var matches = RowRegex().Matches(table1Content);

        foreach (Match match in matches)
        {
            var idStr = match.Groups["id"].Value;
            int.TryParse(idStr, out var id);
            var content = NormalizeWhitespace(match.Groups["content"].Value);
            var dateStr = match.Groups["date"].Value.Trim();

            DateTime nodeStart = DateTime.MinValue;
            DateTime nodeEnd = DateTime.MinValue;
            var type = TimelineNodeType.Range;

            var rangeMatch = ComplexRangeRegex().Match(dateStr);
            if (rangeMatch.Success)
            {
                TryParseDateTimeParts(rangeMatch.Groups["start_date"].Value, rangeMatch.Groups["start_time"].Value, out nodeStart);
                TryParseDateTimeParts(rangeMatch.Groups["end_date"].Value, rangeMatch.Groups["end_time"].Value, out nodeEnd);
                type = TimelineNodeType.Range;
            }
            else
            {
                var singleMatch = SingleDateDetailRegex().Match(dateStr);
                if (singleMatch.Success)
                {
                    TryParseDateTimeParts(singleMatch.Groups["date"].Value, singleMatch.Groups["time"].Value, out nodeStart);
                    nodeEnd = nodeStart;
                    type = TimelineNodeType.SinglePoint;
                }
            }

            // Tự động nhận diện Hạn cuối / Hạn chót
            if (type == TimelineNodeType.SinglePoint &&
                (id == 4 ||
                 content.Contains("hạn cuối", StringComparison.OrdinalIgnoreCase) ||
                 content.Contains("hạn chót", StringComparison.OrdinalIgnoreCase)))
            {
                type = TimelineNodeType.DeadlineOrEnd;
            }

            // Tinh lọc title cực kỳ ngắn gọn, trực quan
            var cleanTitle = id switch
            {
                1 => "Công bộ thời khóa biểu",
                2 => "Đăng ký học phần (Đợt 1)",
                3 => "Điều chỉnh kế hoạch học tập",
                4 => "Duyệt mở thêm lớp học phần",
                5 => "Đóng KHHT & Công bố xóa lớp",
                6 => "Bắt đầu học kỳ mới",
                7 => "Thay đổi, đăng ký HP (Đợt 2)",
                8 => "Điều chỉnh KHHT (Bổ sung)",
                _ => content
            };

            string? subtitle = null;

            // Đồng bộ dòng số 6 (Bắt đầu học kỳ)
            if (id == 6 && semesterStartDate.HasValue)
            {
                nodeStart = semesterStartDate.Value;
                if (semesterEndDate.HasValue && semesterEndDate.Value > semesterStartDate.Value)
                {
                    nodeEnd = semesterEndDate.Value;
                    type = TimelineNodeType.Range;
                }
                else
                {
                    nodeEnd = semesterStartDate.Value;
                    type = TimelineNodeType.StartFrom;
                }
                subtitle = null;
            }

            // Lưu lại hạn kết thúc mặc định của đợt điều chỉnh ở dòng số [3]
            if (id == 3)
            {
                fallbackAdjustmentEndDateTime = nodeEnd;
                
                // ÁP DỤNG ĐỒNG BỘ: Nếu có ngày chính xác trích xuất từ thông báo đóng website phụ
                if (preciseClosingDateTime.HasValue && preciseClosingDateTime.Value >= nodeStart)
                {
                    nodeEnd = preciseClosingDateTime.Value;
                    type = TimelineNodeType.Range;
                    subtitle = "Đồng bộ từ thông báo đóng cổng phụ";
                }
            }

            // Thiết lập lịch chính xác cho Đợt 2 ở dòng số [7] (mặc định null để VM đè lịch cá nhân)
            if (id == 7)
            {
                subtitle = null;
            }

            if (id == 8)
            {
                nodeEnd = semesterEndDate ?? nodeStart.AddDays(7);
                type = TimelineNodeType.StartFrom;
            }

            timelineNodes.Add(new TimelineNode(cleanTitle, nodeStart, nodeEnd, type, subtitle));
        }

        return timelineNodes;
    }

    private static List<TeachingPlanAdjustmentDetail> ParseAdjustmentDetails(string fullText, DateTime endDateTime)
    {
        var adjustmentDetails = new List<TeachingPlanAdjustmentDetail>();
        var startTag3 = "3. Thời gian cụ thể cho đợt điều chỉnh kế hoạch học tập";
        var endTag3 = "4. Thời gian và địa điểm đăng ký:";
        var startIndex3 = fullText.IndexOf(startTag3, StringComparison.OrdinalIgnoreCase);
        var endIndex3 = fullText.IndexOf(endTag3, StringComparison.OrdinalIgnoreCase);

        if (startIndex3 == -1 || endIndex3 == -1)
        {
            return adjustmentDetails;
        }

        var table3Content = fullText.Substring(startIndex3, endIndex3 - startIndex3).Trim();

        // Dòng 1: Khóa 48 trở về trước
        var k48Match = Regex.Match(table3Content, @"1\s+(?<khoa>Khóa\s+48\s+trở\s+về\s+trước)\s+(?<time>\d{2}:\d{2})\s+ngày\s+(?<date>\d{1,2}/\d{1,2}/\d{4})\s+(?<group>Tất cả các Đơn vị)", RegexOptions.IgnoreCase);
        if (k48Match.Success)
        {
            TryParseDateTimeParts(k48Match.Groups["date"].Value, k48Match.Groups["time"].Value, out var start);
            var groups = ParseAllowedGroups(k48Match.Groups["group"].Value);
            adjustmentDetails.Add(new TeachingPlanAdjustmentDetail(k48Match.Groups["khoa"].Value, start, endDateTime, groups));
        }

        // Dòng 2: Khóa 49
        var k49Match = Regex.Match(table3Content, @"2\s+(?<khoa>Khóa\s+49)\s+(?<time>\d{2}:\d{2})\s+ngày\s+(?<date>\d{1,2}/\d{1,2}/\d{4})\s+(?<group>Tất cả các Đơn vị)", RegexOptions.IgnoreCase);
        if (k49Match.Success)
        {
            TryParseDateTimeParts(k49Match.Groups["date"].Value, k49Match.Groups["time"].Value, out var start);
            var groups = ParseAllowedGroups(k49Match.Groups["group"].Value);
            adjustmentDetails.Add(new TeachingPlanAdjustmentDetail(k49Match.Groups["khoa"].Value, start, endDateTime, groups));
        }

        // Dòng 3: Khóa 50
        var k50Index = table3Content.IndexOf("3 Khóa 50", StringComparison.OrdinalIgnoreCase);
        var k51Index = table3Content.IndexOf("4 Khóa 51", StringComparison.OrdinalIgnoreCase);
        if (k50Index != -1 && k51Index != -1)
        {
            var k50Text = table3Content.Substring(k50Index, k51Index - k50Index);
            ParseNestedSubGroups("Khóa 50", k50Text, endDateTime, adjustmentDetails);
        }

        // Dòng 4: Khóa 51
        if (k51Index != -1)
        {
            var k51Text = table3Content.Substring(k51Index);
            ParseNestedSubGroups("Khóa 51", k51Text, endDateTime, adjustmentDetails);
        }

        return adjustmentDetails;
    }

    private static void ParseNestedSubGroups(string cohort, string blockText, DateTime endDateTime, List<TeachingPlanAdjustmentDetail> list)
    {
        // TỐI ƯU HÓA: Sử dụng trực tiếp pre-compiled static SubGroupRegex() từ Source Generator
        var matches = SubGroupRegex().Matches(blockText);
        foreach (Match m in matches)
        {
            TryParseDateTimeParts(m.Groups["date"].Value, m.Groups["time"].Value, out var start);
            var groups = ParseAllowedGroups(m.Groups["group"].Value);
            list.Add(new TeachingPlanAdjustmentDetail(cohort, start, endDateTime, groups));
        }
    }

    private static IReadOnlyList<int> ParseAllowedGroups(string groupStr)
    {
        if (string.IsNullOrWhiteSpace(groupStr)) return Array.Empty<int>();

        groupStr = groupStr.Trim();
        if (groupStr.Contains("Tất cả các Đơn vị") || groupStr.Contains("Tất cả"))
        {
            return [1, 2, 3, 4, 5, 6];
        }

        var result = new List<int>();
        var numberMatches = Regex.Matches(groupStr, @"\d");
        foreach (Match m in numberMatches)
        {
            if (int.TryParse(m.Value, out var val))
            {
                result.Add(val);
            }
        }

        return result;
    }

    private static string NormalizeWhitespace(string input)
    {
        return Regex.Replace(input, "\\s+", " ").Trim();
    }

    private static bool TryParseDate(string value, out DateTime date)
    {
        date = DateTime.MinValue;
        if (DateTime.TryParseExact(value.Trim(), ["d/M/yyyy", "dd/MM/yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            date = dt;
            return true;
        }
        return false;
    }

    private static bool TryParseDateTimeParts(string dateStr, string timeStr, out DateTime result)
    {
        result = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(dateStr)) return false;

        if (!TryParseDate(dateStr, out var date)) return false;

        int hour = 0;
        int minute = 0;

        if (!string.IsNullOrWhiteSpace(timeStr))
        {
            // TỐI ƯU HÓA: Sử dụng pre-compiled static TimeRegex() thay vì Regex động
            var timeMatch = TimeRegex().Match(timeStr.Trim());
            if (timeMatch.Success)
            {
                var hStr = !string.IsNullOrEmpty(timeMatch.Groups[1].Value) ? timeMatch.Groups[1].Value : timeMatch.Groups[3].Value;
                var mStr = !string.IsNullOrEmpty(timeMatch.Groups[2].Value) ? timeMatch.Groups[2].Value : timeMatch.Groups[4].Value;
                int.TryParse(hStr, out hour);
                int.TryParse(mStr, out minute);
            }
        }

        result = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);
        return true;
    }
}
