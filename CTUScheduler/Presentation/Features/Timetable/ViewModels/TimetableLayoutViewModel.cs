using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Timetable.Resources;
using CTUScheduler.Presentation.Shared.Extensions;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Timetable.ViewModels;

public class TimetableLayoutViewModel: ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ScheduleTable _scheduleTable;
    private readonly TimetableViewModel _timeTableVM;
    private string _name;
    private string _description;
    private DateTime _lastUpdated;
    private int _totalCredit;
    
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

    public int SubjectsCount => _scheduleTable.ScheduleData.Count;
    public int TotalCredit
    {
        get => _totalCredit;
        set => this.RaiseAndSetIfChanged(ref _totalCredit, value);
    }

    public DateTime LastUpdated
    {
        get => _lastUpdated;
        set => this.RaiseAndSetIfChanged(ref _lastUpdated, value);
    }
    
    public TimetableLayoutViewModel(ScheduleTable scheduleTable)
    {
        _scheduleTable = scheduleTable;
        _name = scheduleTable.Name;
        _description = scheduleTable.Description;
        _lastUpdated = scheduleTable.LastUpdated;
        _timeTableVM = new TimetableViewModel();

        // BuildTimetable();
        
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

    private void BuildTimetable()
    {
        
    }
    
    public void AddCourseSectionToTable(SectionChoice choice)
    { 
        var (groupCellShared, cells) = choice.ToScheduleCells();
        var cellList = cells.ToList();
        if (cellList.Count == 0 || !_scheduleTable.TryAddToScheduleData(choice))
           return;
        TotalCredit += groupCellShared.Credit;
        _timeTableVM.AddCells(groupCellShared, cellList);
    }
    
    
    public ScheduleTable ToModel() => _scheduleTable;

    public void Dispose()
    {
        TimeTableVM.Dispose();
        _disposables.Dispose();
    }
}