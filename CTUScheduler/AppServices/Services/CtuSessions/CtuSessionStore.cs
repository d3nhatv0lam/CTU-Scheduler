using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Networking;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.CtuSessions;

public class CtuSessionStore: ICtuSessionStore, ICleanup, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly BehaviorSubject<CtuSession?> _subject = new(null);
    private readonly ILogger<CtuSessionStore> _logger;
    
    public IObservable<CtuSession?> CtuSessionChanged { get; }
    public CtuSession? CurrentSession => _subject.Value;
    
    public CtuSessionStore(ILogger<CtuSessionStore> logger)
    {
        _logger = logger;
        _subject.DisposeWith(_disposables);

        CtuSessionChanged = _subject.AsObservable();
    }

    public void Update(CtuSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _subject.OnNext(session);
    }
    
    public void Clear()
    {
        _subject.OnNext(null);
        _logger.LogDebug("Cleared!");
    }

    public void Cleanup() => Clear();

    public void Dispose()
    {
        _disposables.Dispose();
        _logger.LogDebug("Disposed!");
    }
}