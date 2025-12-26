using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
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
    public bool IsValidTimeTableFromRaw(IReadOnlyList<SectionChoice> timeTableData)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (timeTableData is null)
            return false;
        switch (timeTableData.Count)
        {
            case 0:
            case 1:
                return true;
        }
        // Get new courseData from last section choice
        var (_, courseData) = timeTableData[^1];
        for (int i = 0; i < timeTableData.Count - 1; i++)
        {
            if (IsOverlapTimeTable(courseData, timeTableData[i].Section))
                return false;
        }
        return true;
    }

    public bool IsValidTimetable(ScheduleProfile scheduleProfile, Dictionary<(string code,string group),CourseSection> courseSectionDictionary)
    {
        if (scheduleProfile is null || courseSectionDictionary is null)
            return false;
        
        var buildSections = new List<CourseSection>();
        foreach (var (code, group) in scheduleProfile.SavedCourseGroupKeys)
        {
            if (!courseSectionDictionary.TryGetValue((code, group), out var section))
                return false;
            foreach (var buildSection in buildSections)
            {
                if (IsOverlapTimeTable(section, buildSection))
                    return false;
            }
            buildSections.Add(section);
        }
        return true;
    }

    public bool IsOverlapTimeTable(CourseSection x, CourseSection y)
    {
        if (x.ClassDays.Count == 0 || y.ClassDays.Count == 0) return false;
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
    }
    
    public bool IsConflict(ClassDay x, ClassDay y)
    {
        // Trùng nhau nếu KHÔNG thỏa điều kiện "một cái hoàn toàn trước hoặc sau cái kia"
        return !(x.EndPeriod() < y.StartPeriod() || y.EndPeriod() < x.StartPeriod());
    }
    
    /// <summary>
    ///  Check existed timetable in a list by comparing saved course group keys
    /// </summary>
    /// <param name="table"></param>
    /// <param name="tables"></param>
    /// <returns></returns>
    public bool IsExistedTimetable(ScheduleProfile table, IEnumerable<ScheduleProfile> tables)
        => tables.Any(x => table.SavedCourseGroupKeys.DictionaryEquals(x.SavedCourseGroupKeys));
}