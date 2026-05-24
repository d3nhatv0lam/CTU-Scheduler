using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Contexts;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface IAuthClient
{
    Task<LoginBootstrapContext> BootstrapAsync(
        CancellationToken ct = default);

    Task<SamlLoginContext> SubmitCredentialAsync(
        LoginBootstrapContext bootstrap,
        string username,
        string password,
        CancellationToken ct = default);

    Task<bool> CompleteAuthenticationAsync(
        SamlLoginContext challenge,
        CancellationToken ct = default);
}