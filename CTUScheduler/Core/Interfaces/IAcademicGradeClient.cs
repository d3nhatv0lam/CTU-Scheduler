using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Grades;

namespace CTUScheduler.Core.Interfaces;

public interface IAcademicGradeClient
{
    Task<GradeTranscript> FetchTranscriptAsync(CancellationToken cancellationToken = default);
}
