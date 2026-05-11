using System;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public interface IPlannedCourseStore
{
    IObservable<IReadOnlyList<PlannedCourse>?> PlannedCoursesChanged { get; }
    IReadOnlyList<PlannedCourse>? CurrentPlannedCourses { get; }
    void Update(IReadOnlyList<PlannedCourse> courses);
    void Clear();
}