using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

namespace CTUScheduler.AppServices.Abstractions;

public interface ICourseCatalogRefactorService
{
    /// <summary>
    /// Fetches a list of course suggestions based on a search query.
    /// </summary>
    /// <param name="query">The search query used to filter and fetch course suggestions.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>An operation result containing a read-only list of quick-select courses that match the query.</returns>
    Task<OperationResult<IReadOnlyList<QuickSelectDmhpCourse>>> FetchSuggestionsAsync(
        string query,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Fetches detailed information about a specific course based on the provided course code.
    /// </summary>
    /// <param name="courseCode">The unique code of the course to be fetched.</param>
    /// <param name="academicYear">The optional academic year for which the course details are being requested.</param>
    /// <param name="semester">The optional semester for which the course details are being requested.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation if needed.</param>
    /// <returns>An operation result containing the details of the requested course.</returns>
    Task<OperationResult<Course>> FetchCourseAsync(
        string courseCode,
        int? academicYear = null,
        int? semester = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves a batch of course details asynchronously based on the provided course codes.
    /// </summary>
    /// <param name="courseCodes">A collection of course codes for which details are to be fetched.</param>
    /// <param name="academicYear">The optional academic year to filter the courses.</param>
    /// <param name="semester">The optional semester to filter the courses.</param>
    /// <param name="maxWorkers">The maximum number of parallel workers to process the requests.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous stream of course details matching the specified criteria.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="maxWorkers"/> is less than 1.</exception>
    /// <exception cref="ArgumentNullException">If <paramref name="courseCodes"/> is null or empty.</exception>
    /// <exception cref="OperationCanceledException">If the operation is canceled.</exception>
    IAsyncEnumerable<Course> FetchCoursesBatchAsync(
        IEnumerable<string> courseCodes,
        int? academicYear = null,
        int? semester = null,
        int maxWorkers = 2,
        CancellationToken cancellationToken = default
    );
}