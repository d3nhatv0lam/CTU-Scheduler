using System;
using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Interfaces;


public interface IAsyncInitializable
{
    Task InitializeAsync(object? args = null,CancellationToken cancellationToken = default);
}

public interface IAsyncInitializable<in TContext>: IAsyncInitializable
{
    async Task IAsyncInitializable.InitializeAsync(object? args, CancellationToken cancellationToken)
    {
        if (args is TContext tArgs)
        {
            await InitializeAsync(tArgs, cancellationToken);
            return;
        }
        
        if (args is null && default(TContext) is null)
        {
            await InitializeAsync(default!, cancellationToken);
            return;
        }

        // Trường hợp 3: Sai kiểu -> Log warning
        throw new InvalidOperationException($"[Warning] Expected {typeof(TContext).Name}, got {args?.GetType().Name ?? "null"}");
    }
    Task InitializeAsync(TContext args, CancellationToken cancellationToken = default);
}