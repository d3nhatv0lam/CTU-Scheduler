using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

public interface ICourseValidationScope
{
    bool Validate(IEnumerable<Course> courses, IEnumerable<ScheduleTable> tables);
    void Commit();
}