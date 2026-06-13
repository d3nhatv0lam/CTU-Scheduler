using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.TeachingPlan;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace CTUScheduler.Infrastructure.Services.TeachingPlan;

public partial class TeachingPlanPdfParser : ITeachingPlanPdfParser
{
    private readonly ILogger<TeachingPlanPdfParser> _logger;

    #region Regex Wrappers with Hover Documentation

    /// <summary>
    /// Nhận diện ngày giờ đóng kế hoạch dạng chữ đầy đủ tiếng Việt.
    /// <para><b>Ví dụ khớp:</b> "17 giờ 00 ngày 25 tháng 08 năm 2026"</para>
    /// <para><b>Các Group trích xuất:</b></para>
    /// <list type="bullet">
    /// <item><description><c>hour</c>: Giờ đóng hệ thống (Ví dụ: "17")</description></item>
    /// <item><description><c>minute</c>: Phút đóng hệ thống (Ví dụ: "00")</description></item>
    /// <item><description><c>day</c>: Ngày đóng hệ thống (Ví dụ: "25")</description></item>
    /// <item><description><c>month</c>: Tháng đóng hệ thống (Ví dụ: "08")</description></item>
    /// <item><description><c>year</c>: Năm đóng hệ thống (Ví dụ: "2026")</description></item>
    /// </list>
    /// </summary>
    private static Regex ClosingPattern1Regex() => ClosingPattern1RegexInstance();

    /// <summary>
    /// Nhận diện ngày giờ đóng kế hoạch dạng viết tắt ngắn gọn.
    /// <para><b>Ví dụ khớp:</b> "17h00 ngày 25/08/2026" hoặc "17g00 25/08/2026"</para>
    /// <para><b>Các Group trích xuất:</b></para>
    /// <list type="bullet">
    /// <item><description><c>hour</c>: Giờ đóng hệ thống (Ví dụ: "17")</description></item>
    /// <item><description><c>minute</c>: Phút đóng hệ thống (Ví dụ: "00")</description></item>
    /// <item><description><c>day</c>: Ngày đóng hệ thống (Ví dụ: "25")</description></item>
    /// <item><description><c>month</c>: Tháng đóng hệ thống (Ví dụ: "08")</description></item>
    /// <item><description><c>year</c>: Năm đóng hệ thống (Ví dụ: "2026")</description></item>
    /// </list>
    /// </summary>
    private static Regex ClosingPattern2Regex() => ClosingPattern2RegexInstance();

    /// <summary>
    /// Trích xuất khoảng thời gian diễn ra học kỳ ở phần giới thiệu của văn bản.
    /// <para><b>Ví dụ khớp:</b> "diễn ra từ ngày 10/08/2026 đến ngày 31/12/2026"</para>
    /// <para><b>Các Group trích xuất:</b></para>
    /// <list type="bullet">
    /// <item><description><c>start</c>: Ngày bắt đầu học kỳ dạng <c>dd/MM/yyyy</c> (Ví dụ: "10/08/2026")</description></item>
    /// <item><description><c>end</c>: Ngày kết thúc học kỳ dạng <c>dd/MM/yyyy</c> (Ví dụ: "31/12/2026")</description></item>
    /// </list>
    /// </summary>
    private static Regex SemesterRegex() => SemesterRegexInstance();

    /// <summary>
    /// Nhận diện khoảng ngày giờ đăng ký học phần (có thể đi kèm giờ bắt đầu/kết thúc cụ thể hoặc không).
    /// <para><b>Ví dụ khớp:</b> "17h00 ngày 10/08/2026 đến 17h00 ngày 20/08/2026" hoặc "10/08/2026 - 20/08/2026"</para>
    /// <para><b>Các Group trích xuất:</b></para>
    /// <list type="bullet">
    /// <item><description><c>start_time</c>: Giờ bắt đầu đăng ký (Ví dụ: "17h00", có thể <c>null</c>)</description></item>
    /// <item><description><c>start_date</c>: Ngày bắt đầu đăng ký dạng <c>dd/MM/yyyy</c> (Ví dụ: "10/08/2026")</description></item>
    /// <item><description><c>end_time</c>: Giờ kết thúc đăng ký (Ví dụ: "17h00", có thể <c>null</c>)</description></item>
    /// <item><description><c>end_date</c>: Ngày kết thúc đăng ký dạng <c>dd/MM/yyyy</c> (Ví dụ: "20/08/2026")</description></item>
    /// </list>
    /// </summary>
    private static Regex ComplexRangeRegex() => ComplexRangeRegexInstance();

