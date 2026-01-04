using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Services.ScheduleService.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;
using CTUScheduler.Presentation.Features.TimetableRefactor.Adapters;
using DynamicData;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

public class TimetableEditorViewModel: TimetableLayoutBaseViewModel
{
    private readonly ScheduleProfile _scheduleProfile;
    private readonly ObservableAsPropertyHelper<bool> _isEditing;
    public bool IsEditing => _isEditing.Value;

    public ReactiveCommand<Unit, Unit> StartEditCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public TimetableEditorViewModel(ScheduleProfile scheduleProfile, ICourseQueryService  courseQueryService)
    {
        ArgumentNullException.ThrowIfNull(scheduleProfile, nameof(scheduleProfile));
        _scheduleProfile = scheduleProfile;
        Name = _scheduleProfile.Name;
        LastUpdated = _scheduleProfile.LastUpdated;
        
        var savedRef = scheduleProfile.SavedCourseGroupKeys;
        var itemsStream = courseQueryService.ConnectCourses()
            .Filter(x => savedRef.ContainsKey(x.Code))
            .MergeMany(runtimeCourse =>
            {
                string targetGroup = savedRef[runtimeCourse.Code];
                return runtimeCourse.Sections.Connect()
                    .Filter(s => s.Group == targetGroup)
                    .Transform(section => new { RuntimeCourse = runtimeCourse, Section = section });
            })
            .Transform(t => 
            {
                var adapter = new LiveCourseAdapter(t.RuntimeCourse, t.Section);
                return CreateRenderItem(adapter);
            })
            .DisposeMany()
            .RemoveKey()
            .Publish();
        
        VisualizerVM = new TimetableViewModel(itemsStream);
        itemsStream.Connect().DisposeWith(Disposables);
        
        itemsStream.Transform(x => x.SharedData.Credit).ToCollection()
            .Subscribe(x => TotalCredit = x.Sum()).DisposeWith(Disposables);
        
        
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
                StartEditCommand.Select(_ => true),
                SaveCommand.Select(_ => false),
                CancelCommand.Select(_ => false)
            )
            .ToProperty(this, nameof(IsEditing), initialValue:false, scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(Disposables);
    }
}