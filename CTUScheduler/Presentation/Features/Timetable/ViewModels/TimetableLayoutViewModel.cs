using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Timetable.Resources;
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
    
    public ScheduleTable ScheduleTable => _scheduleTable;
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

    public int SubjectsCount => ScheduleTable.ScheduleData.Count;
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

        this.WhenAnyValue(x => x.Name)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(newName => ScheduleTable.Name = newName)
            .DisposeWith(_disposables);
        
        this.WhenAnyValue(x => x.Description)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(newDescription => ScheduleTable.Name = newDescription)
            .DisposeWith(_disposables);
        

        this.WhenAnyValue(x => x.Name,
                x => x.Description,
                x => x.TotalCredit)
            .Subscribe(_ =>
            {
                LastUpdated = DateTime.Now;
                ScheduleTable.LastUpdated = LastUpdated;
            })
            .DisposeWith(_disposables);
    }
    
    public void AddCourseSectionToTable(SectionChoice choice)
    {
       var listCell= choice.ToScheduleCells().ToList();
       if (listCell.Count == 0 || !ScheduleTable.TryAddToScheduleData(choice))
           return;

       var cellColor = ColorPalettes.Colors[SubjectsCount - 1];
       TotalCredit += listCell[0].Credit;
       foreach (var cell in listCell)
       {
           cell.BackgroundColor = cellColor;
           _timeTableVM.ScheduleCells.Add(cell);
       }
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}