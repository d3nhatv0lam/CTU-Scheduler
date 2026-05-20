using System;
using System.Collections.Generic;
using CTUScheduler.Presentation.Shared.Controls.Timeline;

namespace CTUScheduler.Core.Models.TeachingPlan;

/// <summary>
/// Dữ liệu kế hoạch giảng dạy trích xuất và đồng bộ từ PDF của CTU.
/// </summary>
public record TeachingPlanData(
    IReadOnlyList<TimelineNode> RegistrationTimeline,
    DateTime? SemesterStartDate,
    DateTime? SemesterEndDate,
    IReadOnlyList<TeachingPlanAdjustmentDetail> AdjustmentDetails,
    string? PdfUrl = null
)
{
    /// <summary>
    /// Khởi tạo mặc định khi chưa có dữ liệu.
    /// </summary>
    public TeachingPlanData() : this(new List<TimelineNode>(), null, null, new List<TeachingPlanAdjustmentDetail>(), null)
    {
    }

    protected virtual bool PrintMembers(System.Text.StringBuilder builder)
    {
        builder.Append(
            $"{nameof(RegistrationTimeline)} = [{(RegistrationTimeline != null ? string.Join(", ", RegistrationTimeline) : "")}], ");
        builder.Append($"{nameof(SemesterStartDate)} = {SemesterStartDate}, ");
        builder.Append($"{nameof(SemesterEndDate)} = {SemesterEndDate}, ");
        builder.Append(
            $"{nameof(AdjustmentDetails)} = [{(AdjustmentDetails != null ? string.Join(", ", AdjustmentDetails) : "")}], ");
        builder.Append($"{nameof(PdfUrl)} = {PdfUrl}");
        return true;
    }
}

/// <summary>
/// Chi tiết thời gian cụ thể đợt điều chỉnh kế hoạch học tập của từng khóa.
/// </summary>
public record TeachingPlanAdjustmentDetail(
    string Cohort,
    DateTime StartDateTime,
    DateTime EndDateTime,
    IReadOnlyList<int> AllowedGroups
)
{
    protected virtual bool PrintMembers(System.Text.StringBuilder builder)
    {
        builder.Append($"{nameof(Cohort)} = {Cohort}, ");
        builder.Append($"{nameof(StartDateTime)} = {StartDateTime}, ");
        builder.Append($"{nameof(EndDateTime)} = {EndDateTime}, ");
        builder.Append(
            $"{nameof(AllowedGroups)} = [{(AllowedGroups != null ? string.Join(", ", AllowedGroups) : "")}]");
        return true;
    }
}