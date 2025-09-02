using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;

namespace CTUScheduler.Core.Extensions;

public static class SectionChoiceExtension
{
    public static IEnumerable<ScheduleCellViewModel> ToScheduleCells(this SectionChoice sectionChoice)
    {
        List<ScheduleCellViewModel> cells = new();

        foreach (var classDayData in sectionChoice.Section.ClassDayDatas)
        {
            ScheduleCellViewModel cellViewModel = new();
            cellViewModel.CourseCode = sectionChoice.Course.Code;
            cellViewModel.CourseName_VN = sectionChoice.Course.Name_VN;
            cellViewModel.Group = sectionChoice.Section.Group;
            cellViewModel.TotalStudents = sectionChoice.Section.TotalStudents;
            cellViewModel.RemainingStudents = sectionChoice.Section.RemainingStudents;
            cellViewModel.Lecturer = sectionChoice.Section.Lecturer;
            cellViewModel.Credit = sectionChoice.Course.Credit;
            
            cellViewModel.AttendingDay = classDayData.AttendingDay;
            cellViewModel.Room = classDayData.Room;
            cellViewModel.StartPeriod = classDayData.GetStartPeriod();
            cellViewModel.NumberOfPeriods = classDayData.GetPeriodCount();
            cells.Add(cellViewModel);
        }

        return cells;
    }

}