using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.Infrastructure.Services.Registration;

public interface ICourseCatalogService
{
    Task<OperationResult> EnsureReadyAsync();

    /// <summary>
    /// Retrieves a stream of quick-select course suggestions based on the provided query parameter.
    /// </summary>
    /// <param name="query">The search query used to filter and fetch course suggestions.</param>
    /// <param name="timeout">default is 5sec</param>
    /// <returns>An observable sequence that provides a list of quick-select courses matching the query.</returns>
    IObservable<List<QuickSelectCourse>> RequestSuggestionsStream(string query, TimeSpan? timeout = null);

    /// <summary>
    /// Retrieves a stream of course details based on the provided query parameter.
    /// </summary>
    /// <param name="query">The search query used to filter and fetch course details.</param>
    /// <param name="timeout">default is 10sec</param>
    /// <returns>An observable sequence that provides detailed information about the courses matching the query.</returns>
    IObservable<Course> RequestCourseStream(string query, TimeSpan? timeout = null);

    /// <summary>
    /// Fetches detailed course information based on the specified course code.
    /// </summary>
    /// <param name="courseCode">The unique identifier for the course to retrieve.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete, allowing the operation to be canceled.</param>
    /// <param name="timeout"> default is 3sec</param>
    /// <returns>A task that represents the asynchronous operation, returning the course details for the provided course code.</returns>
    Task<Course> FetchCourseAsync(string courseCode, CancellationToken cancellationToken = default,TimeSpan? timeout = null);
}