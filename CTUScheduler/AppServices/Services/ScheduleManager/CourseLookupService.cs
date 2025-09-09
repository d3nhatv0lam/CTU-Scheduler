using System.Collections.Generic;
using CTUScheduler.AppServices.Services.User;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.UserSaves;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

public class CourseLookupService: ICourseLookupService
{
    private readonly Dictionary<string, Course> _courseMap = new();
    private readonly Dictionary<string, CourseSection> _sectionMap = new();
    
    public void BuildLookupMaps(ScheduleSave data)
    {
        _courseMap.Clear();
        _sectionMap.Clear();
        foreach (var course in data.Courses)
        {
            // Map course code to course
            _courseMap[course.Code] = course;

            // Map section keys to sections
            foreach (var section in course.Sections)
            {
                var sectionKey = $"{course.Code}-{section.Group}";
                _sectionMap[sectionKey] = section;
            }
        }
    }

    public Course? GetCourse(string code)
    {
        _courseMap.TryGetValue(code, out var course);
        return course;
    }

    public CourseSection? GetSection(string courseCode, string group)
    {
        var key = $"{courseCode}-{group}";
        _sectionMap.TryGetValue(key, out var section);
        return section;
    }

    public List<CourseSection?> GetScheduledCourses(ScheduleTable scheduleTable)
    {
        var results = new List<CourseSection?>();

        foreach (var (courseCode, group) in scheduleTable.ScheduleData)
        {
            var section = GetSection(courseCode, group);
                results.Add(section);
        }
        return results;
    }
}