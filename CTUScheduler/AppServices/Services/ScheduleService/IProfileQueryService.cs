using System;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleService;

public interface IProfileQueryService
{
    IObservable<IChangeSet<ScheduleProfile, Guid>> ConnectProfiles();
    IEnumerable<ScheduleProfile> GetProfileSnapshot();
    IObservable<ProfileUsageState> ProfileUsageState { get; }
}