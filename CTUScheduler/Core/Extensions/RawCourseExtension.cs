using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Extensions
{
    public static class RawCourseExtension
    {
        public static Course? ToCourse(this RawCourse rawCourse)
        {
            // 1. Nếu dữ liệu đầu vào rỗng hoặc thiếu info quan trọng -> Trả về null luôn
            if (rawCourse?.hoc_phan_info is null || rawCourse.data is null) return null;

            // Biến tạm cho ngắn gọn
            var info = rawCourse.hoc_phan_info;

            // 2. Tạo khung object Course
            var course = new Course()
            {
                Code = info.dkmh_tu_dien_hoc_phan_ma,
                Name_VN = info.dkmh_tu_dien_hoc_phan_ten_vn,
                Credits = info.dkmh_tu_dien_hoc_phan_so_tin_chi,
                TheorySessions = info.dkmh_tu_dien_hoc_phan_so_tiet_ly_thuyet,
                PracticalSessions = info.dkmh_tu_dien_hoc_phan_so_tiet_thuc_hanh,
            };
            
            course.Sections = rawCourse.data
                .GroupBy(d => d.dkmh_nhom_hoc_phan_ma)
                .Select(group =>
                {
                    var firstItem = group.First();

                    return new CourseSection
                    {
                        Key = firstItem.key,
                        Code = firstItem.dkmh_tu_dien_hoc_phan_ma,
                        Group = firstItem.dkmh_nhom_hoc_phan_ma,
                        Lecturer = firstItem.dkmh_tu_dien_giang_vien_ten_vn,
                        LecturerEmail = firstItem.dkmh_tu_dien_giang_vien_email,
                        TotalStudents = firstItem.dkmh_tu_dien_lop_hoc_phan_si_so,
                        RemainingStudents = firstItem.si_so_con_lai,

                        ClassDays = group
                            .Select(GetClassDayData) 
                            .Where(day => day is not null) 
                            .Cast<ClassDay>() 
                            .ToList()
                    };
                })
                .OrderBy(x => x.Key)
                .ToList();

            return course;
            
            static ClassDay? GetClassDayData(RawCourseData item)
            {
                // Kiểm tra kỹ dữ liệu đầu vào
                if (item.dkmh_thu_trong_tuan_ma is null
                    || item.tiet_hoc is null
                    || item.dkmh_tu_dien_phong_hoc_ten is null)
                    return null;

                return new ClassDay()
                {
                    AttendingDay = item.dkmh_thu_trong_tuan_ma.Value,
                    Period = item.tiet_hoc,
                    Room = item.dkmh_tu_dien_phong_hoc_ten
                };
            }
        }

        // public static Course ToCourse(this RawCourse rawCourse)
        // {
        //     if (rawCourse.hoc_phan_info is null) return null!;
        //     try
        //     {
        //         Course course = new Course()
        //         {
        //             Code = rawCourse.hoc_phan_info.dkmh_tu_dien_hoc_phan_ma,
        //             Name_VN = rawCourse.hoc_phan_info.dkmh_tu_dien_hoc_phan_ten_vn,
        //             Credits = rawCourse.hoc_phan_info.dkmh_tu_dien_hoc_phan_so_tin_chi,
        //             TheorySessions = rawCourse.hoc_phan_info.dkmh_tu_dien_hoc_phan_so_tiet_ly_thuyet,
        //             PracticalSessions = rawCourse.hoc_phan_info.dkmh_tu_dien_hoc_phan_so_tiet_thuc_hanh,
        //         };
        //
        //         ConcurrentDictionary<string, CourseSection> courseDataDir = new();
        //
        //         Parallel.ForEach(rawCourse.data, (rawCourseGroupData) =>
        //         {
        //             var newClassDay = GetClassDayData(rawCourseGroupData);
        //             courseDataDir.AddOrUpdate(
        //                     // is this group already exist?
        //                     rawCourseGroupData.dkmh_nhom_hoc_phan_ma,
        //                     // Not: Add new Group data
        //                     new CourseSection()
        //                     {
        //                         Key = rawCourseGroupData.key,
        //                         Code = rawCourseGroupData.dkmh_tu_dien_hoc_phan_ma,
        //                         Group = rawCourseGroupData.dkmh_nhom_hoc_phan_ma,
        //                         Lecturer = rawCourseGroupData.dkmh_tu_dien_giang_vien_ten_vn,
        //                         LecturerEmail = rawCourseGroupData.dkmh_tu_dien_giang_vien_email,
        //                         TotalStudents = rawCourseGroupData.dkmh_tu_dien_lop_hoc_phan_si_so,
        //                         RemainingStudents = rawCourseGroupData.si_so_con_lai,
        //                         ClassDays = newClassDay != null 
        //                             ? new List<ClassDay>()
        //                             {
        //                                 newClassDay
        //                             }
        //                             : new List<ClassDay>()
        //                     },
        //                     // Has group => Add new ClassDay into an existing group
        //                     (group, existing) =>
        //                     {
        //                        if (newClassDay != null)
        //                             existing.ClassDays.Add(newClassDay);
        //                        return existing;
        //                     }
        //
        //                 );
        //         });
        //         course.Sections  = new List<CourseSection>(courseDataDir.Values.OrderBy(x => x.Key).ToList());
        //
        //         return course;
        //     }
        //     catch
        //     {
        //         return null!;
        //     }
        //     
        //     // local funtion
        //     ClassDay? GetClassDayData(RawCourseData rawCourseGroupData)
        //     {
        //         if (rawCourseGroupData.dkmh_thu_trong_tuan_ma == null
        //             || rawCourseGroupData.tiet_hoc == null
        //             || rawCourseGroupData.dkmh_tu_dien_phong_hoc_ten == null)
        //             return null;
        //         
        //         return new ClassDay()
        //         {
        //             AttendingDay = rawCourseGroupData.dkmh_thu_trong_tuan_ma.Value,
        //             Period = rawCourseGroupData.tiet_hoc,
        //             Room = rawCourseGroupData.dkmh_tu_dien_phong_hoc_ten
        //         };
        //     }
        // }
    }
}