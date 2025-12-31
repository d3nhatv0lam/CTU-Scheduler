using System;
using System.Collections.Generic;
using System.Linq;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Legacy.RuntimeCourseService;
using DynamicData;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Legacy.ScheduleProfileService;

public class ScheduleProfileService: IScheduleProfileService
{
    private readonly SourceCache<ScheduleProfile,Guid> _profileSource;
    private readonly IRuntimeCourseService _runtimeCourseService;
    private readonly ILogger<ScheduleProfileService> _logger;
    
    public ScheduleProfileService(AppState appState, 
        IRuntimeCourseService runtimeCourseService, 
        ILogger<ScheduleProfileService> logger)
    {
        _profileSource = appState.ScheduleProfilesSource;
        _runtimeCourseService = runtimeCourseService;
        _logger = logger;
    }

    public bool RegisterBlueprint(ScheduleBlueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        bool isRegistered = false;
        _profileSource.Edit(innerList =>
        {
            if (!blueprint.IsConsistent)
            {
                return;
            }
            _runtimeCourseService.RegisterTimetable(blueprint);
            innerList.AddOrUpdate(blueprint.Metadata);
            isRegistered = true;
        });
        if (!isRegistered)
        {
            _logger.LogInformation("Blueprint is inconsistent");
        }

        return isRegistered;
    }

    public bool RegisterBlueprint(IEnumerable<ScheduleBlueprint> blueprints)
    {
        ArgumentNullException.ThrowIfNull(blueprints);
        
        var registerableList = blueprints
            .Where(x => x is not null && x.IsConsistent)
            .ToList();
        
        if (registerableList.Count == 0) return false;
        
        _profileSource.Edit(innerList =>
        {
           _runtimeCourseService.RegisterTimetable(registerableList);
           innerList.AddOrUpdate(registerableList.Select(x => x.Metadata));
        });
        return true;
    }

    public void UnRegisterProfile(ScheduleProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _runtimeCourseService.UnregisterTimetable(profile);
        _profileSource.Remove(profile.Id);
    }

    public void ClearAll()
    {
        _runtimeCourseService.ClearAll();
        _profileSource.Clear();
    }
}