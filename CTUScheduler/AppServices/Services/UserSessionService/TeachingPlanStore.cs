using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.TeachingPlan;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public sealed class TeachingPlanStore : ITeachingPlanStore, ICleanup, IDisposable
{
    private readonly BehaviorSubject<TeachingPlanData?> _subject = new(null);
    private readonly ILogger<TeachingPlanStore> _logger;
    private bool _isDisposed;

    public TeachingPlanStore(ILogger<TeachingPlanStore> logger)
    {
        _logger = logger;
        TeachingPlanChanged = _subject.AsObservable();
    }

    ~TeachingPlanStore() => Dispose(false);

    public IObservable<TeachingPlanData?> TeachingPlanChanged { get; }
    public TeachingPlanData? CurrentTeachingPlan => _subject.Value;

    public void Update(TeachingPlanData teachingPlan)
    {
        ArgumentNullException.ThrowIfNull(teachingPlan);
        _subject.OnNext(teachingPlan);
    }

    public void Clear()
    {
        _subject.OnNext(null);
    }

    public void Cleanup() => Clear();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            _subject.Dispose();
        }

        _isDisposed = true;
        _logger.LogDebug(nameof(TeachingPlanStore) + " disposed!");
    }
}

