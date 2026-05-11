using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;


namespace CTUScheduler.AppServices.Abstractions;

public interface IStudentIdentityService
{
    Task<OperationResult<StudentProfile>> GetIdentityAsync(CancellationToken ct = default);
}