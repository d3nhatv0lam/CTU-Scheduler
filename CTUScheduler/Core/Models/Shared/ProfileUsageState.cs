namespace CTUScheduler.Core.Models.Shared;

public readonly record struct ProfileUsageState(int Current, int Limit)
{
    public bool CanAdd => Current < Limit;
    public string DisplayText => $"{Current}/{Limit}";
}