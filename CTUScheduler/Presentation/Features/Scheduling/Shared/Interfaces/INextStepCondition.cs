namespace CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;

public interface INextStepCondition
{
    public bool IsNextStepEnabled { get; }
}