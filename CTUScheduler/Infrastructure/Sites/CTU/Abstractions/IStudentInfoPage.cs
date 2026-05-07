using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Infrastructure.Sites.Base;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface IStudentInfoPage : ISitePage
{
    Task<StudentProfile?> GetStudentProfileAsync(CancellationToken cancellationToken = default);
}