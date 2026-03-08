using ClosedXML.Excel;
using CTUScheduler.Core.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CTUScheduler.Infrastructure.Exel;

public static class TimetableExcelBuilder
{
    private static readonly string[] DayHeaders = { "TIẾT / GIỜ", "THỨ 2", "THỨ 3", "THỨ 4", "THỨ 5", "THỨ 6", "THỨ 7", "CHỦ NHẬT" };

    // Khung giờ học chuẩn của Đại học Cần Thơ (CTU)
    private static readonly string[] PeriodTimes = {
        "07:00 - 07:50", // Tiết 1
        "07:50 - 08:40", // Tiết 2
        "08:50 - 09:40", // Tiết 3
        "09:50 - 10:40", // Tiết 4
        "10:40 - 11:30", // Tiết 5
        "13:30 - 14:20", // Tiết 6
        "14:20 - 15:10", // Tiết 7
        "15:20 - 16:10", // Tiết 8
        "16:10 - 17:00", // Tiết 9
        "17:00 - 17:50"  // Tiết 10 (Tiết nghỉ dự phòng)
    };

    // Danh sách các màu Pastel tươi sáng, độ tương phản cao để làm nền
    private static readonly XLColor[] PastelColors = {
        XLColor.FromHtml("#FFD1DC"), // Hồng nhạt
        XLColor.FromHtml("#B2FBA5"), // Xanh lá mạ
        XLColor.FromHtml("#AEC6CF"), // Xanh biển nhạt
        XLColor.FromHtml("#FDFD96"), // Vàng nhạt
        XLColor.FromHtml("#CFCFC4"), // Xám tro nhạt
        XLColor.FromHtml("#FFB347"), // Cam pastel
        XLColor.FromHtml("#E6B3FF"), // Tím nhạt
        XLColor.FromHtml("#FFCC99"), // Đào nhạt
        XLColor.FromHtml("#99FFCC"), // Xanh bạc hà
        XLColor.FromHtml("#FF99CC"), // Hồng cánh sen nhạt
        XLColor.FromHtml("#99CCFF"), // Xanh da trời nhạt
        XLColor.FromHtml("#FFFF99")  // Vàng chanh nhạt
    };

    public static XLWorkbook BuildWorkbook(IEnumerable<SectionChoice> choices, string timetableSheetName = "Thời Khóa Biểu")
    {
        var wb = new XLWorkbook();
        var sheet = wb.Worksheets.Add(timetableSheetName);

        // --- 1. Tạo Tiêu đề ---
        sheet.Cell(1, 1).Value = "Thời Khóa Biểu Chi Tiết";
        var titleRange = sheet.Range(1, 1, 1, DayHeaders.Length);
        titleRange.Merge().Style.Font.SetBold().Font.FontSize = 16;
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        sheet.Cell(1, DayHeaders.Length + 2).Value = "Thông tin Giảng viên";
        sheet.Range(1, DayHeaders.Length + 2, 1, DayHeaders.Length + 5).Merge().Style.Font.SetBold().Font.FontSize = 16;

        // --- 2. Tạo Header Lưới TKB (Dòng 2) ---
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

        // --- 3. Tạo Header Danh sách môn (Dòng 2, Cột I trở đi) ---
        int listStartCol = DayHeaders.Length + 2;
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

        // --- 4. Vẽ khung các Tiết học (Từ dòng 3) ---
        int startRow = 3;
        for (int period = 1; period < 10; period++)
        {
            int currentRow = startRow + (period - 1) * 2;

            // Chèn dòng nghỉ trưa giữa tiết 5 và 6
            if (period == 6)
            {
                var brCell = sheet.Cell(currentRow, 1);
                brCell.Value = "——— NGHỈ TRƯA (11:30 - 13:30) ———";
                var brRange = sheet.Range(currentRow, 1, currentRow, DayHeaders.Length);
                brRange.Merge().Style.Fill.BackgroundColor = XLColor.FromHtml("#1f5ca9");
                brRange.Style.Font.FontColor = XLColor.White;
                brRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                startRow++; // Đẩy các dòng sau xuống 1 ô
                currentRow = startRow + (period - 1) * 2;
            }

            var periodCell = sheet.Cell(currentRow, 1);
            periodCell.Value = period;
            // Lấy giờ học tương ứng với tiết (period - 1 vì mảng đếm từ số 0)
            sheet.Cell(currentRow + 1, 1).Value = PeriodTimes[period - 1];

            var periodRange = sheet.Range(currentRow, 1, currentRow + 1, 1);
            periodRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            periodRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            periodRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F6FA");
            periodRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Kẻ sẵn viền mờ cho các ô trống
            for (int c = 2; c <= DayHeaders.Length; c++)
            {
                sheet.Range(currentRow, c, currentRow + 1, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
        }

        // --- 5. ĐỔ DỮ LIỆU THỰC TẾ VÀO LƯỚI VÀ DANH SÁCH ---
        int listRow = 3;
        var choiceList = choices.ToList(); // Chuyển sang List để dùng chỉ số (index)

        for (int i = 0; i < choiceList.Count; i++)
        {
            var choice = choiceList[i];
            var course = choice.Course;
            var section = choice.Section;

            // Lấy màu xoay vòng từ mảng Pastel (nếu nhiều hơn 12 môn thì quay lại màu đầu tiên)
            var xlBgColor = PastelColors[i % PastelColors.Length];

            // 5.1 Đổ vào bảng thông tin bên phải
            sheet.Cell(listRow, listStartCol).Value = course.Name_VN;
            sheet.Cell(listRow, listStartCol + 1).Value = course.Credits;
            sheet.Cell(listRow, listStartCol + 2).Value = section.Lecturer ?? "";
            sheet.Cell(listRow, listStartCol + 3).Value = section.Group ?? "";

            var listRange = sheet.Range(listRow, listStartCol, listRow, listStartCol + 3);
            listRange.Style.Fill.BackgroundColor = xlBgColor;
            listRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            // Kẻ viền trong (viền dọc giữa các cột) cho bảng danh sách
            listRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            listRow++;

            // 5.2 Rải môn học vào lưới TKB bên trái (Thuật toán map tiết học)
            foreach (var day in section.ClassDays)
            {
                int colIndex = day.AttendingDay;

                // Tính dòng bắt đầu
                int rowIndex = 3 + (day.StartPeriod() - 1) * 2;
                if (day.StartPeriod() > 5) rowIndex++; // Nhảy qua dòng nghỉ trưa

                // Tính dòng kết thúc
                int endRow = rowIndex + (day.PeriodCount() * 2) - 1;

                var cell = sheet.Cell(rowIndex, colIndex);
                cell.Value = $"{course.Name_VN}\n{course.Code} ({section.Group})\nPhòng: {day.Room}";

                // Gộp ô, căn giữa, bọc text, tô màu
                var classRange = sheet.Range(rowIndex, colIndex, endRow, colIndex);
                classRange.Merge();
                classRange.Style.Alignment.WrapText = true;
                classRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                classRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                classRange.Style.Fill.BackgroundColor = xlBgColor;
                classRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick; // Viền đậm bao quanh môn học
            }
        }

        // --- 6. Căn chỉnh UI ---
        sheet.Columns().AdjustToContents();
        sheet.Column(1).Width = 12;
        for (int i = 2; i <= DayHeaders.Length; i++) sheet.Column(i).Width = 20; // Cột thứ rộng ra

        return wb;
    }
}