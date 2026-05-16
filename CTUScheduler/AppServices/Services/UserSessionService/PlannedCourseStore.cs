using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public sealed class PlannedCourseStore : IPlannedCourseStore, ICleanup, IDisposable
{
    private readonly BehaviorSubject<IReadOnlyList<PlannedCourse>?> _subject = new(null);
    private readonly ILogger<PlannedCourseStore> _logger;
    private bool _isDisposed;

    public IObservable<IReadOnlyList<PlannedCourse>?> PlannedCoursesChanged { get; }
    public IReadOnlyList<PlannedCourse>? CurrentPlannedCourses => _subject.Value;

    public PlannedCourseStore(ILogger<PlannedCourseStore> logger)
    {
        _logger = logger;

        PlannedCoursesChanged = _subject.AsObservable();
    }

    ~PlannedCourseStore() => Dispose(false);

    public void Update(IReadOnlyList<PlannedCourse> courses)
    {
        ArgumentNullException.ThrowIfNull(courses);
        _subject.OnNext(courses);
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
        _logger.LogDebug(nameof(PlannedCourseStore) + " disposed!");
    }
}