    /// <summary>
    /// Nhận diện một mốc ngày giờ cụ thể duy nhất (không phải dạng khoảng thời gian).
    /// <para><b>Ví dụ khớp:</b> "17h00 ngày 15/09/2026" hoặc "15/09/2026"</para>
    /// <para><b>Các Group trích xuất:</b></para>
    /// <list type="bullet">
    /// <item><description><c>time</c>: Giờ của mốc cụ thể (Ví dụ: "17h00", có thể <c>null</c>)</description></item>
    /// <item><description><c>date</c>: Ngày của mốc cụ thể dạng <c>dd/MM/yyyy</c> (Ví dụ: "15/09/2026")</description></item>
    /// </list>
    /// </summary>
    private static Regex SingleDateDetailRegex() => SingleDateDetailRegexInstance();

    /// <summary>
    /// Quét cấu trúc một dòng dữ liệu của Bảng 1 (Các mốc thời gian chính) để bóc tách thông tin công việc và thời gian tương ứng.
    /// <para><b>Ví dụ khớp:</b> "2 Đăng ký học phần (Đợt 1) 17h00 ngày 10/08/2026 đến 17h00 ngày 20/08/2026"</para>
    /// <para><b>Các Group trích xuất:</b></para>
    /// <list type="bullet">
    /// <item><description><c>id</c>: Số thứ tự dòng trong bảng (Ví dụ: "2")</description></item>
    /// <item><description><c>content</c>: Nội dung công việc (Ví dụ: "Đăng ký học phần (Đợt 1)")</description></item>
    /// <item><description><c>date</c>: Chuỗi thô chứa thông tin ngày giờ đi kèm (Ví dụ: "17h00 ngày 10/08/2026 đến 17h00 ngày 20/08/2026")</description></item>
    /// </list>
    /// </summary>
    private static Regex RowRegex() => RowRegexInstance();

    /// <summary>
    /// Bóc tách lịch đăng ký học phần chi tiết cho từng nhóm đăng ký trong Bảng 3.
    /// <para><b>Ví dụ khớp:</b> "08:00 ngày 15/08/2026 1"</para>
    /// <para><b>Các Group trích xuất:</b></para>
    /// <list type="bullet">
    /// <item><description><c>time</c>: Giờ bắt đầu đăng ký của nhóm (Ví dụ: "08:00")</description></item>
    /// <item><description><c>date</c>: Ngày đăng ký của nhóm dạng <c>dd/MM/yyyy</c> (Ví dụ: "15/08/2026")</description></item>
    /// <item><description><c>group</c>: Số thứ tự nhóm đăng ký (Ví dụ: "1") - Chỉ 1 số duy nhất, không hỗ trợ "1 2..."</description></item>
    /// </list>
    /// </summary>
    private static Regex SubGroupRegex() => SubGroupRegexInstance();

    /// <summary>
    /// Nhận diện tiêu đề phân loại Khóa trong Bảng 3 nhằm chia nhỏ vùng văn bản đăng ký của từng khóa.
    /// <para><b>Ví dụ khớp:</b> "1 Khóa 48 trở về trước" hoặc "2 Khóa 49"</para>
    /// <para><b>Các Group trích xuất:</b></para>
    /// <list type="bullet">
    /// <item><description><c>row_num</c>: Số thứ tự dòng của khóa trong bảng (Ví dụ: "1")</description></item>
    /// <item><description><c>cohort_name</c>: Tên khóa tương ứng (Ví dụ: "Khóa 48 trở về trước" hoặc "Khóa 49")</description></item>
    /// </list>
    /// </summary>
    private static Regex CohortHeaderRegex() => CohortHeaderRegexInstance();

    #endregion

    #region Regex Source Generator Instances (Collapsed)

