using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Components;

public partial class SchedulingCourseViewModel : ViewModelBase, INeedArgs<Course>, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ILogger<SchedulingCourseViewModel>? _logger;
    [Reactive] private bool _isMainCourse = true;
    [Reactive] private int _mainCourseLockCount = 0;
    [Reactive] private IEnumerable<SchedulingCourseViewModel> _replacementOptions = [];
    [Reactive] private SchedulingCourseViewModel? _selectedReplacement;

    [ObservableAsProperty] private bool _isLocked;
    public Course Item { get; }

    public SchedulingCourseViewModel(Course course, ILogger<SchedulingCourseViewModel>? logger = null)
    {
        Item = course;
        _logger = logger;

        this.WhenAnyValue(x => x.IsMainCourse)
            .Where(isMain => isMain)
            .Subscribe(_ => SelectedReplacement = null)
            .DisposeWith(_disposables);

        _isLockedHelper = this.WhenAnyValue(x => x.MainCourseLockCount)
            .Select(count => count > 0)
            .ToProperty(this, nameof(IsLocked))
            .DisposeWith(_disposables);

        SchedulingCourseViewModel? oldReplacement = SelectedReplacement;
        this.WhenAnyValue(x => x.SelectedReplacement)
            .Subscribe(newReplacement =>
            {
                if (ReferenceEquals(oldReplacement, newReplacement)) return;

                if (oldReplacement != null)
                    oldReplacement.MainCourseLockCount--;

                if (newReplacement != null)
                    newReplacement.MainCourseLockCount++;

                oldReplacement = newReplacement;
            })
            .DisposeWith(_disposables);
    }

    public void Dispose()
    {
        SelectedReplacement = null;
        _disposables.Dispose();
        _logger?.LogDebug("{this} - {code}: disposed", nameof(SchedulingCourseViewModel), Item.Code);
    }
}