using System;
using System.Collections.Generic;
using CTUScheduler.Infrastructure.Sites.CTU.Extensions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;
using FluentAssertions;
using Xunit;

namespace CTUScheduler.Tests.Infrastructure.Sites.CTU;

public class TtHocPhiPayLoadExtensionsTests
{
    [Fact]
    public void ToSummary_ValidPayload_ShouldParseSummaryCorrectly()
    {
        var payload = new RawThongTinHocPhiPayload(
            ThongTinTinhHocPhi: "15/09/2026",
            ChiTietHocPhi: new List<List<RawHocPhiTextValue>>
            {
                new()
                {
                    new("Tổng cộng"),
                    new(""),
                    new("5,000,000 đ")
                },
                new()
                {
                    new("Bảo hiểm y tế"),
                    new(""),
                    new("800,000 đ")
                },
                new()
                {
                    new("Tổng cộng các khoản phải đóng"),
                    new(""),
                    new("5,800,000 đ")
                },
                new()
                {
                    new("Tổng cộng đã đóng"),
                    new(""),
                    new("5,800,000 đ")
                }
            },
            GhiChu: new RawHocPhiHaiCot(
                CotTrai: new List<RawHocPhiTextValue>(),
                CotPhai: new List<RawHocPhiTextValue>
                {
                    new("Học phần đại cương: 350,000 đ / tín chỉ (miễn giảm 0 đ)"),
                    new("Học phần cơ sở ngành / chuyên ngành: 450,000 đ / tín chỉ (miễn giảm 50,000 đ)")
                }
            ),
            ThongTin3: new List<RawHocPhiTextValue>
            {
                new("Hạn chót đóng học phí: 30/10/2026")
            }
        );

        var summary = payload.ToSummary();

        summary.Should().NotBeNull();
        summary!.CalculationDate.Should().Be(new DateOnly(2026, 9, 15));
        summary.SemesterTuitionFee.Should().Be(5000000);
        summary.HealthInsuranceFee.Should().Be(800000);
        summary.TotalPayableAmount.Should().Be(5800000);
        summary.TotalPaidAmount.Should().Be(5800000);
        summary.PaymentDeadline.Should().Be(new DateOnly(2026, 10, 30));
        summary.GeneralCreditFee.Should().NotBeNull();
        summary.GeneralCreditFee!.FeePerCredit.Should().Be(350000);
        summary.MajorCreditFee.Should().NotBeNull();
        summary.MajorCreditFee!.FeePerCredit.Should().Be(450000);
        summary.MajorCreditFee!.DiscountPerCredit.Should().Be(50000);
    }
}