    [GeneratedRegex(@"(?<hour>\d{1,2})\s*giờ\s*(?<minute>\d{2})\s*(?:ngày)?\s*(?<day>\d{1,2})\s*tháng\s*(?<month>\d{1,2})\s*năm\s*(?<year>\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex ClosingPattern1RegexInstance();

    [GeneratedRegex(@"(?<hour>\d{1,2})[h:g](?<minute>\d{2})\s*(?:ngày\s+)?(?<day>\d{1,2})/(?<month>\d{1,2})/(?<year>\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex ClosingPattern2RegexInstance();

    [GeneratedRegex(@"diễn\s+ra\s+từ\s+(?:ngày\s+)?(?<start>\d{1,2}/\d{1,2}/\d{4})\s+đến\s+(?:ngày\s+)?(?<end>\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex SemesterRegexInstance();

    [GeneratedRegex(@"(?<start_time>\d{1,2}[h:g]\d{2}|\d{1,2}:\d{2})?\s*(?:ngày\s+)?(?<start_date>\d{1,2}/\d{1,2}/\d{4})\s*(?:đến|[-–])\s*(?<end_time>\d{1,2}[h:g]\d{2}|\d{1,2}:\d{2})?\s*(?:ngày\s+)?(?<end_date>\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex ComplexRangeRegexInstance();

    [GeneratedRegex(@"(?<time>\d{1,2}[h:g]\d{2}|\d{1,2}:\d{2})?\s*(?:ngày\s+)?(?<date>\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex SingleDateDetailRegexInstance();

    [GeneratedRegex(@"(?<id>\d{1,2})\s+(?<content>.+?)\s+(?<date>(?:(?:\d{1,2}[h:g]\d{2}|\d{1,2}:\d{2})\s+(?:ngày\s+)?)?\d{1,2}/\d{1,2}/\d{4}(?:\s*(?:đến|[-–])\s*(?:(?:\d{1,2}[h:g]\d{2}|\d{1,2}:\d{2})\s+(?:ngày\s+)?)?\d{1,2}/\d{1,2}/\d{4})?)", RegexOptions.Singleline)]
    private static partial Regex RowRegexInstance();

    [GeneratedRegex(@"(?<time>\d{2}:\d{2})\s+ngày\s+(?<date>\d{1,2}/\d{1,2}/\d{4})\s+(?<group>\d)")]
    private static partial Regex SubGroupRegexInstance();

    [GeneratedRegex(@"\b(?<row_num>\d+)\s+(?<cohort_name>Khóa\s+\d+(?:\s+trở\s+về\s+trước)?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CohortHeaderRegexInstance();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"(\d{1,2})[hg](\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex TimeNormalizationRegex();

    [GeneratedRegex(@"\d")]
    private static partial Regex DigitRegex();

    #endregion

    public TeachingPlanPdfParser(ILogger<TeachingPlanPdfParser> logger)
    {
        _logger = logger;
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
                
                var pageCount = document.NumberOfPages;
                var pdfTextBuilder = new StringBuilder(pageCount * 1000);
                foreach (var page in document.GetPages())
                {
                    pdfTextBuilder.AppendLine(page.Text);
                }
                var rawPdfText = pdfTextBuilder.ToString();

                var (semesterStartDate, semesterEndDate) = ParseSemesterDates(rawPdfText);

                var timelineNodes = ParseTimelineNodes(
                    rawPdfText, 
                    semesterStartDate, 
                    semesterEndDate, 
                    preciseClosingDateTime, 
                    out var fallbackAdjustmentEndDateTime);
                
                var adjustmentDetails = ParseAdjustmentDetails(rawPdfText, fallbackAdjustmentEndDateTime);

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

    /// <summary>
    /// Lấy thời gian bắt đầu, kết thúc của học kỳ
    /// </summary>
    /// <param name="rawPdfText"></param>
    /// <returns></returns>
    private static (DateTime? Start, DateTime? End) ParseSemesterDates(string rawPdfText)
    {
        DateTime? semesterStartDate = null;
        DateTime? semesterEndDate = null;

        var semesterMatch = SemesterRegex().Match(rawPdfText);
        if (semesterMatch.Success)
        {
            if (TryParseDate(semesterMatch.Groups["start"].Value, out var start))
                semesterStartDate = start;
            if (TryParseDate(semesterMatch.Groups["end"].Value, out var end))
                semesterEndDate = end;
        }

        return (semesterStartDate, semesterEndDate);
    }

    /// <summary>
    /// Lấy danh sách các Node thời gian trong kế hoạch
    /// </summary>
    /// <param name="rawPdfText"></param>
    /// <param name="semesterStartDate"></param>
    /// <param name="semesterEndDate"></param>
    /// <param name="preciseClosingDateTime"></param>
    /// <param name="fallbackAdjustmentEndDateTime">our var: thời gian kết thúc </param>
    /// <returns></returns>
    private List<TimelineNode> ParseTimelineNodes(
        string rawPdfText, 
        DateTime? semesterStartDate, 
        DateTime? semesterEndDate, 
        DateTime? preciseClosingDateTime, 
        out DateTime fallbackAdjustmentEndDateTime)
    {
        var timelineNodes = new List<TimelineNode>();
        fallbackAdjustmentEndDateTime = semesterStartDate?.AddDays(-21) ?? DateTime.Today.AddDays(14);

        var startTag = "NỘI DUNG CÔNG VIỆC THỜI GIAN THỰC HIỆN";
        var endTag = "Lưu ý:";
        var startIndex = rawPdfText.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
        var endIndex = rawPdfText.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);

        if (startIndex == -1 || endIndex == -1)
        {
            return timelineNodes;
        }

        var table1Content = rawPdfText.Substring(startIndex + startTag.Length, endIndex - (startIndex + startTag.Length)).Trim();
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

            // Nhận diện loại công việc dựa trên từ khóa trong content
            var step = DetectStep(content);

            if (type == TimelineNodeType.SinglePoint &&
                (id == 4 || step == TeachingPlanStep.ApproveExtraClasses))
            {
                type = TimelineNodeType.DeadlineOrEnd;
            }

            // Lấy tiêu đề hiển thị chuẩn hóa dựa trên Enum
            var cleanTitle = step.ToFriendlyString();
            if (step == TeachingPlanStep.Unknown)
            {
                cleanTitle = content;
            }

            string? subtitle = null;

            // Đồng bộ dòng Bắt đầu học kỳ
            if (step == TeachingPlanStep.StartSemester && semesterStartDate.HasValue)
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

            // Lưu lại hạn kết thúc mặc định của đợt điều chỉnh kế hoạch học tập
            if (step == TeachingPlanStep.AdjustStudyPlan)
            {
                fallbackAdjustmentEndDateTime = nodeEnd;
            }

            if (step == TeachingPlanStep.AdjustStudyPlanSupplementary)
            {
                nodeEnd = semesterEndDate ?? nodeStart;
                type = TimelineNodeType.StartFrom;

                if (preciseClosingDateTime.HasValue && preciseClosingDateTime.Value >= nodeStart)
                {
                    nodeEnd = preciseClosingDateTime.Value;
                    type = TimelineNodeType.Range;
                    subtitle = "Đồng bộ từ thông báo đóng KHHT";
                }
            }

            timelineNodes.Add(new TimelineNode(cleanTitle, nodeStart, nodeEnd, type, subtitle));
        }

        return timelineNodes;
    }

    private static TeachingPlanStep DetectStep(string content)
    {
        if (content.Contains("công bố", StringComparison.OrdinalIgnoreCase) && 
            content.Contains("thời khóa biểu", StringComparison.OrdinalIgnoreCase))
        {
            return TeachingPlanStep.PublishSchedule;
        }
        
        if (content.Contains("đăng ký", StringComparison.OrdinalIgnoreCase) && 
            content.Contains("đợt 1", StringComparison.OrdinalIgnoreCase))
        {
            return TeachingPlanStep.CourseRegistrationPhase1;
        }
        
        if (content.Contains("đăng ký", StringComparison.OrdinalIgnoreCase) && 
            content.Contains("đợt 2", StringComparison.OrdinalIgnoreCase))
        {
            return TeachingPlanStep.CourseRegistrationPhase2;
        }
        
        if (content.Contains("điều chỉnh", StringComparison.OrdinalIgnoreCase) && 
            content.Contains("bổ sung", StringComparison.OrdinalIgnoreCase))
        {
            return TeachingPlanStep.AdjustStudyPlanSupplementary;
        }
        
        if (content.Contains("điều chỉnh", StringComparison.OrdinalIgnoreCase) && 
            (content.Contains("kế hoạch", StringComparison.OrdinalIgnoreCase) || content.Contains("KHHT", StringComparison.OrdinalIgnoreCase)))
        {
            return TeachingPlanStep.AdjustStudyPlan;
        }
        
        if (content.Contains("duyệt mở", StringComparison.OrdinalIgnoreCase) || 
            content.Contains("mở thêm", StringComparison.OrdinalIgnoreCase))
        {
            return TeachingPlanStep.ApproveExtraClasses;
        }
        
        if (content.Contains("đóng", StringComparison.OrdinalIgnoreCase) && 
            (content.Contains("xóa lớp", StringComparison.OrdinalIgnoreCase) || content.Contains("xóa HP", StringComparison.OrdinalIgnoreCase)))
        {
            return TeachingPlanStep.CloseRegistration;
        }
        
        if (content.Contains("bắt đầu", StringComparison.OrdinalIgnoreCase) && 
            (content.Contains("giảng dạy", StringComparison.OrdinalIgnoreCase) || content.Contains("học kỳ", StringComparison.OrdinalIgnoreCase)))
        {
            return TeachingPlanStep.StartSemester;
        }

        return TeachingPlanStep.Unknown;
    }

    private static List<TeachingPlanAdjustmentDetail> ParseAdjustmentDetails(string rawPdfText, DateTime endDateTime)
    {
        var adjustmentDetails = new List<TeachingPlanAdjustmentDetail>();
        var startTag3 = "3. Thời gian cụ thể cho đợt điều chỉnh kế hoạch học tập";
        var endTag3 = "4. Thời gian và địa điểm đăng ký:";
        var startIndex3 = rawPdfText.IndexOf(startTag3, StringComparison.OrdinalIgnoreCase);
        var endIndex3 = rawPdfText.IndexOf(endTag3, StringComparison.OrdinalIgnoreCase);

        if (startIndex3 == -1 || endIndex3 == -1)
        {
            return adjustmentDetails;
        }

        var table3Content = rawPdfText.Substring(startIndex3, endIndex3 - startIndex3).Trim();

        var headerRegex = CohortHeaderRegex();
        var matches = headerRegex.Matches(table3Content);
        if (matches.Count == 0) return adjustmentDetails;

        for (int i = 0; i < matches.Count; i++)
        {
            var currentMatch = matches[i];
            var cohortName = currentMatch.Groups["cohort_name"].Value.Trim();
            
            // lấy row cần trích xuất
            int startIdx = currentMatch.Index + currentMatch.Length;
            int nextMatchIdx = (i < matches.Count - 1) ? matches[i + 1].Index : table3Content.Length;
            var blockText = table3Content.Substring(startIdx, nextMatchIdx - startIdx).Trim();

            var subGroupMatches = SubGroupRegex().Matches(blockText);
            // Mỗi nhóm nhỏ có một mốc giờ riêng biệt và 1 nhóm duy nhất
            if (subGroupMatches.Count > 0)
            {
                foreach (Match m in subGroupMatches)
                {
                    if (TryParseDateTimeParts(m.Groups["date"].Value, m.Groups["time"].Value, out var start))
                    {
                        var groups = ParseAllowedGroups(m.Groups["group"].Value);
                        adjustmentDetails.Add(new TeachingPlanAdjustmentDetail(cohortName, start, endDateTime, groups));
                    }
                }
            }
            // trường hợp group là nhiều group gom lại -> Cắt Date / Group ra rồi parse từng cái
            else
            {
                var singleTimeMatch = SingleDateDetailRegex().Match(blockText);
                if (!singleTimeMatch.Success) continue;
                
                if (TryParseDateTimeParts(singleTimeMatch.Groups["date"].Value, singleTimeMatch.Groups["time"].Value, out var start))
                {
                    var groupStr = blockText.Substring(singleTimeMatch.Index + singleTimeMatch.Length).Trim();
                    var groups = ParseAllowedGroups(groupStr);
                    adjustmentDetails.Add(new TeachingPlanAdjustmentDetail(cohortName, start, endDateTime, groups));
                }
            }
        }

        return adjustmentDetails;
    }

    private static List<int> ParseAllowedGroups(string groupStr)
    {
        if (string.IsNullOrWhiteSpace(groupStr)) return [];

        groupStr = groupStr.Trim();
        if (groupStr.Contains("Tất cả các Đơn vị", StringComparison.OrdinalIgnoreCase) || 
            groupStr.Contains("Tất cả", StringComparison.OrdinalIgnoreCase))
        {
            return [1, 2, 3, 4, 5, 6];
        }

        var result = new List<int>();
        var numberMatches = DigitRegex().Matches(groupStr);
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
        return WhitespaceRegex().Replace(input, " ").Trim();
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

    private static DateTime? ParseDateTime(string input)
    {
        if (string.IsNullOrEmpty(input)) return null;

        // Normalize "h", "g" to ":" for time formats (e.g. "14h00" -> "14:00")
        string normalized = TimeNormalizationRegex().Replace(input, "$1:$2");
        normalized = normalized.Replace(" ngày ", " ").Trim();

        string[] formats = 
        [
            "HH:mm dd/MM/yyyy", "dd/MM/yyyy HH:mm", "dd/MM/yyyy",
            "HH:mm d/M/yyyy", "d/M/yyyy HH:mm", "d/M/yyyy",
            "H:mm dd/MM/yyyy", "dd/MM/yyyy H:mm",
            "H:mm d/M/yyyy", "d/M/yyyy H:mm"
        ];

        if (DateTime.TryParseExact(normalized, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        if (DateTime.TryParse(normalized, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out dt))
            return dt;

        return null;
    }

    private static bool TryParseDateTimeParts(string dateStr, string timeStr, out DateTime result)
    {
        result = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(dateStr)) return false;

        string input = string.IsNullOrWhiteSpace(timeStr) ? dateStr : $"{timeStr} ngày {dateStr}";
        var parsed = ParseDateTime(input);
        if (parsed.HasValue)
        {
            result = parsed.Value;
            return true;
        }
        return false;
    }
}
