using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.ExceptionServices;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Timetable.Interfaces;
using CTUScheduler.Presentation.Features.Timetable.Resources;
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
    private readonly ScheduleTable _scheduleTable;
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

    public int SubjectsCount => _scheduleTable.SavedCourseGroupKeys.Count;
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

    public TimetableLayoutViewModel(ScheduleTable scheduleTable)
    {
        _scheduleTable = scheduleTable;
        _name = scheduleTable.Name;
        _description = scheduleTable.Description;
        _lastUpdated = scheduleTable.LastUpdated;
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
        
        this.WhenAnyValue(x => x.Name)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(newName => _scheduleTable.Name = newName)
            .DisposeWith(_disposables);
        
        this.WhenAnyValue(x => x.Description)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(newDescription => _scheduleTable.Description = newDescription)
            .DisposeWith(_disposables);
        

        this.WhenAnyValue(x => x.Name,
                x => x.Description,
                x => x.TotalCredit)
            .Subscribe(_ =>
            {
                LastUpdated = DateTime.Now;
                _scheduleTable.LastUpdated = LastUpdated;
            })
            .DisposeWith(_disposables);
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

    public void ApplyUpdatedTimetableData(IEnumerable<CourseSection> sections)
    {
        foreach (var section in sections)
        {
            TimeTableVM.UpdateGroupCells(section);
        }
        LastUpdated = DateTime.Now;
    }

    public void ApplyBuildTimetableData(IEnumerable<SectionChoice> choices)
    {
        foreach (var choice in choices)
        {
            var (groupCellShared, cells) = choice.ToScheduleCells();
            TotalCredit += groupCellShared.Credit;
            _timeTableVM.AddCells(groupCellShared, cells);
        }
    }
    
    public void TryAddSectionChoice(SectionChoice choice)
    { 
        var (groupCellShared, cells) = choice.ToScheduleCells();
        var cellList = cells.ToList();
        if (cellList.Count == 0 || !_scheduleTable.TryAddToScheduleData(choice))
           return;
        TotalCredit += groupCellShared.Credit;
        
        var courseWithNewSection = choice.Course.CloneWithNewCourseSections([choice.Section]);
        
        _scheduleCourseData.Add(courseWithNewSection.Code, courseWithNewSection);
        _timeTableVM.AddCells(groupCellShared, cellList);
    }
    
    public IEnumerable<Tuple<string,string>> GetScheduleData() => _scheduleTable.SavedCourseGroupKeys.Select(x => new Tuple<string, string>(x.Key, x.Value));
    public ScheduleTableBuildData GetScheduleSaveData() => new (_scheduleCourseData.Values.ToList(), _scheduleTable);
    public ScheduleTable ToModel() => _scheduleTable;

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