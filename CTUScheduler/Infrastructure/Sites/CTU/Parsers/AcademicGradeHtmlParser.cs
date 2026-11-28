// Created: 2026-11-28
// Implemented by AI with author's review.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Models.Academic.Grades;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using HtmlAgilityPack;

namespace CTUScheduler.Infrastructure.Sites.CTU.Parsers;

public partial class AcademicGradeHtmlParser : IAcademicGradeHtmlParser
{
    [GeneratedRegex(
        @"Xem\s+Điểm\s+Học\s+Kỳ\s+(?<term>1|2|3|Hè)\s+Năm\s+Học\s+(?<year>\d{4})\s*-\s*\d{4}",
        RegexOptions.IgnoreCase)]
    private static partial Regex SemesterHeaderRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiWhitespaceRegex();

    public GradeTranscript Parse(string html, DateTimeOffset fetchedAt)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new InvalidDataException("HTML bảng điểm trống.");
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Client đã kiểm tra 401/403 và Captcha trước đó; nếu handler cứu phiên thất bại (VD chưa đăng nhập),
        // server PHP vẫn trả 200 OK kèm script chuyển hướng ../../login.php nên sẽ không có form frmXemDiem.
        var formNode = doc.DocumentNode.SelectSingleNode("//form[@name='frmXemDiem']");
        if (formNode is null)
        {
            throw new SessionExpiredException("Không tìm thấy biểu mẫu bảng điểm (phiên có thể đã hết hạn).");
        }

        var orderedSemesters = ParseSemesters(formNode)
            .OrderBy(s => s.AcademicYearStart)
            .ThenBy(s => s.Term)
            .ToList();

