using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTUScheduler.Presentation.Shared.Models;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

/// <summary>
/// Coordinator between Timetables in runtime and UserService's data
/// </summary>
public interface IScheduleManagerService
{
    public DateTime LastSaved { get; }
    public IObservable<IChangeSet<SelectableTimetableLayout>> TimetableLayouts  { get; }
    public IObservable<int> TimetableCountChanged { get; }

    public void ClearTimetables();
    public void AddTimetable(SelectableTimetableLayout timetable);
    public void AddRangeTimetable(IEnumerable<SelectableTimetableLayout> timetables);
    public void RemoveTimetable(SelectableTimetableLayout timetable);
    /// <summary>
    /// Update data of all Timetables in runtime
    /// </summary>
    /// <returns></returns>
    public Task UpdateTimetablesDataAsync();
    /// <summary>
    ///  Save Timetables from runtime to .json file
    /// </summary>
    /// <returns></returns>
    public Task<bool> TrySaveScheduleAsync(string path);
    /// <summary>
    /// Load Timetables from save to TimetableLayouts
    /// </summary>
    /// <returns></returns>
    public Task<bool> TryLoadScheduleAsync(string path);
}