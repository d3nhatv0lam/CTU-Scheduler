using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using DynamicData;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

/// <summary>
/// Coordinator between TimetableChanges in runtime and UserService's data
/// </summary>
public interface IScheduleService
{
    public int MaxTimetableCount { get; }
    public int CurrentTimetableCount { get; }
    public DateTime LastSaved { get; }
    public IObservable<IChangeSet<ScheduleTable>> TimetableChanges { get; }
    public IObservable<int> TimetableCountChanges { get; }
    
    
    public void AddTimetable(ScheduleTableData data);
    public void AddRangeTimetable(IEnumerable<ScheduleTableData> data);
    public void RemoveTimetable(ScheduleTable timetable);

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