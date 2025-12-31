using System;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleService.Interfaces;

public interface IProfileQueryService
{
    IObservable<IChangeSet<ScheduleProfile, Guid>> ConnectProfiles();
}