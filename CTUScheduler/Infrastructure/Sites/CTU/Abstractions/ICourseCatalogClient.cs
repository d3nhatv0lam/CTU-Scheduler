using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface ICourseCatalogClient
{
    Task<IReadOnlyList<QuickSelectDmhpCourse>> GetAutoCompleteQueryAsync(string keywork, CancellationToken ct = default);
    Task<RawDmhpPayload> GetCourseRawAsync(string courseCode, CancellationToken ct = default);
}