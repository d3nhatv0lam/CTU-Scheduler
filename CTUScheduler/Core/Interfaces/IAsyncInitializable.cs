using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Interfaces;

public interface IAsyncInitializable<in T>
{
    Task InitializeAsync(T args, CancellationToken cancellationToken = default);
}