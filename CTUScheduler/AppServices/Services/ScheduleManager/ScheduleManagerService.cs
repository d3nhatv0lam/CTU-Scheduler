using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.User;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using DynamicData;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

public class ScheduleManagerService: IScheduleManagerService, IDisposable
{
    protected const int MAX_TIMETABLE_COUNT_LIMIT = 10;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly IUserDataService _userDataService;
    private readonly ILogger<ScheduleManagerService> _logger;
    private readonly SourceCache<Course, string> _courseData = new (course => course.Code);
    private readonly SourceList<ScheduleTable> _data = new();

    public int MaxTimetableCount => MAX_TIMETABLE_COUNT_LIMIT;
    public int CurrentTimetableCount => _data.Count;
   
    public DateTime LastSaved { get; private set; }

    public IObservable<IChangeSet<ScheduleTable>> TimetableChanges => _data
        .Connect()
        .Publish()
        .RefCount();
    
    public IObservable<int> TimetableCountChanges => _data.CountChanged.StartWith(_data.Count).Replay(1).RefCount();

    public ScheduleManagerService(IUserDataService userDataService, ILogger<ScheduleManagerService> logger)
    {
        _userDataService = userDataService;
        _logger = logger;
    }
    
    public void AddTimetable(ScheduleTable timetable)
    {
        _data.Add(timetable);
        _logger.LogInformation("Added timetable");
    }

    public void AddRangeTimetable(IEnumerable<ScheduleTable> timetables)
    {
        _data.AddRange(timetables);
        _logger.LogInformation("Added timetables");
    }

    public void RemoveTimetable(ScheduleTable timetable)
    {
        _data.Remove(timetable);
        _logger.LogInformation("Removed timetable");
    }
    
    public void ClearTimetables()
    {
        _data.Clear();
    }

    public Task UpdateTimetablesDataAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<bool> TrySaveScheduleAsync()
    {
        var path = "";
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
        _userDataService.ScheduleSaved.ScheduleTables = _data.Items.ToList();
        _userDataService.ScheduleSaved.LastUpdated = LastSaved;
    }

    private void BindScheduleManager()
    {
        ClearTimetables();
        AddRangeTimetable(_userDataService.ScheduleSaved.ScheduleTables);
        LastSaved = _userDataService.ScheduleSaved.LastUpdated;
    }

    public void Dispose()
    {
        _data.Dispose();
        _disposables.Dispose();
    }
}