using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Models.Context;

public class CourseBlueprint : ReactiveObject, IDisposable
{
    private readonly IDisposable _disposable;
    private readonly SourceList<CourseSection> _sectionsSource = new();
    private readonly ReadOnlyObservableCollection<CourseSection> _sections;

    public Course CoreCourse { get; }
    public ReadOnlyObservableCollection<CourseSection> Sections => _sections;
    public IObservable<IChangeSet<CourseSection>> SectionsSourceChanges => _sectionsSource.Connect();

    public CourseBlueprint(Course course, IEnumerable<CourseSection> selectedSections)
    {
        CoreCourse = course;
        _sectionsSource.AddRange(selectedSections);

        _disposable = _sectionsSource.Connect()
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Sort(SortExpressionComparer<CourseSection>.Ascending(s => s.Key))
            .Bind(out _sections)
            .Subscribe();
    }
    
    public void UpdateSections(Action<IExtendedList<CourseSection>> updateAction)
    {
        _sectionsSource.Edit(updateAction);
    }

    public void Dispose()
    {
        _disposable.Dispose();
        _sectionsSource.Dispose();
    }
}