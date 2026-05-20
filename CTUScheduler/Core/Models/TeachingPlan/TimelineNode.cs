using System;

namespace CTUScheduler.Core.Models.TeachingPlan;

public record TimelineNode(
    string Title,
    DateTime StartDate,
    DateTime EndDate,
    TimelineNodeType Type = TimelineNodeType.Range,
    string? Subtitle = null)
{
    public bool IsRange => Type == TimelineNodeType.Range;
    public bool IsSinglePoint => Type == TimelineNodeType.SinglePoint;
    public bool IsDeadline => Type == TimelineNodeType.DeadlineOrEnd;
    public bool IsStartFrom => Type == TimelineNodeType.StartFrom;

    private static string FormatDateTimeFull(DateTime dt)
    {
        return dt.TimeOfDay == TimeSpan.Zero
            ? dt.ToString("dd/MM/yyyy")
            : dt.ToString("HH:mm dd/MM/yyyy");
    }

    public string DisplayDate => Type switch
    {
        TimelineNodeType.SinglePoint => FormatDateTimeFull(StartDate),
        TimelineNodeType.Range => $"{FormatDateTimeFull(StartDate)} - {FormatDateTimeFull(EndDate)}",
        TimelineNodeType.DeadlineOrEnd => $"Hạn cuối: {FormatDateTimeFull(EndDate)}",
        TimelineNodeType.StartFrom => $"Bắt đầu từ: {FormatDateTimeFull(StartDate)}",
        _ => string.Empty
    };
}

public enum TimelineNodeType
{
    Range,
    SinglePoint,
    DeadlineOrEnd,
    StartFrom
}