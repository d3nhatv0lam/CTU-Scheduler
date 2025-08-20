using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Extensions;

public static class SectionChoiceExtension
{
    public static IEnumerable<ScheduleCell> ToScheduleCells(this SectionChoice sectionChoice)
    {
        List<ScheduleCell> cells = new();

        foreach (var classDayData in sectionChoice.Section.ClassDayDatas)
        {
            ScheduleCell cell = new();
            cell.CourseCode = sectionChoice.Course.Code;
            cell.CourseName_VN = sectionChoice.Course.Name_VN;
            cell.Group = sectionChoice.Section.Group;
            cell.TotalStudents = sectionChoice.Section.TotalStudents;
            cell.RemainingStudents = sectionChoice.Section.RemainingStudents;
            cell.Lecturer = sectionChoice.Section.Lecturer;
            cell.Credit = sectionChoice.Course.Credit;
            
            cell.AttendingDay = classDayData.AttendingDay;
            cell.StartPeriod = classDayData.GetStartPeriod();
            cell.NumberOfPeriods = classDayData.GetPeriodCount();
            cells.Add(cell);
        }

        return cells;
    }

}