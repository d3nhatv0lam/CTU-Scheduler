using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Timetable.Interfaces;
using CTUScheduler.Presentation.Features.Timetable.Models;
using CTUScheduler.Presentation.Features.Timetable.Resources;
using CTUScheduler.Presentation.Features.Timetable.Views;
using CTUScheduler.Presentation.Helpers;
using CTUScheduler.Presentation.Shared.Extensions;
using ReactiveUI;
using Serilog;

namespace CTUScheduler.Presentation.Features.Timetable.ViewModels;

public class TimetableLayoutViewModel: 
    ViewModelBase,
    IRequestBuildTimetable, 
    IUpdatable, 
    IRequestUpdate<TimetableLayoutViewModel>, 
    IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ScheduleProfile _scheduleProfile;
    private readonly TimetableViewModel _timeTableVM;
    private readonly Dictionary<string, Course> _scheduleCourseData = new();
    private string _name;
    private string _description;
    private DateTime _lastUpdated;
    private int _totalCredit;
    
    public event Action<TimetableLayoutViewModel>? BuildTimetableRequested;
    public event Action<TimetableLayoutViewModel>? UpdateRequested;
    
    public TimetableViewModel TimeTableVM => _timeTableVM;
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }
    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    public int SubjectsCount => _scheduleProfile.SavedCourseGroupKeys.Count;
    public int TotalCredit
    {
        get => _totalCredit;
        private set => this.RaiseAndSetIfChanged(ref _totalCredit, value);
    }

    public DateTime LastUpdated
    {
        get => _lastUpdated;
        private set => this.RaiseAndSetIfChanged(ref _lastUpdated, value);
    }
    
    public ReactiveCommand<TimetableLayoutView,Unit> SaveLayoutToImageCommand { get; }

    public TimetableLayoutViewModel(Core.Models.Academic.Curriculum.Schedule.ScheduleProfile scheduleProfile)
    {
        _scheduleProfile = scheduleProfile;
        _name = scheduleProfile.Name;
        _description = scheduleProfile.Description;
        _lastUpdated = scheduleProfile.LastUpdated;
        _timeTableVM = new TimetableViewModel();
        
        // init color palette for schedule table
        if (!ColorPalettes.IsInitialized)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                var _ = ColorPalettes.Colors;
            });
            ColorPalettes.IsInitialized = true;
        }
        
        this.WhenAnyValue(x => x.Name, x => x.Description)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(tuple => 
            {
                var (newName, newDesc) = tuple;
                _scheduleProfile.Name = newName;
                _scheduleProfile.Description = newDesc;
                
                _scheduleProfile.LastUpdated = DateTime.Now;
                LastUpdated = _scheduleProfile.LastUpdated;
            })
            .DisposeWith(_disposables);

        SaveLayoutToImageCommand = ReactiveCommand.CreateFromTask<TimetableLayoutView>(async control =>
        {
            var clone = new TimetableLayoutView();
            clone.DataContext = control.DataContext;
            
            await OffscreenRenderHelper.SaveToFile(clone,"D:/test.png",1500,900);
        }).DisposeWith(_disposables);
    }

    private void AddUnScheduleCourse(ScheduleGroupCellShared groupCellShared)
    {
        TimeTableVM.AddUnscheduledSubject(groupCellShared);
        TotalCredit += groupCellShared.Credit;
    }

    private void AddScheduleCell(ScheduleGroupCellShared groupCellShared, List<ScheduleCellUi> cells)
    {
        _timeTableVM.AddCells(groupCellShared, cells);
        TotalCredit += groupCellShared.Credit;
    }

    private void AddSchedule(SectionChoice choice)
    {
        var (groupCellShared, cells) = choice.ToScheduleCells();
        var cellList = cells.ToList();
        if (cellList.Count == 0)
        {
            AddUnScheduleCourse(groupCellShared);
        }
        else
        {
            AddScheduleCell(groupCellShared, cellList);
        }
    }

    protected virtual void OnRequestUpdate()
    {
        UpdateRequested?.Invoke(this);
    }

    protected virtual void OnBuildTimetable()
    {
        BuildTimetableRequested?.Invoke(this);
    }

    public void BuildTimetable()
    {
        OnBuildTimetable();
    }
    public void Update()
    {
        OnRequestUpdate();
    }
    
    public void ApplyBuildTimetableData(IEnumerable<SectionChoice> choices)
    {
        foreach (var choice in choices)
        {
            this.AddSchedule(choice);
        }
    }

    public void ApplyUpdatedTimetableData(IEnumerable<CourseSection> sections)
    {
        foreach (var section in sections)
        {
            TimeTableVM.UpdateGroupCells(section);
        }
        LastUpdated = DateTime.Now;
    }
    
    public void TryAddSectionChoice(SectionChoice choice)
    { 
        if (!_scheduleProfile.TryAddToScheduleProfile(choice))
           return;
        var courseWithNewSection = choice.Course.WithSections([choice.Section]);
        _scheduleCourseData.Add(courseWithNewSection.Code, courseWithNewSection);
        
       AddSchedule(choice);
    }
    
    public IEnumerable<Tuple<string,string>> GetScheduleData() => _scheduleProfile.SavedCourseGroupKeys.Select(x => new Tuple<string, string>(x.Key, x.Value));
    public ScheduleBlueprint GetScheduleBlueprint() => new (_scheduleCourseData.Values.ToList(), _scheduleProfile);
    public ScheduleProfile ToModel() => _scheduleProfile;

    public void AddDisposable(IDisposable disposable)
    {
        disposable.DisposeWith(_disposables);
    }

    public void Dispose()
    {
        Log.Information("TimetableLayoutViewModel: Disposed");
        _scheduleCourseData.Clear();
        TimeTableVM.Dispose();
        _disposables.Dispose();
    }
}