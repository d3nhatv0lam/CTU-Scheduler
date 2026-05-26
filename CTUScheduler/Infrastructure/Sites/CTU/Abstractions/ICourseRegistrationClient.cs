using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface ICourseRegistrationClient
{
    Task<RawDkhpPayload> GetPlannedCoursesRawAsync(CancellationToken ct = default);
    Task<RawThongTinHocPhiPayload> GetTuitionFeeRawAsync(CancellationToken ct = default);
}