using System;

namespace CTUScheduler.Core.Models.Academic.Curriculum.Registration;

public record TuitionFeeSummary(
    long SemesterTuitionFee,
    long HealthInsuranceFee,
    long PreviousSemesterDebt,
    long TotalPayableAmount,
    long TotalPaidAmount,
    DateOnly? PaymentDeadline,
    CreditFeeRate? GeneralCreditFee,
    CreditFeeRate? MajorCreditFee
)
{
    /// <summary>
    /// Số tiền cần để trả hết học phí
    /// Lấy Phải đóng - Đã đóng. Nếu số âm (đóng dư) thì trả về 0.
    /// </summary>
    public long DebtAmount => Math.Max(0, TotalPayableAmount - TotalPaidAmount);

    /// <summary>
    /// Có còn nợ tiền nhà trường tại thời điểm hiện tại không?
    /// </summary>
    public bool IsInDebt => DebtAmount > 0;

    /// <summary>
    /// Kiểm tra xem sinh viên này có từng bị mang nợ từ học kỳ trước sang không
    /// </summary>
    public bool HadOldDebt => PreviousSemesterDebt > 0;

    public bool HasInsuranceFee => HealthInsuranceFee > 0;
}

public record CreditFeeRate(
    long FeePerCredit, // Mức học phí/1TC (VD: 630000 hoặc 755000)
    long DiscountPerCredit // Mức miễn giảm/1TC (VD: 451000 hoặc 538000)
);