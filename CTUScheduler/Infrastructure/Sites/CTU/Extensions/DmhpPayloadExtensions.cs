using System.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

namespace CTUScheduler.Infrastructure.Sites.CTU.Extensions
{
    public static class DmhpPayloadExtensions
    {
        public static Course? ToCourse(this RawDmhpPayload rawDmhpPayload)
        {
            if (rawDmhpPayload?.HocPhanInfo is null || rawDmhpPayload?.Data is null) return null;

            var info = rawDmhpPayload.HocPhanInfo;

            var course = new Course()
            {
                Code = info.HocPhanMa,
                Name_VN = info.HocPhanTenVn,
                Credits = info.SoTinChi,
                TheorySessions = info.SoTietLyThuyet,
                PracticalSessions = info.SoTietThucHanh,
            };

            course.Sections = rawDmhpPayload.Data
                .GroupBy(d => d.NhomHocPhanMa)
                .Select(group =>
                {
                    var firstItem = group.First();

                    return new CourseSection
                    {
                        Key = firstItem.Key,
                        Code = firstItem.HocPhanMa,
                        Group = firstItem.NhomHocPhanMa,
                        Lecturer = firstItem.GiangVienTenVn,
                        LecturerEmail = firstItem.GiangVienEmail,
                        TotalStudents = firstItem.SiSo,
                        RemainingStudents = firstItem.SiSoConLai,

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

            static ClassDay? GetClassDayData(RawDmhpCourseData item)
            {
                if (item.ThuTrongTuanMa is null
                    || item.TietHoc is null
                    || item.PhongHocTen is null)
                    return null;

                return new ClassDay()
                {
                    AttendingDay = item.ThuTrongTuanMa.Value,
                    Period = item.TietHoc,
                    Room = item.PhongHocTen
                };
            }
        }
    }
}