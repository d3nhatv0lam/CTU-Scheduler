using System;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleProfileService;

public class ScheduleProfileService: IScheduleProfileService
{
    private readonly SourceList<ScheduleProfile> _profiles;

    public ScheduleProfileService(AppState appState)
    {
        _profiles = appState.ScheduleProfilesSource;
    }

    public bool ValidateProfiles()
    {
        throw new NotImplementedException();
    }

    public void ClearAll() => _profiles.Clear();
}