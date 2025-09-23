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
        public static Course ToCourse(this RawCourse rawCourse)
        {
            if (rawCourse.hoc_phan_info is null) return null!;
            try
            {
                Course course = new Course()
                {
                    Code = rawCourse.hoc_phan_info.dkmh_tu_dien_hoc_phan_ma,
                    Name_VN = rawCourse.hoc_phan_info.dkmh_tu_dien_hoc_phan_ten_vn,
                    Credit = rawCourse.hoc_phan_info.dkmh_tu_dien_hoc_phan_so_tin_chi,
                    TheorySessions = rawCourse.hoc_phan_info.dkmh_tu_dien_hoc_phan_so_tiet_ly_thuyet,
                    PracticalSessions = rawCourse.hoc_phan_info.dkmh_tu_dien_hoc_phan_so_tiet_thuc_hanh,
                };

                ConcurrentDictionary<string, CourseSection> courseDataDir = new();

                Parallel.ForEach(rawCourse.data, (rawCourseGroupData) =>
                {
                    var newClassDay = GetClassDayData(rawCourseGroupData);
                    courseDataDir.AddOrUpdate(
                            // is this group already exist?
                            rawCourseGroupData.dkmh_nhom_hoc_phan_ma,
                            // Not: Add new Group data
                            new CourseSection()
                            {
                                Key = rawCourseGroupData.key,
                                Code = rawCourseGroupData.dkmh_tu_dien_hoc_phan_ma,
                                Group = rawCourseGroupData.dkmh_nhom_hoc_phan_ma,
                                Lecturer = rawCourseGroupData.dkmh_tu_dien_giang_vien_ten_vn,
                                LecturerEmail = rawCourseGroupData.dkmh_tu_dien_giang_vien_email,
                                TotalStudents = rawCourseGroupData.dkmh_tu_dien_lop_hoc_phan_si_so,
                                RemainingStudents = rawCourseGroupData.si_so_con_lai,
                                ClassDays = newClassDay != null 
                                    ? new List<ClassDay>()
                                    {
                                        newClassDay
                                    }
                                    : new List<ClassDay>()
                            },
                            // Has group => Add new ClassDay into an existing group
                            (group, existing) =>
                            {
                               if (newClassDay != null)
                                    existing.ClassDays.Add(newClassDay);
                               return existing;
                            }

                        );
                });
                course.Sections  = new List<CourseSection>(courseDataDir.Values.OrderBy(x => x.Key).ToList());

                return course;
            }
            catch
            {
                return null!;
            }
            // local funtion
            ClassDay? GetClassDayData(RawCourseData rawCourseGroupData)
            {
                if (rawCourseGroupData.dkmh_thu_trong_tuan_ma == null
                    || rawCourseGroupData.tiet_hoc == null
                    || rawCourseGroupData.dkmh_tu_dien_phong_hoc_ten == null)
                    return null;
                
                return new ClassDay()
                {
                    AttendingDay = rawCourseGroupData.dkmh_thu_trong_tuan_ma.Value,
                    Period = rawCourseGroupData.tiet_hoc,
                    Room = rawCourseGroupData.dkmh_tu_dien_phong_hoc_ten
                };
            }
        }
    }
}