        return new GradeTranscript(orderedSemesters, fetchedAt);
    }

    private static List<SemesterGrade> ParseSemesters(HtmlNode formNode)
    {
        var result = new List<SemesterGrade>();
        var headerCells = formNode.SelectNodes(".//td[contains(@class, 'main_1')]");
        if (headerCells is null)
        {
            return result;
        }

        foreach (var headerCell in headerCells)
        {
            // Đầu form có bảng bộ lọc cũng dùng class main_1 ("Xem Điểm Học Kỳ");
            // regex chỉ khớp các khối học kỳ thật sự có kèm "Năm Học YYYY - YYYY".
            string headerText = NormalizeText(headerCell.InnerText);
            var match = SemesterHeaderRegex().Match(headerText);
            if (!match.Success)
            {
                continue;
            }

            int academicYearStart = int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture);
            int term = ParseTerm(match.Groups["term"].Value);

            var semesterTable = headerCell.Ancestors("table").FirstOrDefault();
            if (semesterTable is null)
            {
                continue;
            }

            var coursesTable = semesterTable.SelectSingleNode(".//table[contains(@class, 'border_1')]");
            var courses = coursesTable is not null ? ParseCourses(coursesTable) : [];

            // Học kỳ đang học chưa chốt tổng kết sẽ không có bảng tổng kết thứ hai (Summary = null).
            var summaryTable = semesterTable.SelectSingleNode(".//table[not(contains(@class, 'border_1'))]");
            var summary = summaryTable is not null ? ParseSummary(summaryTable) : null;

            result.Add(new SemesterGrade(academicYearStart, term, courses, summary));
        }

        return result;
    }

    private static List<CourseGrade> ParseCourses(HtmlNode coursesTable)
    {
        var courses = new List<CourseGrade>();
        // Hỗ trợ cả HTML thô của trường (không có <tbody>) lẫn HTML trình duyệt lưu lại (có <tbody>).
        var rows = coursesTable.SelectNodes("./tr | ./tbody/tr");
        if (rows is null)
        {
            return courses;
        }

        foreach (var row in rows)
        {
            var cells = row.SelectNodes("./td");
            if (cells is null || cells.Count < 8)
            {
                continue;
            }

            // Bỏ qua dòng tiêu đề cột (Stt | Mã HP | ...)
            if (!int.TryParse(NormalizeCell(cells[0]), NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
            {
                continue;
            }

            string courseCode = NormalizeCell(cells[1]);
            string courseName = NormalizeCell(cells[2]);
            bool isConditionCourse = string.Equals(NormalizeCell(cells[3]), "x", StringComparison.OrdinalIgnoreCase);

            string groupRaw = NormalizeCell(cells[4]);
            string? groupCode = string.IsNullOrEmpty(groupRaw) ? null : groupRaw;

            int credits = int.TryParse(NormalizeCell(cells[5]), NumberStyles.Integer, CultureInfo.InvariantCulture, out int c)
                ? c
                : 0;

            string letterRaw = NormalizeCell(cells[6]);
            string? rawLetterGrade = string.IsNullOrEmpty(letterRaw) ? null : letterRaw;
            GradeLetter letterGrade = GradeLetterExtensions.ParseGradeLetter(rawLetterGrade);

            // Ô Điểm số có thể chứa số (VD: 9.4) hoặc ghi chú phi số như "*(BL)" ở môn được miễn.
            string scoreText = NormalizeCell(cells[7]);
            double? numericScore = null;
            string? rawScoreNote = null;
            if (!string.IsNullOrEmpty(scoreText))
            {
                if (double.TryParse(scoreText, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedScore))
                {
                    numericScore = parsedScore;
                }
                else
                {
                    rawScoreNote = scoreText;
                }
            }

            // Dòng miễn/bảo lưu *(BL) trên HTML của CTU bị thiếu hẳn thẻ <td> thứ 9 (chỉ có 8 cột),
            // nhưng thực tế các môn này vẫn được cộng vào Tổng số tín chỉ tích lũy (VD: XH023 +4 TC, CC009 +10 TC).
            bool isAccumulated = cells.Count >= 9
                ? NormalizeCell(cells[8]).Contains('*')
                : letterGrade == GradeLetter.M || (rawScoreNote?.Contains('*') ?? false);

            courses.Add(new CourseGrade(
                Index: index,
                CourseCode: courseCode,
                CourseName: courseName,
                IsConditionCourse: isConditionCourse,
                GroupCode: groupCode,
                Credits: credits,
                RawLetterGrade: rawLetterGrade,
                LetterGrade: letterGrade,
                NumericScore: numericScore,
                RawScoreNote: rawScoreNote,
                IsAccumulated: isAccumulated));
        }

        return courses;
    }

    private static SemesterGradeSummary? ParseSummary(HtmlNode summaryTable)
    {
        var rows = summaryTable.SelectNodes("./tr | ./tbody/tr");
        if (rows is null || rows.Count == 0)
        {
            return null;
        }

        int semesterCredits = 0;
        int totalCredits = 0;
        double semesterGpa = 0;
        double cumulativeGpa = 0;
        int? conductScore = null;

        foreach (var row in rows)
        {
            var cells = row.SelectNodes("./td");
            if (cells is null)
            {
                continue;
            }

            // Bảng tổng kết bố trí 2 cặp (nhãn, giá trị) trên cùng 1 hàng (4 ô <td>), nên duyệt bước nhảy 2.
            for (int i = 0; i + 1 < cells.Count; i += 2)
            {
                string label = NormalizeText(cells[i].InnerText).ToLowerInvariant();
                string value = NormalizeCell(cells[i + 1]);

                // Phải so khớp chính xác chuỗi nhãn để không nhầm "tổng số tín chỉ tích lũy học kỳ" với "tổng số tín chỉ tích lũy".
                switch (label)
                {
                    case "tổng số tín chỉ tích lũy học kỳ":
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int semCr))
                        {
                            semesterCredits = semCr;
                        }
                        break;

                    case "tổng số tín chỉ tích lũy":
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int totCr))
                        {
                            totalCredits = totCr;
                        }
                        break;

                    case "điểm trung bình học kỳ":
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double semGpa))
                        {
                            semesterGpa = semGpa;
                        }
                        break;

                    case "điểm trung bình tích lũy":
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double cumGpa))
                        {
                            cumulativeGpa = cumGpa;
                        }
                        break;

                    case "điểm rèn luyện":
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int drl))
                        {
                            conductScore = drl;
                        }
                        break;
                }
            }
        }

        return new SemesterGradeSummary(
            SemesterAccumulatedCredits: semesterCredits,
            TotalAccumulatedCredits: totalCredits,
            SemesterGpa: semesterGpa,
            CumulativeGpa: cumulativeGpa,
            ConductScore: conductScore);
    }

    private static int ParseTerm(string rawTerm) =>
        rawTerm.Trim().Equals("Hè", StringComparison.OrdinalIgnoreCase)
            ? 3
            : int.Parse(rawTerm, CultureInfo.InvariantCulture);

    private static string NormalizeCell(HtmlNode cell) => NormalizeText(cell.InnerText);

    private static string NormalizeText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        // HTML của CTU dùng lẫn ký tự 'Ð' (U+00D0 Eth) thay cho 'Đ' (U+0110) ở tiêu đề và tổng kết,
        // kèm &nbsp; (\u00A0) và nhiều khoảng trắng/xuống dòng thụt lề bên trong tên môn học.
        string decoded = HtmlEntity.DeEntitize(raw)
            .Replace('\u00A0', ' ')
            .Replace('Ð', 'Đ')
            .Replace('ð', 'đ');

        return MultiWhitespaceRegex().Replace(decoded, " ").Trim();
    }
}
