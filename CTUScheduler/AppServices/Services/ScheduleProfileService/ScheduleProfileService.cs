using System;
using CTUScheduler.AppServices.Services.RuntimeCourseService;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleProfileService;

internal class ScheduleProfileService: IScheduleProfileService
{
    private readonly SourceList<ScheduleProfile> _profiles;
    private readonly IRuntimeCourseService _runtimeCourseService;
    
    internal ScheduleProfileService(AppState appState, IRuntimeCourseService runtimeCourseService)
    {
        _profiles = appState.ScheduleProfilesSource;
        _runtimeCourseService = runtimeCourseService;
    }

    public bool ValidateBlueprint(ScheduleBlueprint blueprint)
    {
        throw new NotImplementedException();
    }

    public void ClearAll()
    {
        _profiles.Clear();
        _runtimeCourseService.ClearAll();
    }
}