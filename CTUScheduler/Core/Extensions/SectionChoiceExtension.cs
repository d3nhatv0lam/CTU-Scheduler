using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.Timetable.Models;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;

namespace CTUScheduler.Core.Extensions;

public static class SectionChoiceExtension
{
    public static IEnumerable<ScheduleCellUi> ToScheduleCells(this SectionChoice sectionChoice)
    {
        List<ScheduleCellUi> cells = new();

        foreach (var classDayData in sectionChoice.Section.ClassDays)
        {
            ScheduleCellUi cellUi = new();
            cellUi.CourseCode = sectionChoice.Course.Code;
            cellUi.CourseName_VN = sectionChoice.Course.Name_VN;
            cellUi.Group = sectionChoice.Section.Group;
            cellUi.TotalStudents = sectionChoice.Section.TotalStudents;
            cellUi.RemainingStudents = sectionChoice.Section.RemainingStudents;
            cellUi.Lecturer = sectionChoice.Section.Lecturer;
            cellUi.Credit = sectionChoice.Course.Credit;
            
            cellUi.AttendingDay = classDayData.AttendingDay;
            cellUi.Room = classDayData.Room;
            cellUi.StartPeriod = classDayData.StartPeriod();
            cellUi.NumberOfPeriods = classDayData.PeriodCount();
            cells.Add(cellUi);
        }

        return cells;
    }

}