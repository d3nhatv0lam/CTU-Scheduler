using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Helpers.Json;
using CTUScheduler.AppServices.Services.User;
using CTUScheduler.AppServices.Services.WebDriver;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using DynamicData;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

public class ScheduleService: IScheduleService, ICourseScheduleService, IDisposable
{
    protected const int MAX_TIMETABLE_COUNT_LIMIT = 10;
    private readonly CompositeDisposable _disposables = new ();
    private readonly ILogger<ScheduleService> _logger;
    private readonly IUserDataService _userDataService;
    private readonly ICourseManager _courseManager;
    private readonly ICTUWebDriverService _CTUWebDriverService;
    
    private readonly SourceList<ScheduleTable> _data = new();
    private readonly Subject<bool> _isReloadingSubject = new();
    private readonly BehaviorSubject<DateTime> _lastSavedSubject = new(DateTime.MinValue);

    public int MaxTimetableCount => MAX_TIMETABLE_COUNT_LIMIT;
    public int CurrentTimetableCount => _data.Count;
    public DateTime LastSaved { get; private set; }

    public IObservable<IChangeSet<ScheduleTable>> TimetableChanges => _data
        .Connect()
        .Publish()
        .RefCount();
    
    public IObservable<int> TimetableCountChanges => _data.CountChanged
        .StartWith(_data.Count)
        .Replay(1)
        .RefCount();

    public ScheduleService(
        IUserDataService userDataService,
        ICTUWebDriverService CTUWebDriverService,
        ILogger<ScheduleService> logger)
    {
        _userDataService = userDataService;
        _courseManager = new CourseManager();
        _CTUWebDriverService = CTUWebDriverService;
        _logger = logger;
        
    }
    
    public void AddTimetable(ScheduleTableBuildData buildData)
    {
        if (!buildData.TryTrim(out var trimmedBuildData)) return;

        var courses = trimmedBuildData.Courses;
        var scheduleTable = trimmedBuildData.ScheduleTable;
        
        var isExisted = _data.Items
            .Any(x => scheduleTable.SavedCourseGroupKeys.DictionaryEquals(x.SavedCourseGroupKeys));
        if (isExisted) return;
        
        _courseManager.RegisterTimetable(courses, scheduleTable);
        _data.Add(scheduleTable);
        
        _logger.LogInformation("Added timetable");
    }

    public void AddRangeTimetable(IEnumerable<ScheduleTableBuildData> data)
    {
        foreach (var scheduleTableData in data)
        {
            AddTimetable(scheduleTableData);
        }
        _logger.LogInformation("Added timetables");
    }

    public void RemoveTimetable(ScheduleTable timetable)
    {
        _data.Remove(timetable);
        _courseManager.UnregisterTimetable(timetable);
        _logger.LogInformation("Removed timetable");
    }

    public async Task<bool> TryReloadCourseDataAsync()
    {
        try
        {
            _isReloadingSubject.OnNext(true);
            
            await _CTUWebDriverService.GoToCourseCatalogPage();
            foreach (var (code, groups) in _courseManager.GetAllCourseSectionGroupKeys())
            {
                var responseTask = _CTUWebDriverService.CourseCatalogResponse
                    .WhereNotNull()
                    .FirstOrDefaultAsync()
                    .Timeout(TimeSpan.FromSeconds(30)) 
                    .ToTask();
                
                await _CTUWebDriverService.SearchCourse(code);
                var course = await responseTask;

                if (course is null)
                {
                    _logger.LogError($"No response received for course code: {code}");
                    continue;
                }

                var groupList = groups.ToList();
                var newCourseSectionGroups = course.Sections.Select(s => s.Group).ToList();
                
                var unableReloadSections = groupList.Except(newCourseSectionGroups);
                foreach (var sectionGroup in unableReloadSections)
                {
                    var unActiveSection = GetCourseSection(code, sectionGroup);
                    if (unActiveSection is null) continue;
                    unActiveSection.RemainingStudents = 0;
                    unActiveSection.TotalStudents = 0;
                }
                
                var ableReloadSections = groupList.Intersect(newCourseSectionGroups).ToHashSet();
                course.Sections = course.Sections.Where(x => ableReloadSections.Contains(x.Group)).ToList();
                
                _courseManager.UpdateCourse(course);
                await Task.Delay(800);
            }
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to reload course data");
            return false;
        }
        finally
        {
            _isReloadingSubject.OnNext(false);
        }
    }

    public Course? GetCourse(string code)
    {
        return _courseManager.GetCourse(code);
    }

