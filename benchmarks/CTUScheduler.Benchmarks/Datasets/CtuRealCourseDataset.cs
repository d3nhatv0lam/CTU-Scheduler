using System;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Benchmarks.Datasets;

/// <summary>
/// Dữ liệu thực nghiệm thực tế trích xuất từ Cổng thông tin đào tạo MyCTUs (Đại học Cần Thơ)
/// Học kỳ 1, Năm học 2026-2027 cho 8 học phần ngành CNTT.
/// </summary>
public static class CtuRealCourseDataset
{
    public static List<IReadOnlyList<SectionChoice>> GetCourseSets(int courseCount)
    {
        var allCourses = GetAll8Courses();
        return allCourses.GetRange(0, Math.Clamp(courseCount, 4, 8));
    }

    public static List<IReadOnlyList<SectionChoice>> GetAll8Courses()
    {
        return new List<IReadOnlyList<SectionChoice>>
        {

            // CT173: Kiến trúc máy tính (3 TC - 7 nhóm)
            new List<SectionChoice>
            {
                CreateChoice("CT173", "Kiến trúc máy tính", 3, "04", "Giảng viên 01", (day: 2, period: "1234---------", room: "011/K3")),
                CreateChoice("CT173", "Kiến trúc máy tính", 3, "06", "Giảng viên 02", (day: 5, period: "1234---------", room: "011/K3")),
                CreateChoice("CT173", "Kiến trúc máy tính", 3, "05", "Giảng viên 03", (day: 5, period: "1234---------", room: "304/D1")),
                CreateChoice("CT173", "Kiến trúc máy tính", 3, "02", "Giảng viên 04", (day: 6, period: "-----6789----", room: "305/D2")),
                CreateChoice("CT173", "Kiến trúc máy tính", 3, "01", "Giảng viên 05", (day: 4, period: "-----678-----", room: "016/K3")),
                CreateChoice("CT173", "Kiến trúc máy tính", 3, "03", "Giảng viên 06", (day: 5, period: "-----6789----", room: "304/D1")),
                CreateChoice("CT173", "Kiến trúc máy tính", 3, "H01", "Giảng viên 07", (day: 2, period: "-----678-----", room: "203HA5"), (day: 3, period: "-----678-----", room: "203HA5"))
            },

            // CT174: Phân tích và thiết kế thuật toán (3 TC - 4 nhóm)
            new List<SectionChoice>
            {
                CreateChoice("CT174", "Phân tích và thiết kế thuật toán", 3, "02", "Giảng viên 01", (day: 3, period: "1234---------", room: "303/C1")),
                CreateChoice("CT174", "Phân tích và thiết kế thuật toán", 3, "03", "Giảng viên 02", (day: 5, period: "1234---------", room: "303/C1")),
                CreateChoice("CT174", "Phân tích và thiết kế thuật toán", 3, "01", "Giảng viên 03", (day: 4, period: "1234---------", room: "303/C1")),
                CreateChoice("CT174", "Phân tích và thiết kế thuật toán", 3, "04", "Giảng viên 04", (day: 5, period: "1234---------", room: "206/D1"))
            },

            // CT175: Lý thuyết đồ thị (3 TC - 5 nhóm)
            new List<SectionChoice>
            {
                CreateChoice("CT175", "Lý thuyết đồ thị", 3, "03", "Giảng viên 01", (day: 3, period: "-----6789----", room: "306/D1")),
                CreateChoice("CT175", "Lý thuyết đồ thị", 3, "01", "Giảng viên 02", (day: 3, period: "-----6789----", room: "303/C1")),
                CreateChoice("CT175", "Lý thuyết đồ thị", 3, "02", "Giảng viên 03", (day: 5, period: "-2345--------", room: "201/K3")),
                CreateChoice("CT175", "Lý thuyết đồ thị", 3, "04", "Giảng viên 04", (day: 5, period: "-----6789----", room: "307/D1")),
                CreateChoice("CT175", "Lý thuyết đồ thị", 3, "06", "Giảng viên 05", (day: 5, period: "1234---------", room: "203/K3"))
            },

            // CT176: Lập trình hướng đối tượng (3 TC - 8 nhóm)
            new List<SectionChoice>
            {
                CreateChoice("CT176", "Lập trình hướng đối tượng", 3, "03", "Giảng viên 01", (day: 3, period: "-----6789----", room: "302/C1")),
                CreateChoice("CT176", "Lập trình hướng đối tượng", 3, "02", "Giảng viên 02", (day: 3, period: "1234---------", room: "206/D1")),
                CreateChoice("CT176", "Lập trình hướng đối tượng", 3, "01", "Giảng viên 03", (day: 2, period: "-----6789----", room: "206/D1")),
                CreateChoice("CT176", "Lập trình hướng đối tượng", 3, "09", "Giảng viên 04", (day: 3, period: "1234---------", room: "307/D1")),
                CreateChoice("CT176", "Lập trình hướng đối tượng", 3, "06", "Giảng viên 05", (day: 4, period: "1234---------", room: "207/D2")),
                CreateChoice("CT176", "Lập trình hướng đối tượng", 3, "05", "Giảng viên 06", (day: 2, period: "1234---------", room: "301/D1")),
                CreateChoice("CT176", "Lập trình hướng đối tượng", 3, "04", "Giảng viên 07", (day: 4, period: "1234---------", room: "302/C1")),
                CreateChoice("CT176", "Lập trình hướng đối tượng", 3, "07", "Giảng viên 08", (day: 3, period: "123----------", room: "301/C1"), (day: 5, period: "1234---------", room: "203/DI"))
            },

            // CT178: Nguyên lý hệ điều hành (3 TC - 4 nhóm)
            new List<SectionChoice>
            {
                CreateChoice("CT178", "Nguyên lý hệ điều hành", 3, "01", "Giảng viên 01", (day: 3, period: "-----6789----", room: "011/K3")),
                CreateChoice("CT178", "Nguyên lý hệ điều hành", 3, "02", "Giảng viên 02", (day: 4, period: "1234---------", room: "403/D1")),
                CreateChoice("CT178", "Nguyên lý hệ điều hành", 3, "03", "Giảng viên 03", (day: 4, period: "1234---------", room: "405/D2")),
                CreateChoice("CT178", "Nguyên lý hệ điều hành", 3, "04", "Giảng viên 04", (day: 7, period: "-----6789----", room: "202/C1"))
            },

            // TN012: Đại số tuyến tính và hình học (4 TC - 4 nhóm)
            new List<SectionChoice>
            {
                CreateChoice("TN012", "Đại số tuyến tính và hình học", 4, "03", "Giảng viên 01", (day: 5, period: "12-----------", room: "109/A3"), (day: 6, period: "-----6789----", room: "103/A3")),
                CreateChoice("TN012", "Đại số tuyến tính và hình học", 4, "01", "Giảng viên 02", (day: 6, period: "123----------", room: "114/B1"), (day: 7, period: "123----------", room: "103/KH")),
                CreateChoice("TN012", "Đại số tuyến tính và hình học", 4, "02", "Giảng viên 03", (day: 2, period: "-----678-----", room: "3014T2"), (day: 5, period: "-----678-----", room: "3014T2")),
                CreateChoice("TN012", "Đại số tuyến tính và hình học", 4, "M01", "Giảng viên 04", (day: 2, period: "123----------", room: "304ATL"), (day: 6, period: "123----------", room: "312ATL"))
            },

            // ML014: Triết học Mác - Lênin (3 TC - 30 nhóm)
            new List<SectionChoice>
            {
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "M01", "Giảng viên 01", (day: 4, period: "1234---------", room: "313ATL")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "S01", "Giảng viên 02", (day: 5, period: "-2345--------", room: "STA703"), (day: 5, period: "-----678-----", room: "STA703")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F26", "Giảng viên 03", (day: 2, period: "---45--------", room: "107/C1"), (day: 5, period: "-------89----", room: "106/C1")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F25", "Giảng viên 04", (day: 4, period: "---45--------", room: "HT/KSP"), (day: 7, period: "-----67------", room: "HT/KSP")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F24", "Giảng viên 05", (day: 3, period: "---45--------", room: "HT/KSP"), (day: 4, period: "---45--------", room: "102/KT")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F23", "Giảng viên 06", (day: 3, period: "-----67------", room: "102/KT"), (day: 4, period: "-----678-----", room: "103/KH")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F22", "Giảng viên 07", (day: 3, period: "---45--------", room: "102/KT"), (day: 4, period: "-----678-----", room: "201/MT")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F21", "Giảng viên 08", (day: 4, period: "-----678-----", room: "102/KH"), (day: 5, period: "-----67------", room: "106/C1")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F27", "Giảng viên 09", (day: 4, period: "---45--------", room: "107/C1"), (day: 6, period: "123----------", room: "HT/KSP")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F20", "Giảng viên 10", (day: 4, period: "123----------", room: "102/KH"), (day: 6, period: "-----67------", room: "102/KH")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F18", "Giảng viên 11", (day: 5, period: "-----678-----", room: "HTR/DB"), (day: 6, period: "---45--------", room: "107/C1")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F17", "Giảng viên 12", (day: 4, period: "---45--------", room: "102/KH"), (day: 5, period: "123----------", room: "103/KH")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F16", "Giảng viên 13", (day: 2, period: "---45--------", room: "201/MT"), (day: 5, period: "123----------", room: "HTR/DB")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F15", "Giảng viên 14", (day: 3, period: "123----------", room: "107/C1"), (day: 5, period: "-----67------", room: "107/C1")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F14", "Giảng viên 15", (day: 2, period: "---45--------", room: "HT/KSP"), (day: 5, period: "---45--------", room: "106/C1")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F13", "Giảng viên 16", (day: 2, period: "123----------", room: "103/KH"), (day: 5, period: "---45--------", room: "202/KH")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F12", "Giảng viên 17", (day: 5, period: "---45--------", room: "102/KT"), (day: 7, period: "12-----------", room: "HT/KSP")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F11", "Giảng viên 18", (day: 3, period: "-----67------", room: "103/KH"), (day: 7, period: "123----------", room: "203/KH")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F10", "Giảng viên 19", (day: 2, period: "---45--------", room: "102/KT"), (day: 6, period: "123----------", room: "106/C1")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F09", "Giảng viên 20", (day: 5, period: "---45--------", room: "HTR/DB"), (day: 7, period: "---45--------", room: "102/KT")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F08", "Giảng viên 21", (day: 2, period: "-----678-----", room: "HTR/DB"), (day: 3, period: "-----67------", room: "106/C1")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F07", "Giảng viên 22", (day: 3, period: "-------89----", room: "102/KT"), (day: 5, period: "123----------", room: "106/C1")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F06", "Giảng viên 23", (day: 2, period: "123----------", room: "102/KH"), (day: 6, period: "---45--------", room: "102/KH")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F05", "Giảng viên 24", (day: 3, period: "-------89----", room: "106/C1"), (day: 7, period: "-----678-----", room: "106/C1")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F04", "Giảng viên 25", (day: 5, period: "-------89----", room: "102/KH"), (day: 7, period: "--345--------", room: "HT/KSP")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F03", "Giảng viên 26", (day: 4, period: "---45--------", room: "106/C1"), (day: 6, period: "---45--------", room: "HTR/DB")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F02", "Giảng viên 27", (day: 3, period: "---45--------", room: "201/KH"), (day: 6, period: "123----------", room: "103/KH")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "F01", "Giảng viên 28", (day: 5, period: "-----67------", room: "102/KH"), (day: 6, period: "123----------", room: "102/KH")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "02", "Giảng viên 29", (day: 2, period: "---45--------", room: "106/C1"), (day: 5, period: "123----------", room: "203/KH")),
                CreateChoice("ML014", "Triết học Mác - Lênin", 3, "01", "Giảng viên 30", (day: 5, period: "---45--------", room: "107/C1"), (day: 6, period: "---45--------", room: "106/C1"))
            },

            // TN010: Xác suất thống kê (3 TC - 39 nhóm)
            new List<SectionChoice>
            {
                CreateChoice("TN010", "Xác suất thống kê", 3, "03", "Giảng viên 01", (day: 7, period: "1234---------", room: "206/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "S02", "Giảng viên 02", (day: 6, period: "-2345--------", room: "STA701"), (day: 6, period: "-----678-----", room: "STA701")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F03", "Giảng viên 03", (day: 6, period: "-----6789----", room: "207/C1")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "04", "Giảng viên 04", (day: 7, period: "-----6789----", room: "206/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "M02", "Giảng viên 05", (day: 6, period: "-----6789----", room: "246RLC")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "S01", "Giảng viên 06", (day: 3, period: "-2345--------", room: "STA701"), (day: 3, period: "-----678-----", room: "STA701")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F04", "Giảng viên 07", (day: 2, period: "-----6789----", room: "101/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F01", "Giảng viên 08", (day: 2, period: "-----6789----", room: "207/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "06", "Giảng viên 09", (day: 5, period: "-----6789----", room: "105/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "05", "Giảng viên 10", (day: 3, period: "-----6789----", room: "2024T2")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F02", "Giảng viên 11", (day: 2, period: "123----------", room: "101/KH"), (day: 3, period: "---45--------", room: "101/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "D02", "Giảng viên 12", (day: 5, period: "--345--------", room: "206/KH"), (day: 6, period: "-----6789----", room: "206/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "B02", "Giảng viên 13", (day: 4, period: "-----678-----", room: "210/KH"), (day: 7, period: "1234---------", room: "204/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "B01", "Giảng viên 14", (day: 2, period: "-----678-----", room: "206/KH"), (day: 7, period: "-----6789----", room: "111/B1")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "01", "Giảng viên 15", (day: 3, period: "-------89----", room: "302/D1"), (day: 5, period: "---45--------", room: "208/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "02", "Giảng viên 16", (day: 4, period: "---45--------", room: "211/B1"), (day: 6, period: "---45--------", room: "108/B1")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "B03", "Giảng viên 17", (day: 5, period: "-----6789----", room: "210/KH"), (day: 6, period: "123----------", room: "210/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "D01", "Giảng viên 18", (day: 5, period: "1234---------", room: "210/KH"), (day: 6, period: "-----678-----", room: "210/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "D03", "Giảng viên 19", (day: 5, period: "-----678-----", room: "207/C1"), (day: 6, period: "123----------", room: "103/MT")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "D04", "Giảng viên 20", (day: 2, period: "123----------", room: "207/KH"), (day: 3, period: "-----6789----", room: "212/B1")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F05", "Giảng viên 21", (day: 2, period: "---45--------", room: "401/D2"), (day: 6, period: "-----678-----", room: "212/B1")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F23", "Giảng viên 22", (day: 5, period: "-----678-----", room: "208/KH"), (day: 6, period: "-----67------", room: "208/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F18", "Giảng viên 23", (day: 2, period: "-----67------", room: "208/KH"), (day: 3, period: "-----67------", room: "208/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F14", "Giảng viên 24", (day: 5, period: "---45--------", room: "102/KH"), (day: 6, period: "-----678-----", room: "101/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F13", "Giảng viên 25", (day: 2, period: "-----678-----", room: "102/KH"), (day: 3, period: "---45--------", room: "102/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F12", "Giảng viên 26", (day: 2, period: "123----------", room: "206/KH"), (day: 6, period: "---45--------", room: "207/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F11", "Giảng viên 27", (day: 4, period: "---45--------", room: "208/C2"), (day: 5, period: "123----------", room: "301/D2")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F10", "Giảng viên 28", (day: 5, period: "-----67------", room: "103/B1"), (day: 6, period: "-----678-----", room: "303/MT")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F15", "Giảng viên 29", (day: 4, period: "---45--------", room: "101/KH"), (day: 6, period: "-----678-----", room: "103/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F16", "Giảng viên 30", (day: 2, period: "12-----------", room: "209/B1"), (day: 4, period: "12-----------", room: "101/C2")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F17", "Giảng viên 31", (day: 2, period: "-------89----", room: "208/KH"), (day: 3, period: "-------89----", room: "207/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F09", "Giảng viên 32", (day: 4, period: "123----------", room: "103/KH"), (day: 5, period: "---45--------", room: "101/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F19", "Giảng viên 33", (day: 2, period: "---45--------", room: "208/KH"), (day: 5, period: "12-----------", room: "208/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F20", "Giảng viên 34", (day: 2, period: "--345--------", room: "212/B1"), (day: 5, period: "12-----------", room: "120ATL")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F22", "Giảng viên 35", (day: 3, period: "--345--------", room: "206/C1"), (day: 4, period: "---45--------", room: "206/C1")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F08", "Giảng viên 36", (day: 2, period: "---45--------", room: "108/B1"), (day: 3, period: "123----------", room: "103/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "M01", "Giảng viên 37", (day: 2, period: "-----67------", room: "221RLC"), (day: 4, period: "--345--------", room: "245RLC")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F07", "Giảng viên 38", (day: 3, period: "---45--------", room: "202/KH"), (day: 5, period: "-----678-----", room: "101/KH")),
                CreateChoice("TN010", "Xác suất thống kê", 3, "F06", "Giảng viên 39", (day: 5, period: "123----------", room: "102/KH"), (day: 6, period: "---45--------", room: "103/KH"))
            },

        };
    }

    private static SectionChoice CreateChoice(
        string code, string name, int credits, string group, string lecturer,
        params (int day, string period, string room)[] sessions)
    {
        var classDays = new List<ClassDay>();
        foreach (var (day, period, room) in sessions)
        {
            classDays.Add(new ClassDay(day, period, room));
        }

        var section = new CourseSection(
            IsCancelled: false,
            Key: Math.Abs(string.Concat(code, "_", group).GetHashCode()),
            Code: code,
            Group: group,
            Lecturer: lecturer,
            LecturerEmail: $"{code.ToLower()}@ctu.edu.vn",
            TotalStudents: 40,
            RemainingStudents: 10,
            ClassDays: classDays
        );

        var course = new Course(code, name, credits, 30, 0, new[] { section });
        return new SectionChoice(course, section);
    }
}
