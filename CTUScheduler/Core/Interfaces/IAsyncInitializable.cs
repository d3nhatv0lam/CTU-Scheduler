using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Interfaces;

public interface IAsyncInitializable<in T>
{
    Task InitAsync(T args, CancellationToken cancellationToken = default);
}