    public CourseSection? GetCourseSection(string code, string group)
    {
       return  _courseManager.GetCourseSection(code, group);
    }

    public SectionChoice? GetSectionChoice(string code, string group)
    {
       return _courseManager.GetSectionChoice(code, group);
    }

    public async Task<bool> TrySaveScheduleAsync(string filePath)
    {
        var currentSavedTime = LastSaved;
        
        var courses = _courseManager.GetAllCourses();
        var tables = _data.Items.ToList();
        TrimCoursesAndTables(courses,tables);
        BindScheduleSave(courses,tables);
        var isSaved =  await _userDataService.TrySaveUserDataAsync(filePath);
        if (!isSaved)
        {
            _logger.LogError("Failed to save schedule");
            LastSaved = currentSavedTime;
            return false;
        }
        _logger.LogInformation("Saved schedule");
        return true;
    }

    public async Task<bool> TryLoadScheduleAsync(string filePath)
    {
        try
        {
            bool isLoaded = await _userDataService.TryLoadUserDataAsync(filePath,JsonHelper.ScheduleLoadOptions);
            if (!isLoaded)
            {
                _logger.LogError("Failed to load schedule");
                return false;
            }

            var courses = _userDataService.ScheduleSaved.Courses;
            var tables = _userDataService.ScheduleSaved.ScheduleTables;
            if (!ValidateSavedTables(courses, tables))
            {
                _logger.LogError("Saved are damaged");
                return false;
            }

            TrimCoursesAndTables(courses, tables);

            _logger.LogInformation("Loaded schedule");
            BindScheduleManager(courses, tables);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load schedule");
            return false;
        }
    }

    private void BindScheduleSave(List<Course> courses, List<ScheduleTable> tables)
    {
        _userDataService.ScheduleSaved.Courses = courses;
        _userDataService.ScheduleSaved.ScheduleTables = tables;
        _userDataService.ScheduleSaved.LastSaved = DateTime.Now;
    }

    private void BindScheduleManager(List<Course> courses, List<ScheduleTable> tables)
    {
        ClearTimetables();
        _courseManager.RegisterTimetables(courses, tables);
        _data.AddRange(tables);
        LastSaved = _userDataService.ScheduleSaved.LastSaved;
    }
    
    private void ClearTimetables()
    {
        _courseManager.ClearAll();
        _data.Clear();
    }

    private bool ValidateSavedTables(List<Course> courses, List<ScheduleTable> tables)
    {
        if (courses == null || tables == null)
            return false;
        
        // validate unique (course,section)
        var courseSet = new HashSet<(string, string)>();
        foreach (var course in courses)
        {
            foreach (var section in course.Sections)
            {
                if (!courseSet.Add((course.Code, section.Group)))
                    return false;
            }
        }
        
        // validate limit of timetable
        if (tables.Count > MAX_TIMETABLE_COUNT_LIMIT)
            return false;

        var timetableCourseKeys = tables
            .SelectMany(table => table.SavedCourseGroupKeys)
            .Select(pkv => (pkv.Key, pkv.Value))
            .ToHashSet();
        
        return timetableCourseKeys.IsSubsetOf(courseSet);
    }

    /// <summary>
    ///  Trim courses and tables
    /// </summary>
    /// <param name="courses"></param>
    /// <param name="tables"></param>
    /// <returns>courses and tables have trimmed, fit with tables</returns>
    private void TrimCoursesAndTables(List<Course> courses, List<ScheduleTable> tables)
    {
        if (courses == null)
            throw new ArgumentNullException(nameof(courses));
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (tables == null)
            return;

        // trim table with none course
        tables.RemoveAll(table => table.SavedCourseGroupKeys.Count == 0);
        if (tables.Count == 0)
            courses.Clear();
        
        var timetableCourseKeys = tables
            .SelectMany(t => t.SavedCourseGroupKeys)
            .GroupBy(kvp => kvp.Key, kvp => kvp.Value)
            .ToDictionary(g => g.Key, g => g.ToHashSet());
        
        var courseDict = courses.ToDictionary(c => c.Code, c => c);

        foreach (var (code, groups) in timetableCourseKeys)
        {
            if (!courseDict.TryGetValue(code, out var existedCourse))
                continue;

            existedCourse.Sections = existedCourse.Sections
                .Where(s => groups.Contains(s.Group))
                .ToList();
            
            if (existedCourse.Sections.Count == 0)
                courses.Remove(existedCourse);
        }
    }
    
    public void Dispose()
    {
        _data.Dispose();
        _disposables.Dispose();
    }
}