using System;
using System.Globalization;
using System.Text.RegularExpressions;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Extensions;

public static partial class TtHocPhiPayLoadExtensions
{
    [GeneratedRegex(@"(\d{2}/\d{2}/\d{4})")]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"([\d,]+)\s*đ", RegexOptions.IgnoreCase)]
    private static partial Regex FeeRegex();


    public static TuitionFeeSummary? ToSummary(this RawThongTinHocPhiPayload payload, ILogger? logger = null)
    {
        decimal? semesterTuitionFee = null;
        decimal? healthInsuranceFee = 0;
        decimal? previousSemesterDebt = 0;
        decimal? totalPayableAmount = null;
        decimal? totalPaidAmount = null;
        DateOnly? calculationDate = null;

        if (DateOnly.TryParseExact(
                payload.ThongTinTinhHocPhi,
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            calculationDate = parsedDate;
        }
        else
        {
            logger?.LogWarning("Không có thông tin thời gian bắt đầu học phí hoặc định dạng không hợp lệ: {Value}",
                payload.ThongTinTinhHocPhi);
        }


        if (payload.ChiTietHocPhi is not null)
        {
            foreach (var row in payload.ChiTietHocPhi)
            {
                if (row is [{ Value: { } rawTitle }, _, { Value: { } moneyString }, ..])
                {
                    var titleSpan = rawTitle.AsSpan().Trim();

                    if (titleSpan.Equals("tổng cộng", StringComparison.OrdinalIgnoreCase))
                        semesterTuitionFee = ParseMoney(moneyString);
                    else if (titleSpan.Contains("bảo hiểm y tế", StringComparison.OrdinalIgnoreCase))
                        healthInsuranceFee = ParseMoney(moneyString) ?? 0;
                    else if (titleSpan.Contains("Nợ học phí", StringComparison.OrdinalIgnoreCase))
                        previousSemesterDebt = ParseMoney(moneyString) ?? 0;
                    else if (titleSpan.Equals("tổng cộng các khoản phải đóng", StringComparison.OrdinalIgnoreCase))
                        totalPayableAmount = ParseMoney(moneyString);
                    else if (titleSpan.Equals("tổng cộng đã đóng", StringComparison.OrdinalIgnoreCase))
                        totalPaidAmount = ParseMoney(moneyString);
                }
            }
        }

        if (semesterTuitionFee is null || totalPayableAmount is null || totalPaidAmount is null)
        {
            logger?.LogWarning("Cấu trúc dữ liệu dữ liệu các field {0}, {1}, {2},", nameof(semesterTuitionFee), nameof(totalPayableAmount), nameof(totalPaidAmount));
        }

        DateOnly? paymentDeadline = null;
        if (payload.ThongTin3 is not null)
        {
            foreach (var item in payload.ThongTin3)
            {
                if (item.Value.Contains("Hạn chót", StringComparison.OrdinalIgnoreCase))
                {
                    var match = DateRegex().Match(item.Value);
                    if (match.Success && DateOnly.TryParseExact(match.Groups[1].ValueSpan, "dd/MM/yyyy", out var date))
                    {
                        paymentDeadline = date;
                        break;
                    }
                }
            }
        }

        CreditFeeRate? generalFee = null;
        CreditFeeRate? majorFee = null;

        if (payload.GhiChu?.CotPhai is not null)
        {
            foreach (var item in payload.GhiChu.CotPhai)
            {
                var textSpan = item.Value.AsSpan();

                if (textSpan.Contains("đại cương", StringComparison.OrdinalIgnoreCase))
                    generalFee = ExtractCreditFeeRate(item.Value);
                else if (textSpan.Contains("ngành", StringComparison.OrdinalIgnoreCase) ||
                         textSpan.Contains("còn lại", StringComparison.OrdinalIgnoreCase))
                    majorFee = ExtractCreditFeeRate(item.Value);
            }
        }

        return new TuitionFeeSummary(
            SemesterTuitionFee: semesterTuitionFee,
            HealthInsuranceFee: healthInsuranceFee,
            PreviousSemesterDebt: previousSemesterDebt,
            TotalPayableAmount: totalPayableAmount,
            TotalPaidAmount: totalPaidAmount,
            CalculationDate: calculationDate,
            PaymentDeadline: paymentDeadline,
            GeneralCreditFee: generalFee,
            MajorCreditFee: majorFee
        );
    }


    private static long? ParseMoney(ReadOnlySpan<char> rawMoney)
    {
        if (rawMoney.IsWhiteSpace()) return null;

        rawMoney = rawMoney.Trim();

        if (rawMoney.EndsWith("đ", StringComparison.OrdinalIgnoreCase))
        {
            rawMoney = rawMoney[..^1].TrimEnd();
        }

        if (long.TryParse(rawMoney, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out long result))
            return result;

        return null;
    }

    private static CreditFeeRate? ExtractCreditFeeRate(string rawText)
    {
        var matches = FeeRegex().Matches(rawText);

        if (matches.Count >= 2)
        {
            // group[1] là cặp ngoặc đầu tiên của regex
            var fee = ParseMoney(matches[0].Groups[1].ValueSpan);
            var discount = ParseMoney(matches[1].Groups[1].ValueSpan);

            if (fee.HasValue && discount.HasValue)
                return new CreditFeeRate(fee.Value, discount.Value);
        }
        else if (matches.Count == 1)
        {
            var fee = ParseMoney(matches[0].Groups[1].ValueSpan);
            if (fee.HasValue)
                return new CreditFeeRate(fee.Value, 0);
        }

        return null;
    }
}