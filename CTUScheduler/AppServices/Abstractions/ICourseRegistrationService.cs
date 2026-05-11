using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.AppServices.Abstractions;

public interface ICourseRegistrationService
{
    Task<OperationResult> EnsureReadyAsync();

    Task<OperationResult<IReadOnlyList<PlannedCourse>>> FetchPlannedCourseAsync(TimeSpan? timeout = null,
        CancellationToken token = default);
}