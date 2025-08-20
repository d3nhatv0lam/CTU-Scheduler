using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels;

namespace CTUScheduler.AppServices.Validators;

public class ScheduleValidator
{
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

    public bool IsOverlapTimeTable(CourseData x, CourseData y)
    {
        foreach (var xTime in x.ClassDayDatas)
        {
            foreach (var yTime in y.ClassDayDatas)
            {
                if (xTime.AttendingDay != yTime.AttendingDay) 
                    continue;

                if (IsConflict(xTime, yTime))
                    return true;
                // điều kiện cổ của không lập
                // if (xTime.GetStartPeriod() < yTime.GetStartPeriod() 
                //     && xTime.GetEndPeriod() < yTime.GetStartPeriod())
                //     continue;
                //
                // if (xTime.GetStartPeriod() > yTime.GetStartPeriod() 
                //     && xTime.GetStartPeriod() > yTime.GetEndPeriod()) 
                //     continue;  
                // return true;
            }
        }
        return false;
        
        bool IsConflict(ClassDayData x, ClassDayData y)
        {
            // Trùng nhau nếu KHÔNG thỏa điều kiện "một cái hoàn toàn trước hoặc sau cái kia"
            return !(x.GetEndPeriod() < y.GetStartPeriod() || y.GetEndPeriod() < x.GetStartPeriod());
        }
    }
    
}