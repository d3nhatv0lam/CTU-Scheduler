namespace CTUScheduler.Presentation.Services.Navigation.Models;

public readonly record struct RegionId(string Value)
{
    public string Value { get; init; } = Value ?? string.Empty;
    public bool IsEmpty => string.IsNullOrEmpty(Value);
}