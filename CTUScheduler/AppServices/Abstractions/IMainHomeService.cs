using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;
using System.Threading;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.AppServices.Abstractions;

public interface IMainHomeService
{
    Task<OperationResult> EnsureReadyAsync();
    Task<OperationResult<StudentProfile>> GetStudentProfileAsync(CancellationToken ct = default);
}