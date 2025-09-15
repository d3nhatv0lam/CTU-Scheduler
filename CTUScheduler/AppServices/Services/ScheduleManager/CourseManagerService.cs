using System;
using System.Collections.Generic;
using System.Linq;
using CTUScheduler.AppServices.Mappers;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

public class CourseManagerService: ICourseManagerService, IDisposable
{
    private readonly CourseMapper _courseMapper = new();
    private readonly SourceCache<EditableCourse, string> _courses;

    public CourseManagerService()
    {
        _courses = new SourceCache<EditableCourse, string>(course => course.Code);
    }
    
    public void AddOrUpdateCourse(Course course)
    {
        var lookup = _courses.Lookup(course.Code);
        if (lookup.HasValue)
        {
            lookup.Value.Sections.AddOrUpdate(course.Sections);
        }
        else
        {
            var editableCourse = _courseMapper.ToEditableCourse(course);
            _courses.AddOrUpdate(editableCourse);
        }
    }

    public void AddOrUpdateCourse(IEnumerable<Course> courses)
    {
        foreach (var course in courses)
        {
            AddOrUpdateCourse(course);
        }
    }

    public void RemoveCourse(string code)
    {
        _courses.Remove(code);
    }

    public void RemoveSection(string code, string group)
    {  
        var courseLookup = _courses.Lookup(code);
        if (!courseLookup.HasValue) return;
        courseLookup.Value.Sections.Remove(group);
        if (courseLookup.Value.Sections.Count == 0)
            RemoveCourse(code);
    }

    public void Clear()
    {
        _courses.Clear();
    }

    public Course? GetCourse(string code)
    {
        var lookup = _courses.Lookup(code);
        if (!lookup.HasValue) return null;
        return _courseMapper.ToCourse(lookup.Value);
    }

    public CourseSection? GetSection(string courseCode, string group)
    {
        var lookup = _courses.Lookup(courseCode);
        if (!lookup.HasValue) return null;
        var sectionLookup = lookup.Value.Sections.Lookup(group);
        if (!sectionLookup.HasValue) return null;
        return sectionLookup.Value;
    }

    public List<Course> GetCourses()
    {
        return _courses.Items.Select(x => _courseMapper.ToCourse(x)).ToList();
    }

    public IEnumerable<(string, IEnumerable<string>)> GetCourseGroups()
    {
        return _courses.Items.Select(x => (x.Code, x.Sections.Keys));
    }

    public void Dispose()
    {
        _courses.Dispose();
    }
}