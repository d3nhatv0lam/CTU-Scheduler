using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.User;
using CTUScheduler.Presentation.Features.Timetable.Models;
using CTUScheduler.Presentation.Shared.Models;
using DynamicData;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

public class ScheduleManagerService: IScheduleManagerService, IDisposable
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly IUserDataService _userDataService;
    private readonly ILogger<ScheduleManagerService> _logger;
    private readonly SourceList<SelectableTimetableLayout> _data = new();
    public DateTime LastSaved { get; set; }
    public IObservable<IChangeSet<SelectableTimetableLayout>> TimetableLayouts => _data.Connect();
    public IObservable<int> TimetableCountChanged => _data.CountChanged;

    public ScheduleManagerService(IUserDataService userDataService, ILogger<ScheduleManagerService> logger)
    {
        _userDataService = userDataService;
        _logger = logger;
    }
    
    public void ClearTimetables()
    {
        _data.Clear();
    }

    public void AddTimetable(SelectableTimetableLayout timetable)
    {
        _data.Add(timetable);
    }

    public void AddRangeTimetable(IEnumerable<SelectableTimetableLayout> timetables)
    {
        _data.AddRange(timetables);
    }

    public void RemoveTimetable(SelectableTimetableLayout timetable)
    {
        _data.Remove(timetable);
        _logger.LogInformation("Removed timetable");
    }

    public Task UpdateTimetablesDataAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<bool> TrySaveScheduleAsync(string path)
    {
       bool isSaved =  await _userDataService.TrySaveUserDataAsync(path);
       if (!isSaved)
       {
           _logger.LogError("Failed to save schedule");
           return false;
       }
       _logger.LogInformation("Saved schedule");
       
       return true;
    }

    public async Task<bool> TryLoadScheduleAsync(string path)
    {
        bool isLoaded =  await _userDataService.TryLoadUserDataAsync(path);
        if (!isLoaded)
        {
            _logger.LogError("Failed to load schedule");
            return false;
        }
        _logger.LogInformation("Loaded schedule");
        
        LastSaved = _userDataService.ScheduleSaved.LastUpdated;
        
        
        
        return true;
    }

    public void Dispose()
    {
        _data.Dispose();
    }
}