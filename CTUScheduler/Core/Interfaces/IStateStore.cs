using System;

namespace CTUScheduler.Core.Interfaces;

public interface IStateAccessor<out T>
{
    IObservable<T?> Changed { get; }
    T? Current { get; }
}

public interface IStateStore<T>: IStateAccessor<T>
{
    void Update(T value);
    void Clear();
}