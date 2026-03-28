using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Infrastructure.Excel;

public static class TimetableExcelBuilder
{
    private static readonly string[] DayHeaders = { "TIẾT / GIỜ", "THỨ 2", "THỨ 3", "THỨ 4", "THỨ 5", "THỨ 6", "THỨ 7", "CHỦ NHẬT" };

    private static readonly string[] PeriodTimes = {
        "07:00 - 07:50", "07:50 - 08:40", "08:50 - 09:40", "09:50 - 10:40", "10:40 - 11:30",
        "13:30 - 14:20", "14:20 - 15:10", "15:20 - 16:10", "16:10 - 17:00"
    };

    private static readonly XLColor[] PastelColors = {
        XLColor.FromHtml("#FFD1DC"), XLColor.FromHtml("#B2FBA5"), XLColor.FromHtml("#AEC6CF"),
        XLColor.FromHtml("#FDFD96"), XLColor.FromHtml("#CFCFC4"), XLColor.FromHtml("#FFB347"),
        XLColor.FromHtml("#E6B3FF"), XLColor.FromHtml("#FFCC99"), XLColor.FromHtml("#99FFCC"),
        XLColor.FromHtml("#FF99CC"), XLColor.FromHtml("#99CCFF"), XLColor.FromHtml("#FFFF99")
    };

    public static XLWorkbook BuildWorkbook(ScheduleBlueprint blueprint, string timetableSheetName = "Thời Khóa Biểu")
    {
        var wb = new XLWorkbook();
        AddTimetableWorksheet(wb, blueprint, timetableSheetName);
        return wb;
    }

    public static XLWorkbook BuildWorkbook(IEnumerable<(ScheduleBlueprint Blueprint, string SheetName)> timetables)
    {
        ArgumentNullException.ThrowIfNull(timetables);

        var wb = new XLWorkbook();
        var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int index = 1;

        foreach (var (blueprint, sheetName) in timetables)
        {
            var normalizedName = NormalizeWorksheetName(sheetName, $"TKB_{index}");
            var uniqueName = EnsureUniqueWorksheetName(normalizedName, usedSheetNames);
            AddTimetableWorksheet(wb, blueprint, uniqueName);
            index++;
        }

        if (wb.Worksheets.Count == 0)
        {
            throw new ArgumentException("Danh sách thời khóa biểu trống.", nameof(timetables));
        }

        return wb;
    }

