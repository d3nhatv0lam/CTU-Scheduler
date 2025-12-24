using System.Collections.Generic;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.Timetable.Models;

namespace CTUScheduler.Presentation.Shared.Extensions;

public static class SectionChoiceExtension
{

    private static ScheduleGroupCellShared ToScheduleGroupCell(this SectionChoice sectionChoice)
    {
        return new ScheduleGroupCellShared()
        {
            CourseCode = sectionChoice.Course.Code,
            CourseName_VN = sectionChoice.Course.Name_VN,
            Group = sectionChoice.Section.Group,
            TotalStudents = sectionChoice.Section.TotalStudents,
            RemainingStudents = sectionChoice.Section.RemainingStudents,
            Lecturer = sectionChoice.Section.Lecturer,
            Credit = sectionChoice.Course.Credits,
        };
    }
    public static (ScheduleGroupCellShared, IEnumerable<ScheduleCellUi>) ToScheduleCells(this SectionChoice sectionChoice)
    {
        ScheduleGroupCellShared scheduleGroupCellShared = sectionChoice.ToScheduleGroupCell();
        List<ScheduleCellUi> cells = new();
        foreach (var classDayData in sectionChoice.Section.ClassDays)
        {
            ScheduleCellUi cellUi = new(scheduleGroupCellShared);
            
            cellUi.AttendingDay = classDayData.AttendingDay;
            cellUi.Room = classDayData.Room;
            cellUi.StartPeriod = classDayData.StartPeriod();
            cellUi.NumberOfPeriods = classDayData.PeriodCount();
            cells.Add(cellUi);
        }
        return (scheduleGroupCellShared, cells);
    }

}