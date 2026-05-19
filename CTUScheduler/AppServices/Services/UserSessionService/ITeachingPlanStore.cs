using System;
using CTUScheduler.Core.Models.TeachingPlan;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public interface ITeachingPlanStore
{
    IObservable<TeachingPlanData?> TeachingPlanChanged { get; }
    TeachingPlanData? CurrentTeachingPlan { get; }
    void Update(TeachingPlanData teachingPlan);
    void Clear();
}