    private static void AddTimetableWorksheet(XLWorkbook workbook, ScheduleBlueprint blueprint, string timetableSheetName)
    {
        ArgumentNullException.ThrowIfNull(workbook);

        if (!blueprint.TryTrim(out var trimmedBlueprint))
        {
            throw new Exception("Dữ liệu Thời khóa biểu không nhất quán (IsConsistent = false).");
        }

        var sheet = workbook.Worksheets.Add(timetableSheetName);

        sheet.Cell(1, 1).Value = "Thời Khóa Biểu Chi Tiết";
        var titleRange = sheet.Range(1, 1, 1, DayHeaders.Length);
        titleRange.Merge().Style.Font.SetBold().Font.FontSize = 16;
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int listStartCol = DayHeaders.Length + 2;
        sheet.Cell(1, listStartCol).Value = "Thông tin Giảng viên";
        sheet.Range(1, listStartCol, 1, listStartCol + 3).Merge().Style.Font.SetBold().Font.FontSize = 16;

        for (int c = 0; c < DayHeaders.Length; c++)
        {
            var cell = sheet.Cell(2, c + 1);
            cell.Value = DayHeaders[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1f5ca9");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        var headers = new[] { "Môn học", "Số TC", "Giảng viên", "Nhóm" };
        for (int i = 0; i < headers.Length; i++)
        {
            var h = sheet.Cell(2, listStartCol + i);
            h.Value = headers[i];
            h.Style.Fill.BackgroundColor = XLColor.FromHtml("#1f5ca9");
            h.Style.Font.FontColor = XLColor.White;
            h.Style.Font.Bold = true;
            h.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            h.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int startRow = 3;
        for (int period = 1; period < 10; period++)
        {
            int currentRow = startRow + (period - 1) * 2;

            if (period == 6)
            {
                var brCell = sheet.Cell(currentRow, 1);
                brCell.Value = "——— NGHỈ TRƯA (11:30 - 13:30) ———";
                var brRange = sheet.Range(currentRow, 1, currentRow, DayHeaders.Length);
                brRange.Merge().Style.Fill.BackgroundColor = XLColor.FromHtml("#1f5ca9");
                brRange.Style.Font.FontColor = XLColor.White;
                brRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                startRow++;
                currentRow = startRow + (period - 1) * 2;
            }

            var periodCell = sheet.Cell(currentRow, 1);
            periodCell.Value = period;
            sheet.Cell(currentRow + 1, 1).Value = PeriodTimes[period - 1];

            var periodRange = sheet.Range(currentRow, 1, currentRow + 1, 1);
            periodRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            periodRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            periodRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F6FA");
            periodRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            for (int c = 2; c <= DayHeaders.Length; c++)
            {
                sheet.Range(currentRow, c, currentRow + 1, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
        }

        int listRow = 3;
        int colorIndex = 0;
        int totalCredits = 0;

        var unscheduledClasses = new List<(Course course, CourseSection section)>();

        foreach (var course in trimmedBlueprint.Courses)
        {
            totalCredits += course.Credits;

            foreach (var section in course.Sections)
            {
                var xlBgColor = PastelColors[colorIndex % PastelColors.Length];

                if (section.ClassDays == null || !section.ClassDays.Any())
                {
                    unscheduledClasses.Add((course, section));
                }

                sheet.Cell(listRow, listStartCol).Value = course.Name_VN;
                sheet.Cell(listRow, listStartCol + 1).Value = course.Credits;
                sheet.Cell(listRow, listStartCol + 2).Value = section.Lecturer ?? "";
                sheet.Cell(listRow, listStartCol + 3).Value = section.Group ?? "";

                var listRange = sheet.Range(listRow, listStartCol, listRow, listStartCol + 3);
                listRange.Style.Fill.BackgroundColor = xlBgColor;
                listRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                listRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                listRow++;

                if (section.ClassDays != null && section.ClassDays.Any())
                {
                    foreach (var day in section.ClassDays)
                    {
                        int colIndex = day.AttendingDay;
                        int rowIndex = 3 + (day.StartPeriod - 1) * 2;
                        if (day.StartPeriod > 5) rowIndex++;

                        int endRow = rowIndex + (day.PeriodCount * 2) - 1;

                        var cell = sheet.Cell(rowIndex, colIndex);
                        cell.Value = $"{course.Name_VN}\n{course.Code} ({section.Group})\nPhòng: {day.Room}";

                        var classRange = sheet.Range(rowIndex, colIndex, endRow, colIndex);
                        classRange.Merge();
                        classRange.Style.Alignment.WrapText = true;
                        classRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        classRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        classRange.Style.Fill.BackgroundColor = xlBgColor;
                        classRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
                    }
                }

                colorIndex++;
            }
        }

        var tcLabelCell = sheet.Cell(listRow, listStartCol);
        tcLabelCell.Value = "Tổng số tín chỉ";
        tcLabelCell.Style.Font.SetBold();

        var tcValueCell = sheet.Cell(listRow, listStartCol + 1);
        tcValueCell.Value = totalCredits;
        tcValueCell.Style.Font.SetBold();
        tcValueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        var tcRange = sheet.Range(listRow, listStartCol, listRow, listStartCol + 3);
        tcRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tcRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tcLabelCell.Style.Border.LeftBorder = XLBorderStyleValues.Thick;
        tcLabelCell.Style.Border.LeftBorderColor = XLColor.FromHtml("#1f5ca9");

        int bottomRow = 25;

        if (unscheduledClasses.Any())
        {
            var titleCell = sheet.Cell(bottomRow, 2);
            titleCell.Value = "Môn học chưa có lịch:";
            titleCell.Style.Font.SetBold();

            sheet.Cell(bottomRow, 3).Value = unscheduledClasses[0].course.Name_VN;

            for (int i = 1; i < unscheduledClasses.Count; i++)
            {
                bottomRow++;
                sheet.Cell(bottomRow, 3).Value = unscheduledClasses[i].course.Name_VN;
            }
        }
        
        int footerRow = bottomRow + 1;
        if (footerRow < 26) footerRow = 26;

        var footerCell = sheet.Cell(footerRow, 7);
        footerCell.Value = "Được tạo bởi App CTU_Scheduler";
        footerCell.Style.Font.Italic = true;
        footerCell.Style.Font.FontColor = XLColor.Gray;
        sheet.Range(footerRow, 7, footerRow, 8).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        sheet.Columns().AdjustToContents();
        sheet.Column(1).Width = 14;
        for (int i = 2; i <= DayHeaders.Length; i++) sheet.Column(i).Width = 20;
    }

    private static string NormalizeWorksheetName(string? name, string fallback)
    {
        var raw = string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
        char[] invalidChars = { ':', '\\', '/', '?', '*', '[', ']' };
        foreach (var invalidChar in invalidChars)
        {
            raw = raw.Replace(invalidChar, '_');
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = fallback;
        }

        return raw.Length > 31 ? raw[..31] : raw;
    }

    private static string EnsureUniqueWorksheetName(string baseName, ISet<string> usedNames)
    {
        if (usedNames.Add(baseName))
        {
            return baseName;
        }

        int suffix = 2;
        while (true)
        {
            var suffixText = $"_{suffix}";
            int maxBaseLength = 31 - suffixText.Length;
            var truncated = baseName.Length > maxBaseLength ? baseName[..maxBaseLength] : baseName;
            var candidate = $"{truncated}{suffixText}";

            if (usedNames.Add(candidate))
            {
                return candidate;
            }

            suffix++;
        }
    }
}