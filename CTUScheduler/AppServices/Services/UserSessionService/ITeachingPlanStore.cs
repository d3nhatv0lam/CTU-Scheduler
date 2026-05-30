using System;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.TeachingPlan;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public interface ITeachingPlanStore: IStateStore<TeachingPlanData>
{
}

