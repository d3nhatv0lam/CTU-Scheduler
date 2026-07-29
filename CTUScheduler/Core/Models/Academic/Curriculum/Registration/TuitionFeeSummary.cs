using System;

namespace CTUScheduler.Core.Models.Academic.Curriculum.Registration;

public record TuitionFeeSummary(
    decimal? SemesterTuitionFee,
    decimal? HealthInsuranceFee,
    decimal? PreviousSemesterDebt,
    decimal? TotalPayableAmount,
    decimal? TotalPaidAmount,
    // ngày bắt đầu tính học phí
    DateOnly? CalculationDate,
    DateOnly? PaymentDeadline,
    CreditFeeRate? GeneralCreditFee,
    CreditFeeRate? MajorCreditFee
)
{
    /// <summary>
    /// Đã có thông tin tính toán học phí chính thức từ trường chưa?
    /// </summary>
    public bool IsCalculated => TotalPayableAmount.HasValue || SemesterTuitionFee.HasValue;
    
    public bool HasInsuranceFee => HealthInsuranceFee > 0m;
    public bool HasDeadline => PaymentDeadline is not null;

    /// <summary>
    /// Số tiền cần để trả hết học phí
    /// Lấy Phải đóng - Đã đóng. Nếu số âm (đóng dư) thì trả về 0.
    /// </summary>
    public decimal DebtAmount => IsCalculated
        ? Math.Max(0m, (TotalPayableAmount ?? 0m) - (TotalPaidAmount ?? 0m))
        : 0m;

    /// <summary>
    /// Có còn nợ tiền nhà trường tại thời điểm hiện tại không?
    /// </summary>
    public bool IsInDebt => DebtAmount > 0m;

    /// <summary>
    /// Kiểm tra xem sinh viên này có từng bị mang nợ từ học kỳ trước sang không
    /// </summary>
    public bool HadOldDebt => PreviousSemesterDebt > 0m;

    /// <summary>
    /// Kiểm tra xem ngày hiện tại đã vượt qua hạn đóng tiền của trường chưa
    /// </summary>
    public bool IsPastDeadline =>
        PaymentDeadline.HasValue && DateOnly.FromDateTime(DateTime.Today) > PaymentDeadline.Value;

    /// <summary>
    /// TRẠNG THÁI ĐỎ - NỢ NGUY HIỂM: Đã qua deadline kỳ này, vẫn còn nợ tiền,
    /// VÀ số tiền đã đóng chưa đủ để trả hết cục nợ cũ của kỳ trước (TotalPaidAmount ;lt PreviousSemesterDebt).
    /// </summary>
    public bool IsCriticalDebt => IsCalculated && HadOldDebt && IsInDebt && ((TotalPaidAmount ?? 0m) < PreviousSemesterDebt);

    /// <summary>
    /// NỢ QUÁ HẠN THÔNG THƯỜNG: Đã qua deadline kỳ này, vẫn còn nợ tiền,
    /// nhưng KHÔNG rơi vào diện nợ nguy hiểm (hoặc là trễ kỳ này lần đầu, hoặc có nợ cũ nhưng đã đóng đủ để xóa nợ cũ).
    /// </summary>
    public bool IsNormalDebt => IsCalculated && IsInDebt && IsPastDeadline && !IsCriticalDebt;
}

public record CreditFeeRate(
    decimal FeePerCredit, // Mức học phí/1TC (VD: 630000 hoặc 755000)
    decimal DiscountPerCredit // Mức miễn giảm/1TC (VD: 451000 hoặc 538000)
);