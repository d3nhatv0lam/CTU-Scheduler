using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text.Json;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Timetable.Models;
using CTUScheduler.Presentation.Features.Timetable.Resources;
using DynamicData;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Timetable.ViewModels;

public class TimetableViewModel : ViewModelBase, IDisposable
{
    // Used to update the group cells in ScheduleCells
    private Dictionary<string, ScheduleGroupCellShared> GroupCells { get; } = new();
    public ObservableCollection<ScheduleCellUi> ScheduleCells { get; } = new();

    public void AddCells(ScheduleGroupCellShared groupCellShared, List<ScheduleCellUi> cells)
    {
        GroupCells.Add(groupCellShared.CourseCode, groupCellShared);
        
        var cellsColor = ColorPalettes.Colors[GroupCells.Count - 1];
        groupCellShared.BackgroundColor = cellsColor;
        ScheduleCells.AddRange(cells);
    }

    public void Dispose()
    {
        foreach (var groupCellShared in GroupCells.Values)
            groupCellShared.Dispose();
    }
}