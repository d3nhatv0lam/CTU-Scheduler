using System;
using CTUScheduler.Core.Models.TeachingPlan;

namespace CTUScheduler.AppServices.State;

public interface IAppState
{
    IObservable<TeachingPlanData?> TeachingPlanChanged { get; }
    TeachingPlanData? TeachingPlan { get; }
    void SetTeachingPlan(TeachingPlanData? data);
}