using System;
using CTUScheduler.AppServices.Models;
using CTUScheduler.AppServices.Services.ScheduleService.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleService;

public interface IScheduleManager :
    IScheduleRegistrationService,
    ICourseQueryService,
    IProfileQueryService
{
    
}