using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface IRegistrationRulesClient
{
    Task<RawQddkPayload> GetRegistrationInformationRawAsync(CancellationToken ct = default);
}