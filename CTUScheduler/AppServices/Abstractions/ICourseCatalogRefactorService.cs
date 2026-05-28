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
    /// Lấy danh sách gợi ý Autocomplete khi gõ tìm kiếm môn học (Một chạm - Siêu tốc)
    /// </summary>
    Task<OperationResult<IReadOnlyList<QuickSelectDmhpCourse>>> FetchSuggestionsAsync(
        string query,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Lấy chi tiết thông tin của 1 môn học dựa vào mã môn (Một chạm)
    /// </summary>
    Task<OperationResult<Course>> FetchCourseAsync(
        string courseCode,
        int? academicYear = null,
        int? semester = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Cào hàng loạt nhiều môn học cùng lúc
    /// </summary>
    IAsyncEnumerable<Course> FetchCoursesBatchAsync(
        IEnumerable<string> courseCodes,
        int? academicYear = null,
        int? semester = null,
        int maxWorkers = 2,
        CancellationToken cancellationToken = default
    );
}