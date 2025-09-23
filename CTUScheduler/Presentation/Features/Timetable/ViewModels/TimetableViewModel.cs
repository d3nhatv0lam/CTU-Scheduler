using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Timetable.Models;
using CTUScheduler.Presentation.Features.Timetable.Resources;
using DynamicData;

namespace CTUScheduler.Presentation.Features.Timetable.ViewModels;

public class TimetableViewModel : ViewModelBase, IDisposable
{
    // Used to update the group cells in ScheduleCells
    private Dictionary<string, ScheduleGroupCellShared> GroupCells { get; } = new();
    public ObservableCollection<ScheduleCellUi> ScheduleCells { get; } = new();

    public void AddCells(ScheduleGroupCellShared groupCellShared, IEnumerable<ScheduleCellUi> cells)
    {
        var key = $"{groupCellShared.CourseCode}-{groupCellShared.Group}";
        GroupCells.Add(key, groupCellShared);
        
        var cellsColor = ColorPalettes.Colors[GroupCells.Count - 1];
        groupCellShared.BackgroundColor = cellsColor;
        ScheduleCells.AddRange(cells);
    }

    public void UpdateGroupCells(CourseSection section)
    {
        var key = $"{section.Code}-{section.Group}";
        if (!GroupCells.TryGetValue(key, out var groupCellShared)) return;
        groupCellShared.RemainingStudents = section.RemainingStudents;
        groupCellShared.TotalStudents = section.TotalStudents;
    }

    public void Dispose()
    {
        foreach (var groupCellShared in GroupCells.Values)
            groupCellShared.Dispose();
    }
}