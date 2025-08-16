using System;
using System.Diagnostics;
using System.Text.Json;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Base;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimeTable.ViewModels
{
    public class TimeTableViewModel : ViewModelBase, IDisposable, IRoutableViewModel
    {
        public ScheduleTable ScheduleTable { get; set; }

        public string? UrlPathSegment => "TimeTableViewModel";

        public IScreen HostScreen { get; }

        public TimeTableViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            ScheduleTable = new ScheduleTable();
            ScheduleTable.ScheduleCells.Add(new ScheduleCell
            {
                AttendingDay = 2,
                StartPeriod = 1,
                NumberOfPeriods = 2
            });

            ScheduleTable.ScheduleCells.Add(new ScheduleCell
            {
                AttendingDay = 5,
                StartPeriod = 6,
                NumberOfPeriods = 4
            });
            ScheduleTable.ScheduleCells.Add(new ScheduleCell
            {
                AttendingDay = 6,
                StartPeriod = 1,
                NumberOfPeriods = 5
            });
            string json = JsonSerializer.Serialize(ScheduleTable);
            Debug.WriteLine(json);
        }
        public TimeTableViewModel(IScreen hostScreen, ScheduleTable scheduleTable)
        {
            HostScreen = hostScreen;
            ScheduleTable = scheduleTable;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
