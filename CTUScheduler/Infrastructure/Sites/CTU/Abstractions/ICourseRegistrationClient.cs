using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface ICourseRegistrationClient
{
    Task<List<RawDkhpPayload>> GetPlannedCoursesRawAsync(CancellationToken ct = default);

    Task<RawThongTinHocPhiPayload> GetTuitionFeeRawAsync(
        int academicYear,
        int semester,
        CancellationToken ct = default);
}