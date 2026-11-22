using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.Abstractions;

public class StateStore<T> : IStateStore<T>, ICleanup, IDisposable
{
    private readonly BehaviorSubject<T?> _subject = new(default);
    private readonly ILogger _logger;
    private bool _isDisposed;
    
    public IObservable<T?> Changed { get; }
    public T? Current => _subject.Value;
    
    public StateStore(ILogger<StateStore<T>> logger)
    {
        Changed = _subject.AsObservable();
        _logger = logger;
    }   
    
    public virtual void Update(T value)
    {
       ArgumentNullException.ThrowIfNull(value);
       _subject.OnNext(value);
    }

    public virtual void Clear()
    {
        _subject.OnNext(default);
    }

    public virtual void Cleanup() => Clear();

    public virtual void Dispose()
    {
        if (_isDisposed) return;

        _subject.Dispose();
        _logger.LogDebug("Disposed");

        _isDisposed = true;
    }
}