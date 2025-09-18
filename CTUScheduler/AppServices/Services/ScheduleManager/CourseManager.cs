using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using CTUScheduler.AppServices.Mappers;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Shared;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

public class CourseManager: ICourseManager, IDisposable
{
    private readonly CourseMapper _courseMapper = new();
    private readonly SourceCache<EditableCourse, string> _courses;
    private readonly ConcurrentDictionary<(string courseCode, string courseGroup),int> _courseSectionRefCount = new();

    public CourseManager()
    {
        _courses = new SourceCache<EditableCourse, string>(course => course.Code);
    }
    
    public IDisposable RegisterTimetable(IEnumerable<Course> courses)
    {
        var disposables = new CompositeDisposable();
        foreach (var course in courses)
        {
            AddOrUpdateCourse(course);
            foreach (var section in course.Sections)
            {
                RegisterSection(course.Code, section.Group);
                Disposable.Create(() =>
                {
                    UnregisterSection(course.Code, section.Group);
                }).DisposeWith(disposables);
            }
        }
        return disposables;
    }
    
    public void UpdateCourse(Course course)
    {
        var lookup = _courses.Lookup(course.Code);
        if (lookup.HasValue)
        {
            lookup.Value.Sections.AddOrUpdate(course.Sections);
        }
    }

    public void UpdateCourses(IEnumerable<Course> courses)
    {
        foreach (var course in courses)
        {
            UpdateCourse(course);
        }
    }
    
    public void ClearAll()
    {
        _courses.Clear();
        _courseSectionRefCount.Clear();
    }

    public Course? GetCourse(string code)
    {
        var lookup = _courses.Lookup(code);
        if (!lookup.HasValue) return null;
        return _courseMapper.ToCourse(lookup.Value);
    }

    public CourseSection? GetCourseSection(string code, string group)
    {
        var lookup = _courses.Lookup(code);
        if (!lookup.HasValue) return null;
        var sectionLookup = lookup.Value.Sections.Lookup(group);
        if (!sectionLookup.HasValue) return null;
        return sectionLookup.Value;
    }
    
    public SectionChoice? GetSectionChoice(string code, string group)
    {
        var courseLookup = _courses.Lookup(code);
        if (!courseLookup.HasValue) return null;
        var sectionLookup = courseLookup.Value.Sections.Lookup(group);
        if (!sectionLookup.HasValue) return null;
        return new SectionChoice(_courseMapper.ToCourse(courseLookup.Value), sectionLookup.Value);
    }

    public List<Course> GetAllCourses()
    {
        return _courses.Items.Select(x => _courseMapper.ToCourse(x)).ToList();
    }

    public IEnumerable<(string courseCode, IEnumerable<string> groups)> GetAllCourseSectionGroupKeys()
    {
        return _courses.Items.Select(x => (x.Code, x.Sections.Keys));
    }
    
    private void RegisterSection(string courseCode, string coursGroup)
    {
        var key = (courseCode, coursGroup);
        _courseSectionRefCount[key] = _courseSectionRefCount.GetValueOrDefault(key, 0) + 1;
    }
    
    private void UnregisterSection(string courseCode, string courseGroup)
    {
        var key = (courseCode, courseGroup);
        if (!_courseSectionRefCount.TryGetValue(key, out var count)) return;
        if (count <= 1)
        {
            _courseSectionRefCount.Remove(key, out _);
            RemoveSectionFromCourse(courseCode, courseGroup);
        }
        else
        {
            _courseSectionRefCount[key] = count - 1;
        }
    }
    
    private void RemoveCourse(string code)
    {
        _courses.Remove(code);
    }

    private void RemoveSectionFromCourse(string code, string group)
    {  
        var courseLookup = _courses.Lookup(code);
        if (!courseLookup.HasValue) return;
        courseLookup.Value.Sections.Remove(group);
        if (courseLookup.Value.Sections.Count == 0)
            RemoveCourse(code);
    }

    private void AddOrUpdateCourse(Course course)
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

    public void Dispose()
    {
        _courseSectionRefCount.Clear();
        _courses.Dispose();
    }
}