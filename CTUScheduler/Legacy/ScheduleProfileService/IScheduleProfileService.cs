using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Legacy.ScheduleProfileService;

public interface IScheduleProfileService
{
    bool RegisterBlueprint(ScheduleBlueprint blueprint);
    bool RegisterBlueprint(IEnumerable<ScheduleBlueprint> blueprints);
    void UnRegisterProfile(ScheduleProfile profile);
    void ClearAll();
}