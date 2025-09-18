using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.Core.Models.Shared;

public record ScheduleTableBuildData(IEnumerable<Course> Courses, ScheduleTable ScheduleTable);