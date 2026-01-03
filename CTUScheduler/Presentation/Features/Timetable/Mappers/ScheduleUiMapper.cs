using System.Collections.Generic;
using System.Collections.Specialized;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.Timetable.Models;

namespace CTUScheduler.Presentation.Features.Timetable.Mappers;

public static class ScheduleUiMapper
{
    public static (ScheduleGroupCellShared, IEnumerable<ScheduleCellUi>) ToScheduleCells(
        Course course, 
        CourseSection section)
    {
        var shared = new ScheduleGroupCellShared()
        {
            CourseCode = course.Code,
            CourseName_VN = course.Name_VN,
            Group = section.Group,
            TotalStudents = section.TotalStudents,
            RemainingStudents = section.RemainingStudents,
            Lecturer = section.Lecturer,
            Credit = course.Credits,
        };
        List<ScheduleCellUi> cells = new();
        foreach (var classDayData in section.ClassDays)
        {
            ScheduleCellUi cellUi = new(shared);
            
            cellUi.AttendingDay = classDayData.AttendingDay;
            cellUi.Room = classDayData.Room;
            cellUi.StartPeriod = classDayData.StartPeriod();
            cellUi.NumberOfPeriods = classDayData.PeriodCount();
            cells.Add(cellUi);
        }
        return (shared, cells);
    }
    
    public static (ScheduleGroupCellShared, IEnumerable<ScheduleCellUi>) ToScheduleCells(this SectionChoice sectionChoice)
    {
       return ToScheduleCells(sectionChoice.Course, sectionChoice.Section);
    }
}