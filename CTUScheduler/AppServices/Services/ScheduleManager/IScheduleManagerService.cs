using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

/// <summary>
/// Coordinator between TimetableChanges in runtime and UserService's data
/// </summary>
public interface IScheduleManagerService
{
    public int MaxTimetableCount { get; }
    public int CurrentTimetableCount { get; }
    public DateTime LastSaved { get; }
    public IObservable<IChangeSet<ScheduleTable>> TimetableChanges { get; }
    public IObservable<int> TimetableCountChanges { get; }
    

    public void ClearTimetables();
    public void AddTimetable(ScheduleTable timetable);
    public void AddRangeTimetable(IEnumerable<ScheduleTable> timetables);
    public void RemoveTimetable(ScheduleTable timetable);
    /// <summary>
    /// Update data of all TimetableChanges in runtime
    /// </summary>
    /// <returns></returns>
    public Task UpdateTimetablesDataAsync();
    /// <summary>
    ///  Save TimetableChanges from runtime to .json file
    /// </summary>
    /// <returns></returns>
    public Task<bool> TrySaveScheduleAsync();
    /// <summary>
    /// Load TimetableChanges from save to TimetableLayouts
    /// </summary>
    /// <returns></returns>
    public Task<bool> TryLoadScheduleAsync();
}