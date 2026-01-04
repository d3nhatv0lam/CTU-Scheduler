using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Timetable.Models;
using CTUScheduler.Presentation.Features.Timetable.Resources;
using DynamicData;

namespace CTUScheduler.Presentation.Features.Timetable.ViewModels;

public class TimetableViewModel1 : ViewModelBase, IDisposable
{
    // Used to update the group cells in ScheduleCells
    private Dictionary<string, ScheduleGroupCellShared> GroupCellsDict { get; } = new();
    public ObservableCollection<ScheduleCellUi> ScheduleCells { get; } = new();
    public ObservableCollection<ScheduleGroupCellShared> UnscheduledCourse { get; } = new();

    private void AddGroupCell(ScheduleGroupCellShared groupCellShared)
    {
        var key = $"{groupCellShared.CourseCode}-{groupCellShared.Group}";
        GroupCellsDict.Add(key, groupCellShared);
    }
    
    public void AddCells(ScheduleGroupCellShared groupCellShared, IEnumerable<ScheduleCellUi> cells)
    {
        AddGroupCell(groupCellShared); // ( Add to GroupCellsDict)
        var cellsColor = ColorPalettes.Colors[GroupCellsDict.Count - 1];
        groupCellShared.BackgroundColor = cellsColor;
        ScheduleCells.AddRange(cells);
    }

    public void AddUnscheduledSubject(ScheduleGroupCellShared groupCellShared)
    {
        AddGroupCell(groupCellShared);
        UnscheduledCourse.Add(groupCellShared);
    }
    
    public void UpdateGroupCells(CourseSection section)
    {
        var key = $"{section.Code}-{section.Group}";
        if (!GroupCellsDict.TryGetValue(key, out var groupCellShared)) return;
        groupCellShared.RemainingStudents = section.RemainingStudents;
        groupCellShared.TotalStudents = section.TotalStudents;
    }

    public void Dispose()
    {
        foreach (var groupCellShared in GroupCellsDict.Values)
            groupCellShared.Dispose();
    }
}