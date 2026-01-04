using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Models;
using CTUScheduler.AppServices.Services.ScheduleService;
using CTUScheduler.AppServices.Services.ScheduleService.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Features.Timetable.Mappers;
using CTUScheduler.Presentation.Features.Timetable.Models;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Timetable.ViewModels;

public class TimetableEditorViewModel: TimetableLayoutBaseViewModel
{
    private readonly ScheduleProfile _scheduleProfile;
    private readonly ObservableAsPropertyHelper<bool> _isEditing;
    public bool IsEditing => _isEditing.Value;

    public ReactiveCommand<Unit, Unit> StartEditCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public TimetableEditorViewModel(ScheduleProfile scheduleProfile, ICourseQueryService courseQueryService)
    {
        ArgumentNullException.ThrowIfNull(scheduleProfile, nameof(scheduleProfile));
        _scheduleProfile = scheduleProfile;
        Name = _scheduleProfile.Name;
        LastUpdated = _scheduleProfile.LastUpdated;
        
        var savedRef = _scheduleProfile.SavedCourseGroupKeys;
        var timetableItemsStream = courseQueryService.ConnectCourses()
            .Filter(x => savedRef.ContainsKey(x.Code))
            .MergeMany(runtimeCourse =>
            {
                string targetGroup = savedRef[runtimeCourse.Code];
                return runtimeCourse.Sections.Connect()
                    .Filter(s => s.Group == targetGroup)
                    .Transform(section => new { RuntimeCourse = runtimeCourse, Section = section });
            })
            .Transform(tuple => CreateRenderItem(tuple.RuntimeCourse, tuple.Section))
            .DisposeMany()
            .RemoveKey()
            .Publish();
        
        VisualizerVM = new TimetableViewModel(timetableItemsStream);
            
        timetableItemsStream.Connect().DisposeWith(Disposables);
        
        // Tính tổng tín chỉ tự động (Reactive)
        timetableItemsStream
            .Transform(x => x.SharedData.Credit)
            .ToCollection()
            .Subscribe(credits => TotalCredit = credits.Sum())
            .DisposeWith(Disposables);
        
        StartEditCommand = ReactiveCommand.Create(() => {}).DisposeWith(Disposables);
        SaveCommand = ReactiveCommand.Create(() =>
            {
                _scheduleProfile.Name = this.Name;
                _scheduleProfile.LastUpdated = DateTimeOffset.Now;
            }).DisposeWith(Disposables);
        CancelCommand = ReactiveCommand.Create(() =>
            {
                Name = _scheduleProfile.Name;
            }).DisposeWith(Disposables);

        _isEditing = Observable.Merge(
                StartEditCommand.Select(x => true),
                SaveCommand.Select(x => false),
                CancelCommand.Select(x => false)
            )
            .ToProperty(this, nameof(IsEditing), initialValue:false, scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(Disposables);
    }
    
    public TimetableRenderItem CreateRenderItem(RuntimeCourse course, CourseSection section)
    {
        var (shared, cells) = ScheduleUiMapper.ToScheduleCells(course.ToCourse(), section);
        shared.BackgroundColor = ColorProvider.GetColorForCourse(course.Code);
        return new TimetableRenderItem(shared, cells);
    }
    
    public ScheduleProfile ToModel() => _scheduleProfile;
}