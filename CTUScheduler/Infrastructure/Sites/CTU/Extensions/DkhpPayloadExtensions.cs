using System.Collections.Generic;
using System.IO;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

namespace CTUScheduler.Infrastructure.Sites.CTU.Extensions;

public static class DkhpPayloadExtensions
{
    public static PlannedCourse ToPlannedCourse(this RawDkhpPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.HocPhanMa))
        {
            throw new InvalidDataException("Mapping failed: 'Mã học phần' bị trống hoặc null từ API.");
        }

        if (string.IsNullOrWhiteSpace(payload.HocPhanTen))
        {
            throw new InvalidDataException($"Mapping failed: 'Tên học phần' bị trống (Mã HP: {payload.HocPhanMa}).");
        }

        if (payload.SoTinChi < 0)
        {
            throw new InvalidDataException($"Mapping failed: Số tín chỉ không hợp lệ (Mã HP: {payload.HocPhanMa}).");
        }

        return new PlannedCourse(
            Code: payload.HocPhanMa,
            NameVn: payload.HocPhanTen,
            Credits: payload.SoTinChi,
            Group: payload.NhomHocPhanMa,
            ScheduleText: payload.ThoiKhoaBieu,
            LecturerName: payload.GiangVienTen,
            LecturerEmail: payload.GiangVienEmail,
            IsRegistered: payload.TrangThaiDangKy == 1
        );
    }
}