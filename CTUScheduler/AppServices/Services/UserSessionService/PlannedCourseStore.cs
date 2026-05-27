using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.AppServices.Services.Abstractions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public sealed class PlannedCourseStore : StateStore<IReadOnlyList<PlannedCourse>>, IPlannedCourseStore
{
    public PlannedCourseStore(ILogger<StateStore<IReadOnlyList<PlannedCourse>>> logger) : base(logger)
    {
    }
}