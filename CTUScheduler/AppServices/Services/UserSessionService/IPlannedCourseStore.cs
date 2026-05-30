using System.Collections.Generic;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public interface IPlannedCourseStore: IStateStore<IReadOnlyList<PlannedCourse>>
{

}