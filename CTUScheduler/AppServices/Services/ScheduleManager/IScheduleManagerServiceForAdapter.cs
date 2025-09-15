using System;
using System.Reactive;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

public interface IScheduleManagerServiceForAdapter
{
    IObservable<Unit> UpdateScheduleTableRequest { get; }
}