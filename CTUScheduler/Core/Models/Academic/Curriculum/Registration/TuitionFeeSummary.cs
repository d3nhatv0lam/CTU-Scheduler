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
    public bool HasInsuranceFee => HealthInsuranceFee > 0;
    public bool HasDeadline => PaymentDeadline is not null;

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

    /// <summary>
    /// Kiểm tra xem ngày hiện tại đã vượt qua hạn đóng tiền của trường chưa
    /// </summary>
    public bool IsPastDeadline =>
        PaymentDeadline.HasValue && DateOnly.FromDateTime(DateTime.Today) > PaymentDeadline.Value;

    /// <summary>
    /// TRẠNG THÁI ĐỎ - NỢ NGUY HIỂM: Đã qua deadline kỳ này, vẫn còn nợ tiền,
    /// VÀ số tiền đã đóng chưa đủ để trả hết cục nợ cũ của kỳ trước (TotalPaidAmount ;lt PreviousSemesterDebt).
    /// </summary>
    public bool IsCriticalDebt => HadOldDebt && IsInDebt && (TotalPaidAmount < PreviousSemesterDebt);

    /// <summary>
    /// NỢ QUÁ HẠN THÔNG THƯỜNG: Đã qua deadline kỳ này, vẫn còn nợ tiền,
    /// nhưng KHÔNG rơi vào diện nợ nguy hiểm (hoặc là trễ kỳ này lần đầu, hoặc có nợ cũ nhưng đã đóng đủ để xóa nợ cũ).
    /// </summary>
    public bool IsNormalDebt => IsInDebt && IsPastDeadline && !IsCriticalDebt;
}

public record CreditFeeRate(
    long FeePerCredit, // Mức học phí/1TC (VD: 630000 hoặc 755000)
    long DiscountPerCredit // Mức miễn giảm/1TC (VD: 451000 hoặc 538000)
);