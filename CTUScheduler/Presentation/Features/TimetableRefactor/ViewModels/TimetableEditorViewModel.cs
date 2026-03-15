using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Services.ScheduleService;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.TimetableRefactor.Adapters;
using DynamicData;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

public class TimetableEditorViewModel : TimetableLayoutBaseViewModel, INeedArgs<ScheduleProfile>
{
    public readonly ScheduleProfile _scheduleProfile;

    public ScheduleProfile ScheduleProfile => _scheduleProfile;
    private readonly ObservableAsPropertyHelper<bool> _isEditing;
    private readonly ICourseQueryService _courseQueryService;
    public bool IsEditing => _isEditing.Value;

    public ReactiveCommand<Unit, Unit> StartEditCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public TimetableEditorViewModel(ScheduleProfile scheduleProfile, ICourseQueryService courseQueryService)
    {
        ArgumentNullException.ThrowIfNull(scheduleProfile);
        _scheduleProfile = scheduleProfile;
        _courseQueryService = courseQueryService;

        Name = _scheduleProfile.Name;
        LastUpdated = _scheduleProfile.LastUpdated;

        var savedRef = scheduleProfile.SavedCourseGroupKeys;
        var sharedCourse = courseQueryService.ConnectCourses()
            .SubscribeOn(RxApp.TaskpoolScheduler)
            .Filter(x => savedRef.ContainsKey(x.Code))
            .MergeMany(runtimeCourse =>
            {
                string targetGroup = savedRef[runtimeCourse.Code];
                return runtimeCourse.Sections.Connect()
                    .Filter(s => string.Equals(s.Group, targetGroup, StringComparison.OrdinalIgnoreCase))
                    .Transform(section =>
                    {
                        var adapter = new LiveCourseAdapter(runtimeCourse, section);
                        return CreateRenderItem(adapter);
                    })
                    .ChangeKey(_ => Guid.NewGuid());
            })
            .DisposeMany()
            .RemoveKey()
            .AsObservableList()
            .DisposeWith(Disposables);

        VisualizerVM = new TimetableViewModel(sharedCourse.Connect())
            .DisposeWith(Disposables);

        sharedCourse.Connect()
            .Transform(item => item.SharedData.Credits)
            .QueryWhenChanged(items => new
            {
                items.Count,
                Sum = items.Sum()
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x =>
            {
                SubjectsCount = x.Count;
                TotalCredits = x.Sum;
            })
            .DisposeWith(Disposables);

        sharedCourse.Connect()
            .Throttle(TimeSpan.FromSeconds(1.2))
            .Skip(1)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                LastUpdated = DateTimeOffset.Now;
                _scheduleProfile.LastUpdated = LastUpdated;
            })
            .DisposeWith(Disposables);

        StartEditCommand = ReactiveCommand.Create(() => { }).DisposeWith(Disposables);

        SaveCommand = ReactiveCommand.Create(() =>
        {
            _scheduleProfile.Name = this.Name;
            _scheduleProfile.LastUpdated = DateTimeOffset.Now;
            LastUpdated = _scheduleProfile.LastUpdated;
        }).DisposeWith(Disposables);
        CancelCommand = ReactiveCommand.Create(() => { Name = _scheduleProfile.Name; }).DisposeWith(Disposables);

        _isEditing = Observable.Merge(
                StartEditCommand.Select(_ => true),
                SaveCommand.Select(_ => false),
                CancelCommand.Select(_ => false)
            )
            .ToProperty(this, nameof(IsEditing), initialValue: false, scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(Disposables);
    }

    public override ScheduleBlueprint ToScheduleBlueprint()
    {
        var savedRef = _scheduleProfile.SavedCourseGroupKeys;
        // LINQ
        var courses = _courseQueryService.GetCoursesSnapshot()
            .Where(course => savedRef.TryGetValue(course.Code, out _))
            .Select(course =>
            {
                var targetGroup = savedRef[course.Code];
                var filteredSections = course.Sections
                    .Where(section => string.Equals(section.Group, targetGroup, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                return course.WithSections(filteredSections);
            })
            .ToList();

        return new ScheduleBlueprint(courses, _scheduleProfile);

        // Performance
        // var coursesSnapshot = _courseQueryService.GetCoursesSnapshot();
        // var courses = new List<Course>();
        // foreach (var course in coursesSnapshot)
        // {
        //     if (savedRef.TryGetValue(course.Code, out var targetGroup))
        //     {
        //         var filteredSections = course.Sections
        //             .Where(section => string.Equals(section.Group, targetGroup, StringComparison.OrdinalIgnoreCase))
        //             .ToList();
        //         courses.Add(course.WithSections(filteredSections));
        //     }
        // }
        // return new ScheduleBlueprint(courses,_scheduleProfile);
    }

    public override void Dispose()
    {
        base.Dispose();
        Debug.WriteLine($"TimetableEditorViewModel ID: {_scheduleProfile.Id}, Name: {Name} disposed");
    }
}