using System;
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

public class ScheduleManagerService: IScheduleManagerService, IDisposable
{
    protected const int MAX_TIMETABLE_COUNT_LIMIT = 10;
    private readonly CompositeDisposable _disposables = new ();
    private readonly ILogger<ScheduleManagerService> _logger;
    private readonly IUserDataService _userDataService;
    private readonly ICourseManagerService _courseManagerService;
    private readonly ICTUWebDriverService _CTUWebDriverService;
    private readonly SourceList<ScheduleTable> _data = new();
    private readonly Subject<bool> _isReloadingSubject = new ();

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

    public ScheduleManagerService(
        IUserDataService userDataService,
        ICTUWebDriverService CTUWebDriverService,
        ILogger<ScheduleManagerService> logger)
    {
        _userDataService = userDataService;
        _courseManagerService = new CourseManagerService();
        _CTUWebDriverService = CTUWebDriverService;
        _logger = logger;
    }
    
    private void ClearTimetables()
    {
        _courseManagerService.Clear();
        _data.Clear();
    }
    
    public void AddTimetable(ScheduleTableData data)
    {
        var courseList = data.courses.ToList();
        if (courseList.Count == 0 || data.scheduleTable is null) return;

        _courseManagerService.AddOrUpdateCourse(courseList);
        _data.Add(data.scheduleTable);
        _logger.LogInformation("Added timetable");
    }

    public void AddRangeTimetable(IEnumerable<ScheduleTableData> data)
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
        _logger.LogInformation("Removed timetable");
    }

    public async Task ReloadCourseDataAsync()
    {
        try
        {
            _isReloadingSubject.OnNext(true);
            await _CTUWebDriverService.GoToCourseCatalogPage();
            foreach (var (code, groups) in _courseManagerService.GetCourseGroups())
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
                };
                course.Sections = course.Sections.Where(x => groups.Contains(x.Group)).ToList();
                
                _courseManagerService.AddOrUpdateCourse(course);
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

    public CourseSection? GetCourseSection(string code, string group)
    {
       return  _courseManagerService.GetSection(code, group);
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
        if (!isLoaded)
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
        _userDataService.ScheduleSaved.Courses = _courseManagerService.GetCourses();
        _userDataService.ScheduleSaved.ScheduleTables = _data.Items.ToList();
        _userDataService.ScheduleSaved.LastSaved = LastSaved;
    }

    private void BindScheduleManager()
    {
        ClearTimetables();
        _courseManagerService.AddOrUpdateCourse(_userDataService.ScheduleSaved.Courses);
        _data.AddRange(_userDataService.ScheduleSaved.ScheduleTables);
        LastSaved = _userDataService.ScheduleSaved.LastSaved;
    }

    public void Dispose()
    {
        _data.Dispose();
        _disposables.Dispose();
    }
}