using System;

namespace CTUScheduler.Core.Interfaces;


public interface IInitializable
{
    void Initialize(object? args = null);
}

public interface IInitializable<in TContext> : IInitializable
{
    void IInitializable.Initialize(object? args)
    {
        if (args is TContext tArgs)
        {
            Initialize(tArgs);
            return;
        }
        
        if (args is null && default(TContext) is null)
        {
            Initialize(default!);
            return;
        }

        // Trường hợp 3: Sai kiểu -> Log warning
        throw new InvalidOperationException($"[Warning] Expected {typeof(TContext).Name}, got {args?.GetType().Name ?? "null"}");
    }
    
    void Initialize(TContext context);
}