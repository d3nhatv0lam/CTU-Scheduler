using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

/// <summary>
/// Coordinator between Timetables in runtime and UserService's data
/// </summary>
public interface IScheduleManagerService
{
    public DateTime LastSaved { get; }
    public IObservable<IChangeSet<ScheduleTable>> Timetables { get; }
    public IObservable<int> TimetableCountChanged { get; }

    public void ClearTimetables();
    public void AddTimetable(ScheduleTable timetable);
    public void AddRangeTimetable(IEnumerable<ScheduleTable> timetables);
    public void RemoveTimetable(ScheduleTable timetable);
    /// <summary>
    /// Update data of all Timetables in runtime
    /// </summary>
    /// <returns></returns>
    public Task UpdateTimetablesDataAsync();
    /// <summary>
    ///  Save Timetables from runtime to .json file
    /// </summary>
    /// <returns></returns>
    public Task<bool> TrySaveScheduleAsync();
    /// <summary>
    /// Load Timetables from save to TimetableLayouts
    /// </summary>
    /// <returns></returns>
    public Task<bool> TryLoadScheduleAsync();
}