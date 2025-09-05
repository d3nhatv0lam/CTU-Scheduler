using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels;

namespace CTUScheduler.AppServices.Validators;

public class ScheduleValidator
{
    /// <summary>
    /// Check valid timetable from raw data
    /// </summary>
    /// <param name="timeTableData">
    /// List of section choice, used to build timetable
    /// </param>
    /// <returns>
    /// True if valid, false if invalid
    /// </returns>
    public bool IsValidTimeTableFromRaw(List<SectionChoice> timeTableData)
    {
        switch (timeTableData.Count)
        {
            case 0:
            case 1:
                return true;
        }
        
        var (_, courseData) = timeTableData[^1];
        for (int i = 0; i < timeTableData.Count - 1; i++)
        {
            if (IsOverlapTimeTable(courseData, timeTableData[i].Section))
                return false;
        }
        return true;
    }

    public bool IsOverlapTimeTable(CourseSection x, CourseSection y)
    {
        foreach (var xTime in x.ClassDays)
        {
            foreach (var yTime in y.ClassDays)
            {
                if (xTime.AttendingDay != yTime.AttendingDay) 
                    continue;

                if (IsConflict(xTime, yTime))
                    return true;
                // old condition
                // if (xTime.StartPeriod() < yTime.StartPeriod() 
                //     && xTime.EndPeriod() < yTime.StartPeriod())
                //     continue;
                //
                // if (xTime.StartPeriod() > yTime.StartPeriod() 
                //     && xTime.StartPeriod() > yTime.EndPeriod()) 
                //     continue;  
                // return true;
            }
        }
        return false;
        
        bool IsConflict(ClassDay x, ClassDay y)
        {
            // Trùng nhau nếu KHÔNG thỏa điều kiện "một cái hoàn toàn trước hoặc sau cái kia"
            return !(x.EndPeriod() < y.StartPeriod() || y.EndPeriod() < x.StartPeriod());
        }
    }
    
}