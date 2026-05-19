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

    ~TuitionFeeStore() => Dispose(false);

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
        _logger.LogDebug(nameof(TuitionFeeStore) + " disposed!");
    }
}