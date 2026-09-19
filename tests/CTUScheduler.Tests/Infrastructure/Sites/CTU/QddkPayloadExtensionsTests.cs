using System;
using System.Collections.Generic;
using CTUScheduler.Infrastructure.Sites.CTU.Extensions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;
using FluentAssertions;
using Xunit;

namespace CTUScheduler.Tests.Infrastructure.Sites.CTU;

public class QddkPayloadExtensionsTests
{
    [Fact]
    public void ToRegistrationInformation_ValidPayload_ParsesMaxCreditAndPeriod()
    {
        var payload = new RawQddkPayload(
            NamHoc: 2026,
            HocKy: "1",
            DanhSachQuyDinh: new List<RawQddkQuyDinh>
            {
                new(
                    CotTrai: new List<RawQddkQuyDinhData>
                    {
                        new("Số tín chỉ tối đa được đăng ký:", new List<string>())
                    },
                    CotPhai: new List<RawQddkQuyDinhData>
                    {
                        new("", new List<string> { "25" })
                    }
                ),
                new(
                    CotTrai: new List<RawQddkQuyDinhData>
                    {
                        new("Thời gian đăng ký: từ 01/09/2026 đến 15/09/2026", new List<string>())
                    },
                    CotPhai: new List<RawQddkQuyDinhData>()
                ),
                new(
                    CotTrai: new List<RawQddkQuyDinhData>(),
                    CotPhai: new List<RawQddkQuyDinhData>
                    {
                        new("Nhóm 1: Khoa Công Nghệ Thông Tin", new List<string> { "Nhóm 1" }),
                        new("Nhóm 2: Khoa Kinh Tế", new List<string> { "Nhóm 2" })
                    }
                )
            },
            DanhSachThoiGianDangKy: new List<List<RawQddkThoiGianDangKyItem>>
            {
                new()
                {
                    new("1", "1", null),
                    new("Khóa 49", "1", null),
                    new("05/09/2026 08:00:00", "1", null),
                    new("10/09/2026 17:00:00", "1", null),
                    new("Nhóm 1", "1", null)
                }
            }
        );

        var result = payload.ToRegistrationInformation(userKey: "Khóa 49", userUnit: "Khoa Công Nghệ Thông Tin");

        result.Should().NotBeNull();
        result.AcademicYear.Should().Be(2026);
        result.Semester.Should().Be("1");
        result.MaxCreditPerSemester.Should().Be(25);
        result.Period.Should().Contain("Thời gian đăng ký");
        result.Groups.Should().HaveCount(2);
        result.UserPeriod.Should().NotBeNull();
        result.UserPeriod!.StartDate.Should().Be(new DateTime(2026, 9, 5, 8, 0, 0));
        result.UserPeriod!.EndDate.Should().Be(new DateTime(2026, 9, 10, 17, 0, 0));
    }
}
