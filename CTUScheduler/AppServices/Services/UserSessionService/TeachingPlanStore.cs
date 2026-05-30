using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.AppServices.Services.Abstractions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.TeachingPlan;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public sealed class TeachingPlanStore : StateStore<TeachingPlanData>, ITeachingPlanStore
{
    public TeachingPlanStore(ILogger<StateStore<TeachingPlanData>> logger) : base(logger)
    {
    }
}

