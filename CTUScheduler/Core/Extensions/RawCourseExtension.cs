using Avalonia.Controls.ApplicationLifetimes;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Extensions
{
    public static class RawCourseExtension
    {
        public static Course ToCourse(this RawCourse rawCourse)
        {
            if (rawCourse.hoc_phan_info == null) return null!;
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

                ConcurrentDictionary<string, CourseData> courseDataDir = new();

                Parallel.ForEach(rawCourse.data, (rawCourseGroupData) =>
                {
                    courseDataDir.AddOrUpdate(
                            // is this group already exist?
                            rawCourseGroupData.dkmh_nhom_hoc_phan_ma,
                            // Not: Add new Group data
                            new CourseData()
                            {
                                Key = rawCourseGroupData.key,
                                Code = rawCourseGroupData.dkmh_tu_dien_hoc_phan_ma,
                                Group = rawCourseGroupData.dkmh_nhom_hoc_phan_ma,
                                Lecturer = rawCourseGroupData.dkmh_tu_dien_giang_vien_ten_vn,
                                LecturerEmail = rawCourseGroupData.dkmh_tu_dien_giang_vien_email,
                                TotalStudents = rawCourseGroupData.dkmh_tu_dien_lop_hoc_phan_si_so,
                                RemainingStudents = rawCourseGroupData.si_so_con_lai,
                                ClassDayDatas = new List<ClassDayData>()
                                {
                                    GetClassDayData(rawCourseGroupData)
                                }
                            },
                            // Has group => Add new ClassDayData into existing group
                            (group, existing) =>
                            {
                                var newClassDay = GetClassDayData(rawCourseGroupData);
                                existing.ClassDayDatas.Add(newClassDay);
                                return existing;
                            }

                        );
                });
                course.Sections  = new List<CourseData>(courseDataDir.Values.OrderBy(x => x.Key).ToList());

                return course;
            }
            catch
            {
                return null!;
            }
            // local funtion
            ClassDayData GetClassDayData(RawCourseData rawCourseGroupData)
            {
                return new ClassDayData()
                {
                    AttendingDay = rawCourseGroupData.dkmh_thu_trong_tuan_ma,
                    Period = rawCourseGroupData.tiet_hoc,
                    Room = rawCourseGroupData.dkmh_tu_dien_phong_hoc_ten
                };
            }
        }
    }
}
