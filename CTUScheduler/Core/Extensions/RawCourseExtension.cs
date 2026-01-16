using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using System.Linq;

namespace CTUScheduler.Core.Extensions
{
    public static class RawCourseExtension
    {
        public static Course? ToCourse(this RawCourse rawCourse)
        {
            if (rawCourse?.hoc_phan_info is null || rawCourse?.data is null) return null;

            var info = rawCourse.hoc_phan_info;

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
    }
}