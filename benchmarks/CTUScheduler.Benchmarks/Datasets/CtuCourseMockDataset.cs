using System;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Benchmarks.Datasets;

public static class CtuCourseMockDataset
{
    /// <summary>
    /// Lấy danh sách các tập nhóm lớp theo số lượng môn học cần thực nghiệm (từ 4 đến 8 môn).
    /// </summary>
    public static List<IReadOnlyList<SectionChoice>> GetCourseSets(int courseCount)
    {
        var allCourses = GetAll8Courses();
        return allCourses.GetRange(0, Math.Clamp(courseCount, 4, 8));
    }
    
    public static List<IReadOnlyList<SectionChoice>> GetAll8Courses()
    {
        return new List<IReadOnlyList<SectionChoice>>
        {
            //CT174: Toán rời rạc (3 TC - 3 nhóm lý thuyết)
            new List<SectionChoice>
            {
                CreateChoice("CT174", "Toán rời rạc", 3, "01", "TS. Nguyễn Văn A",
                    (day: 2, period: "123-------", room: "C1-101")),
                CreateChoice("CT174", "Toán rời rạc", 3, "02", "TS. Nguyễn Văn A",
                    (day: 3, period: "-----678--", room: "C1-102")),
                CreateChoice("CT174", "Toán rời rạc", 3, "03", "ThS. Lê Thị B",
                    (day: 4, period: "123-------", room: "DI-201"))
            },

            // CT176: Cấu trúc dữ liệu (3 TC - 4 nhóm: 1 buổi Lý thuyết + 1 buổi Thực hành)
            new List<SectionChoice>
            {
                //[trùng lịch nhóm 01 sáng Thứ 2 với CT174-01 để kiểm thử Pruning]
                CreateChoice("CT176", "Cấu trúc dữ liệu", 3, "01", "TS. Trần Văn C",
                    (day: 2, period: "123-------", room: "C1-201"),
                    (day: 5, period: "-----678--", room: "PM-DI01")),

                CreateChoice("CT176", "Cấu trúc dữ liệu", 3, "02", "TS. Trần Văn C",
                    (day: 2, period: "-----678--", room: "C1-201"),
                    (day: 6, period: "123-------", room: "PM-DI01")),

                CreateChoice("CT176", "Cấu trúc dữ liệu", 3, "03", "ThS. Hoàng Văn D",
                    (day: 3, period: "123-------", room: "DI-101"),
                    (day: 5, period: "123-------", room: "PM-DI02")),

                CreateChoice("CT176", "Cấu trúc dữ liệu", 3, "04", "ThS. Hoàng Văn D",
                    (day: 4, period: "-----678--", room: "DI-101"),
                    (day: 7, period: "123-------", room: "PM-DI02"))
            },

            // CT175: Lập trình hướng đối tượng (3 TC - 4 nhóm: LT + TH)
            new List<SectionChoice>
            {
                CreateChoice("CT175", "Lập trình hướng đối tượng", 3, "01", "TS. Đỗ Thị E",
                    (day: 3, period: "-----678--", room: "C1-301"),
                    (day: 6, period: "-----678--", room: "PM-DI03")),

                CreateChoice("CT175", "Lập trình hướng đối tượng", 3, "02", "TS. Đỗ Thị E",
                    (day: 4, period: "123-------", room: "C1-301"),
                    (day: 7, period: "-----678--", room: "PM-DI03")),

                CreateChoice("CT175", "Lập trình hướng đối tượng", 3, "03", "ThS. Phạm Văn F",
                    (day: 2, period: "123-------", room: "DI-102"),
                    (day: 5, period: "-----678--", room: "PM-DI04")),

                CreateChoice("CT175", "Lập trình hướng đối tượng", 3, "04", "ThS. Phạm Văn F",
                    (day: 3, period: "123-------", room: "DI-102"),
                    (day: 6, period: "123-------", room: "PM-DI04"))
            },

            // CT180: Cơ sở dữ liệu (3 TC - 5 nhóm: LT + TH)
            new List<SectionChoice>
            {
                CreateChoice("CT180", "Cơ sở dữ liệu", 3, "01", "TS. Vũ Thị G",
                    (day: 2, period: "-----678--", room: "C1-202"),
                    (day: 4, period: "123-------", room: "PM-DI01")),
                CreateChoice("CT180", "Cơ sở dữ liệu", 3, "02", "TS. Vũ Thị G",
                    (day: 3, period: "123-------", room: "C1-202"),
                    (day: 5, period: "-----678--", room: "PM-DI01")),
                CreateChoice("CT180", "Cơ sở dữ liệu", 3, "03", "ThS. Ngô Văn H",
                    (day: 4, period: "-----678--", room: "DI-202"),
                    (day: 6, period: "123-------", room: "PM-DI02")),
                CreateChoice("CT180", "Cơ sở dữ liệu", 3, "04", "ThS. Ngô Văn H",
                    (day: 5, period: "123-------", room: "DI-202"),
                    (day: 7, period: "-----678--", room: "PM-DI02")),
                CreateChoice("CT180", "Cơ sở dữ liệu", 3, "05", "ThS. Ngô Văn H",
                    (day: 6, period: "-----678--", room: "DI-202"),
                    (day: 2, period: "123-------", room: "PM-DI03"))
            },

            // CT112: Mạng máy tính (3 TC - 4 nhóm: LT + TH)
            new List<SectionChoice>
            {
                CreateChoice("CT112", "Mạng máy tính", 3, "01", "TS. Bùi Văn I",
                    (day: 3, period: "-----678--", room: "DI-103"),
                    (day: 5, period: "123-------", room: "LabMạng")),
                CreateChoice("CT112", "Mạng máy tính", 3, "02", "TS. Bùi Văn I",
                    (day: 4, period: "123-------", room: "DI-103"),
                    (day: 6, period: "-----678--", room: "LabMạng")),
                CreateChoice("CT112", "Mạng máy tính", 3, "03", "ThS. Đặng Thị K",
                    (day: 2, period: "-----678--", room: "C1-103"),
                    (day: 7, period: "123-------", room: "LabMạng")),
                CreateChoice("CT112", "Mạng máy tính", 3, "04", "ThS. Đặng Thị K",
                    (day: 5, period: "-----678--", room: "C1-103"),
                    (day: 3, period: "123-------", room: "LabMạng"))
            },

            // CT240: Công nghệ phần mềm (3 TC - 4 nhóm lý thuyết)
            new List<SectionChoice>
            {
                CreateChoice("CT240", "Công nghệ phần mềm", 3, "01", "TS. Nguyễn Thanh Khoa",
                    (day: 3, period: "123-------", room: "DI-203")),
                CreateChoice("CT240", "Công nghệ phần mềm", 3, "02", "TS. Nguyễn Thanh Khoa",
                    (day: 4, period: "-----678--", room: "DI-203")),
                CreateChoice("CT240", "Công nghệ phần mềm", 3, "03", "ThS. Mai Văn L",
                    (day: 5, period: "123-------", room: "C1-302")),
                CreateChoice("CT240", "Công nghệ phần mềm", 3, "04", "ThS. Mai Văn L",
                    (day: 6, period: "-----678--", room: "C1-302"))
            },

            // CT178: An toàn thông tin (3 TC - 3 nhóm lý thuyết)
            new List<SectionChoice>
            {
                CreateChoice("CT178", "An toàn thông tin", 3, "01", "TS. Trịnh Văn M",
                    (day: 2, period: "123-------", room: "DI-104")),
                CreateChoice("CT178", "An toàn thông tin", 3, "02", "TS. Trịnh Văn M",
                    (day: 4, period: "123-------", room: "DI-104")),
                CreateChoice("CT178", "An toàn thông tin", 3, "03", "ThS. Phan Thị N",
                    (day: 6, period: "-----678--", room: "C1-203"))
            },

            // CT188: Phát triển ứng dụng Web (3 TC - 5 nhóm: LT + TH)
            new List<SectionChoice>
            {
                CreateChoice("CT188", "Phát triển ứng dụng Web", 3, "01", "TS. Hồ Văn P",
                    (day: 3, period: "-----678--", room: "DI-204"),
                    (day: 5, period: "123-------", room: "PM-DI03")),
                CreateChoice("CT188", "Phát triển ứng dụng Web", 3, "02", "TS. Hồ Văn P",
                    (day: 4, period: "-----678--", room: "DI-204"),
                    (day: 6, period: "123-------", room: "PM-DI03")),
                CreateChoice("CT188", "Phát triển ứng dụng Web", 3, "03", "ThS. Lâm Văn Q",
                    (day: 2, period: "-----678--", room: "C1-104"),
                    (day: 7, period: "-----678--", room: "PM-DI04")),
                CreateChoice("CT188", "Phát triển ứng dụng Web", 3, "04", "ThS. Lâm Văn Q",
                    (day: 5, period: "-----678--", room: "C1-104"),
                    (day: 3, period: "123-------", room: "PM-DI04")),
                CreateChoice("CT188", "Phát triển ứng dụng Web", 3, "05", "ThS. Lâm Văn Q",
                    (day: 6, period: "-----678--", room: "C1-104"),
                    (day: 4, period: "123-------", room: "PM-DI04"))
            }
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
