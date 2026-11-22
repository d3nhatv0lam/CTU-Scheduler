using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public class TuitionFeeStore : ITuitionFeeStore, ICleanup, IDisposable
{
    private readonly BehaviorSubject<TuitionFeeSummary?> _subject = new(null);
    private readonly ILogger<TuitionFeeStore> _logger;
    private bool _isDisposed;

    public TuitionFeeStore(ILogger<TuitionFeeStore> logger)
    {
        _logger = logger;
        TuitionFeeSummaryChanged = _subject.AsObservable();
    }

    public IObservable<TuitionFeeSummary?> TuitionFeeSummaryChanged { get; }
    public TuitionFeeSummary? CurrentTuitionFeeSummary => _subject.Value;

    public void Update(TuitionFeeSummary tuitionFeeSummary)
    {
        ArgumentNullException.ThrowIfNull(tuitionFeeSummary);
        _subject.OnNext(tuitionFeeSummary);
    }

    public void Clear()
    {
        _subject.OnNext(null);
    }

    public void Cleanup() => Clear();

    public void Dispose()
    {
        if (_isDisposed) return;

        _subject.Dispose();
        _logger.LogDebug("{ClassName} disposed!", nameof(TuitionFeeStore));

        _isDisposed = true;
    }
}