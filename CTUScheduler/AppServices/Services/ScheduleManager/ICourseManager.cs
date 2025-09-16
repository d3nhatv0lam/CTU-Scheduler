using System.Collections.Generic;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Shared;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

internal interface ICourseManager
{
    public void AddOrUpdateCourse(Course course);
    public void AddOrUpdateCourse(IEnumerable<Course> courses);
    public void RemoveCourse(string code);
    public void RemoveSection(string code, string group);
    public void Clear();
    
    public Course? GetCourse(string code);
    public CourseSection? GetSection(string code, string group);
    public SectionChoice? GetSectionChoice(string code, string group);
    
    public List<Course> GetCourses();
    
    public IEnumerable<(string, IEnumerable<string>)> GetCourseGroups();
}