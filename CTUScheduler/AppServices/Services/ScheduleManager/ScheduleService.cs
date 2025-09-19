using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
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
    private readonly ConcurrentDictionary<ScheduleTable, IDisposable> _timetableRegistrations = new();
    private readonly Subject<bool> _isReloadingSubject = new();

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
        if (!buildData.IsValid()) return;
        
        var courseList = buildData.Courses.ToList();
        var scheduleTable = buildData.ScheduleTable;
        
        var isExisted = _data.Items
            .Any(x => scheduleTable.SavedCourseGroupKeys.DictionaryEquals(x.SavedCourseGroupKeys));
        if (isExisted) return;
        
        var timetableRegistration = _courseManager.RegisterTimetable(courseList);
        _timetableRegistrations.TryAdd(scheduleTable, timetableRegistration);

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
        _timetableRegistrations.TryRemove(timetable, out var timetableRegistration);
        timetableRegistration?.Dispose();
        _logger.LogInformation("Removed timetable");
    }

    public async Task ReloadCourseDataAsync()
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
                course.Sections = course.Sections.Where(x => groups.Contains(x.Group)).ToList();
                
                _courseManager.UpdateCourse(course);
                await Task.Delay(800);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to reload course data");
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

    public async Task<bool> TrySaveScheduleAsync()
    {
        var path = "D:/Schedule.json";
        LastSaved = DateTime.Now;
        BindScheduleSave();
        var isSaved =  await _userDataService.TrySaveUserDataAsync(path);
        if (!isSaved)
        {
            _logger.LogError("Failed to save schedule");
            return false;
        }
        _logger.LogInformation("Saved schedule");
        return true;
    }

    public async Task<bool> TryLoadScheduleAsync()
    {
        var path = "";
        bool isLoaded =  await _userDataService.TryLoadUserDataAsync(path);
        if (!isLoaded || !ValidateScheduleTables())
        {
            _logger.LogError("Failed to load schedule");
            return false;
        }
        _logger.LogInformation("Loaded schedule");
        BindScheduleManager();
        return true;
    }

    private void BindScheduleSave()
    {
        _userDataService.ScheduleSaved.Courses = _courseManager.GetAllCourses();
        _userDataService.ScheduleSaved.ScheduleTables = _data.Items.ToList();
        _userDataService.ScheduleSaved.LastSaved = LastSaved;
    }

    private void BindScheduleManager()
    {
        ClearTimetables();
        _data.AddRange(_userDataService.ScheduleSaved.ScheduleTables);
        LastSaved = _userDataService.ScheduleSaved.LastSaved;
    }

    private bool ValidateScheduleTables()
    {
        _courseManager.UpdateCourses(_userDataService.ScheduleSaved.Courses);
        var tables = _userDataService.ScheduleSaved.ScheduleTables;
        foreach (var table in tables)
        {
            foreach (var (code,group) in table.SavedCourseGroupKeys)
            {
                if (_courseManager.GetCourseSection(code, group) is null)
                {
                    OnValidateScheduleTablesFail();
                    return false;
                }
            }
        }
        return true;
    }

    protected virtual void OnValidateScheduleTablesFail()
    {
        _userDataService.ClearScheduleSaved();
        _courseManager.ClearAll();
    }
    
    private void ClearTimetables()
    {
        _courseManager.ClearAll();
        _timetableRegistrations.Clear();
        _data.Clear();
    }

    public void Dispose()
    {
        _data.Dispose();
        _disposables.Dispose();
    }
}