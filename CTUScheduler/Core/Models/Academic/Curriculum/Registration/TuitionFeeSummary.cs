using System;

namespace CTUScheduler.Core.Models.Academic.Curriculum.Registration;

public record TuitionFeeSummary(
    long SemesterTuitionFee,
    bool HasHealthInsuranceFee,
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
    /// Có nợ học phí không?
    /// </summary>
    public bool IsInDebt => DebtAmount > 0 && !HasHealthInsuranceFee;
}

public record CreditFeeRate(
    long FeePerCredit, // Mức học phí/1TC (VD: 630000 hoặc 755000)
    long DiscountPerCredit // Mức miễn giảm/1TC (VD: 451000 hoặc 538000)
);