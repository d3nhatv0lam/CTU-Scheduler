using System;
using System.Collections;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Settings;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleService.Interfaces;

public interface IProfileQueryService
{
    IObservable<IChangeSet<ScheduleProfile, Guid>> ConnectProfiles();
    IEnumerable<ScheduleProfile> GetProfileSnapshot();
    IObservable<ProfileUsageState> ProfileUsageState { get; }
}