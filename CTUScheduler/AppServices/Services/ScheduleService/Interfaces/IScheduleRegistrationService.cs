using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.AppServices.Services.ScheduleService.Interfaces;

public interface IScheduleRegistrationService
{
    bool RegisterBlueprint(ScheduleBlueprint blueprint);
    bool RegisterBlueprint(IEnumerable<ScheduleBlueprint> blueprints);
    void UnregisterProfile(ScheduleProfile profile);
    void ClearAll();
}