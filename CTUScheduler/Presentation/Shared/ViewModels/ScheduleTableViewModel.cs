using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Base;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace CTUScheduler.Presentation.Shared.ViewModels;

public class ScheduleTableViewModel: ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ScheduleTable _scheduleTable;
    private string _name;
    private string _description;
    private readonly ObservableCollection<ScheduleCell> _scheduleCells;
    private DateTime _lastUpdated;
    private int _totalCredit;
    
    public ScheduleTable ScheduleTable => _scheduleTable;
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
    
    public ObservableCollection<ScheduleCell> ScheduleCells => _scheduleCells;
    
    public ScheduleTableViewModel(ScheduleTable scheduleTable)
    {
        _scheduleTable = scheduleTable;
        _name = scheduleTable.Name;
        _description = scheduleTable.Description;
        _lastUpdated = scheduleTable.LastUpdated;
        _totalCredit = scheduleTable.TotalCredit;
        _scheduleCells = new();
        
        ScheduleCells
            .ToObservableChangeSet()
            .OnItemAdded(cell =>
            {
                TotalCredit += cell.Credit;
            })
            .Subscribe()
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.Name,
                x => x.Description,
                x => x.TotalCredit)
            .Subscribe(_ =>
            {
                LastUpdated = DateTime.Now;
            })
            .DisposeWith(_disposables);
    }

    public void SaveChanges()
    {
        _scheduleTable.Name = Name;
        _scheduleTable.Description = Description;
        _scheduleTable.TotalCredit = TotalCredit;
        _scheduleTable.LastUpdated = LastUpdated;
    }

    public void AddScheduleCell(ScheduleCell scheduleCell)
    {
        ScheduleTable.Add(scheduleCell);
        ScheduleCells.Add(scheduleCell);
    }

    public void AddScheduleCell(IEnumerable<ScheduleCell> scheduleCell)
    {
        foreach (var cell in scheduleCell)
        {
            AddScheduleCell(cell);
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}