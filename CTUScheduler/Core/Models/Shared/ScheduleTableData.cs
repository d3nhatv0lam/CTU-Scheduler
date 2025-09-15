using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.Core.Models.Shared;

public record ScheduleTableData(IEnumerable<Course> courses, ScheduleTable scheduleTable);