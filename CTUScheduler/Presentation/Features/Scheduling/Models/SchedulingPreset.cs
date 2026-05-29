using CTUScheduler.Core.Models.Scoring;

namespace CTUScheduler.Presentation.Features.Scheduling.Models;

/// <summary>
/// Model chứa thông tin hiển thị của một Preset trên UI
/// </summary>
public class SchedulingPreset
{
    public string Name { get; init; } = string.Empty;
    public string Icon { get; init; } = "";
    public string Description { get; init; } = string.Empty;
    public ScoringProfile Profile { get; init; } = new();
}
