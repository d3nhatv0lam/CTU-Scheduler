using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Helpers.Json;
using CTUScheduler.AppServices.Services.RegistrationInfor;
using CTUScheduler.AppServices.Services.User;
using CTUScheduler.AppServices.Services.WebDriver;
using CTUScheduler.AppServices.Validators;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Helpers;
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
    private readonly IRegistrationInformationService _registrationInformationService;
  
    
    private readonly SourceList<ScheduleProfile> _data = new();
    private readonly Subject<bool> _isReloadingSubject = new();
    private readonly BehaviorSubject<bool> _isExpiredSavedSubject = new(false);
    private readonly BehaviorSubject<DateTime?> _lastSavedSubject = new(null);

    private DateTime _lastSaved = DateTime.Now;
    public int MaxTimetableCount => MAX_TIMETABLE_COUNT_LIMIT;
    public int CurrentTimetableCount => _data.Count;
    public IObservable<DateTime?> LastSaveChanged => _lastSavedSubject;

    private DateTime LastSaved
    {
        get => _lastSaved;
        set 
        {
            _lastSaved = value;
            _lastSavedSubject.OnNext(_lastSaved);
        } 
    }

    public IObservable<IChangeSet<ScheduleProfile>> TimetableChanges => _data
        .Connect()
        .Publish()
        .RefCount();
    
    public IObservable<int> TimetableCountChanges => _data.CountChanged
        .StartWith(_data.Count)
        .Replay(1)
        .RefCount();
    
    public IObservable<bool> IsExpiredSaved => _isExpiredSavedSubject;

    public ScheduleService(
        IUserDataService userDataService,
        ICTUWebDriverService ctuWebDriverService,
        IRegistrationInformationService registrationInformationService,
        ILogger<ScheduleService> logger)
    {
        _userDataService = userDataService;
        _courseManager = new CourseManager();
        _CTUWebDriverService = ctuWebDriverService;
        _registrationInformationService = registrationInformationService;
        _logger = logger;
    }
    
    public void AddTimetable(ScheduleBlueprint buildData)
    {
        if (!buildData.TryTrim(out var trimmedBuildData)) return;

        var courses = trimmedBuildData.Courses;
        var scheduleTable = trimmedBuildData.Metadata;

        if (ScheduleValidator.IsExistedTimetable(scheduleTable, _data.Items))
            return;
        
        _courseManager.RegisterTimetable(courses, scheduleTable);
        _data.Add(scheduleTable);
        
        _logger.LogInformation("Added timetable");
    }

    public void AddRangeTimetable(IEnumerable<ScheduleBlueprint> data)
    {
        foreach (var scheduleTableData in data)
        {
            AddTimetable(scheduleTableData);
        }
        _logger.LogInformation("Added timetables");
    }

    public void RemoveTimetable(ScheduleProfile scheduleProfile)
    {
        _data.Remove(scheduleProfile);
        _courseManager.UnregisterTimetable(scheduleProfile);
        if (_data.Count == 0)
            _isExpiredSavedSubject.OnNext(false);
        _logger.LogInformation("Removed scheduleProfile");
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
        if (!_registrationInformationService.HasRegistrationInformation())
        {
            _logger.LogError("No registration information");
            return false;
        }

        var savedTime = DateTime.Now;
        var registerInformation = _registrationInformationService.GetSemester();
        var courses = _courseManager.GetAllCourses();
        var tables = _data.Items.ToList();
        ScheduleOptimizer.Trim(courses,tables);
        BindScheduleSave(registerInformation,courses,tables,savedTime);
        var isSaved =  await _userDataService.TrySaveUserDataAsync(filePath);
        if (!isSaved)
        {
            _logger.LogError("Failed to save schedule");
            return false;
        }
        _lastSavedSubject.OnNext(savedTime);
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
            
            var savedSemester = _userDataService.ScheduleSaved.Semester;
            var savedAcademicYear = _userDataService.ScheduleSaved.AcademicYear;
            
            _isExpiredSavedSubject.OnNext(!_registrationInformationService.IsEqualSemester(savedSemester, savedAcademicYear));

            ScheduleOptimizer.Trim(courses,tables);

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

    private void BindScheduleSave((int academicYear,string semester) registerInformation,List<Course> courses, List<ScheduleProfile> tables, DateTime savedTime)
    {
        _userDataService.ScheduleSaved.Semester = registerInformation.semester;
        _userDataService.ScheduleSaved.AcademicYear = registerInformation.academicYear;
        _userDataService.ScheduleSaved.Courses = courses;
        _userDataService.ScheduleSaved.ScheduleTables = tables;
        _userDataService.ScheduleSaved.LastSaved = savedTime;
        
    }

    private void BindScheduleManager(List<Course> courses, List<ScheduleProfile> tables)
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

    private bool ValidateSavedTables(List<Course> courses, List<ScheduleProfile> tables)
    {
        if (courses == null || tables == null)
            return false;

        var courseList = courses.ToList();
        // validate unique (course,section)
        var courseSet = new HashSet<(string, string)>();
        foreach (var course in courseList)
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
        
        var courseSectionDictionary = courseList
            .SelectMany(course => course.Sections.Select(section => (course.Code,section.Group,section)))
            .ToDictionary(x => (x.Code,x.Group), x => x.section);
        
        // validate timetable
        foreach (var table in tables)
        {
            if (!ScheduleValidator.IsValidTimetable(table, courseSectionDictionary))
                return false;
        }
        return true;
    }

    /// <summary>
    ///  Trim courses and tables
    /// </summary>
    /// <param name="courses"></param>
    /// <param name="tables"></param>
    /// <returns>courses and tables have trimmed, fit with tables</returns>
    private void TrimCoursesAndTables(List<Course> courses, List<ScheduleProfile> tables)
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