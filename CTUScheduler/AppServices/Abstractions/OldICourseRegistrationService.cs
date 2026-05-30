using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.AppServices.Abstractions;

[Obsolete]
public interface OldICourseRegistrationService
{
    Task<OperationResult<IReadOnlyList<PlannedCourse>>> FetchPlannedCourseAsync(CancellationToken token = default);
}