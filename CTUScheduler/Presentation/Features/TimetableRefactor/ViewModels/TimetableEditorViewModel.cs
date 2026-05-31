using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia.Threading;
using CTUScheduler.AppServices.Services.ScheduleService;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Infrastructure.Excel;
using CTUScheduler.Presentation.Features.TimetableRefactor.Adapters;
using CTUScheduler.Presentation.Features.TimetableRefactor.Models;
using CTUScheduler.Presentation.Services.ControlRenderer;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using DynamicData;
using ReactiveUI;
using DynamicData.Aggregation;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

public class TimetableEditorViewModel : TimetableLayoutBaseViewModel, INeedArgs<ScheduleProfile>
{
    private readonly ScheduleProfile _scheduleProfile;
    private readonly ObservableAsPropertyHelper<bool> _isEditing;
    private readonly ICourseQueryService _courseQueryService;
    private readonly IObservableList<TimetableRenderItem> _sharedCourse;

    public ScheduleProfile ScheduleProfile => _scheduleProfile;
    public bool IsEditing => _isEditing.Value;
    public ReactiveCommand<Unit, Unit> StartEditCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }


    public TimetableEditorViewModel(
        ScheduleProfile scheduleProfile,
        ICourseQueryService courseQueryService,
        IExcelExporterService excelExporter,
        IControlRendererService controlRendererService,
        IUserInteractionService userInteractionService) : base(excelExporter, controlRendererService, userInteractionService)
    {
        ArgumentNullException.ThrowIfNull(scheduleProfile);

        _scheduleProfile = scheduleProfile;
        _courseQueryService = courseQueryService;

        Name = _scheduleProfile.Name;
        LastUpdated = _scheduleProfile.LastUpdated;
        
        var savedRef = scheduleProfile.SavedCourseGroupKeys;
        _sharedCourse = courseQueryService.ConnectCourses()
            .ObserveOn(RxSchedulers.TaskpoolScheduler)
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
                    });
            })
            .DisposeMany()
            .RemoveKey()
            .AsObservableList()
            .DisposeWith(Disposables);

        VisualizerVM = new TimetableViewModel(_sharedCourse)
            .DisposeWith(Disposables);

        _sharedCourse.Connect()
            .Transform(item => item.SharedData)
            .AutoRefresh(shared => shared.Credits)
            .Transform(shared => shared.Credits, transformOnRefresh: true)
            .Sum(credit => credit)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(sum => TotalCredits = sum)
            .DisposeWith(Disposables);

        _sharedCourse.CountChanged
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(count => SubjectsCount = count)
            .DisposeWith(Disposables);

        _sharedCourse.Connect()
            .Throttle(TimeSpan.FromSeconds(1))
            .Skip(1)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(_ =>
            {
                LastUpdated = DateTimeOffset.Now;
                _scheduleProfile.LastUpdated = LastUpdated;
            })
            .DisposeWith(Disposables);

        _sharedCourse.Connect()
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(async _ =>
            {
                await GeneratePreviewAsync();
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
            .ToProperty(this, nameof(IsEditing), initialValue: false, scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(Disposables);
        
    }

    public override ScheduleBlueprint ToScheduleBlueprint()
    {
        var savedRef = _scheduleProfile.SavedCourseGroupKeys;
        var courses = new List<Course>(savedRef.Count);

        foreach (var (courseCode, group) in savedRef)
        {
            var course = _courseQueryService.GetCourseSnapshot(courseCode);
            if (course is null) continue;

            var filteredSections = course.Sections
                .Where(section => string.Equals(section.Group, group, StringComparison.OrdinalIgnoreCase))
                .ToList();

            courses.Add(course.WithSections(filteredSections));
        }

        return new ScheduleBlueprint(courses, _scheduleProfile);
    }

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            foreach (var item in _sharedCourse.Items)
            {
                item.Dispose();
            }

            Debug.WriteLine($"TimetableEditorViewModel ID: {_scheduleProfile.Id}, Name: {Name} has been disposed");
        }

        base.Dispose(isDisposing);
    }
}