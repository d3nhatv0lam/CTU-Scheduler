using System;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

public interface ICourseManager
{
    public void RegisterTimetable(IEnumerable<Course> courses, ScheduleProfile table);
    public void RegisterTimetables(IEnumerable<Course> courses, IEnumerable<ScheduleProfile> tables);
    public void UnregisterTimetable(ScheduleProfile table);
    
    /// <summary>
    /// If course does not exist, do nothing.
    /// If course exist, update it
    /// </summary>
    /// <param name="course"></param>
    public void UpdateCourse(Course course);
    public void UpdateCourses(IEnumerable<Course> courses);
    public void ClearAll();
    
    public Course? GetCourse(string code);
    public CourseSection? GetCourseSection(string code, string group);
    public SectionChoice? GetSectionChoice(string code, string group);
    public List<Course> GetAllCourses();
    
    /// <summary>
    /// Get all course code - all groups of these courses in Manager via Key
    /// </summary>
    /// <returns></returns>
    public IEnumerable<(string courseCode, IEnumerable<string> groups)> GetAllCourseSectionGroupKeys();
}