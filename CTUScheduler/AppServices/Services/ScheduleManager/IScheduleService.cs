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
    public IObservable<DateTime?> LastSaveChanged { get; }
    public IObservable<IChangeSet<ScheduleProfile>> TimetableChanges { get; }
    public IObservable<int> TimetableCountChanges { get; }
    
    public IObservable<bool> IsExpiredSaved { get; }
    
    
    /// <summary>
    /// Add schedule table to manager, do nothing if existed or invalid data
    /// </summary>
    /// <param name="buildData"></param>
    public void AddTimetable(ScheduleBlueprint buildData);
    public void AddRangeTimetable(IEnumerable<ScheduleBlueprint> data);
    public void RemoveTimetable(ScheduleProfile scheduleProfile);

    /// <summary>
    ///  Save TimetableChanges from runtime to .json file
    /// </summary>
    /// <returns></returns>
    public Task<bool> TrySaveScheduleAsync(string filePath);
    /// <summary>
    /// Load TimetableChanges from save to TimetableLayouts
    /// </summary>
    /// <returns></returns>
    public Task<bool> TryLoadScheduleAsync(string filePath);
}