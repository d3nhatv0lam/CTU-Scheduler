using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.ViewModels.Base;
using CTUScheduler.Presentation.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels
{
    public class ScheduleViewModel : ViewModelBase, IDisposable, IRoutableViewModel
    {
        public ScheduleTable ScheduleTable { get; set; }

        public string? UrlPathSegment => "Schedule View";

        public IScreen HostScreen { get; }

        public ScheduleViewModel(IScreen hostScreen)
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
        public ScheduleViewModel(IScreen hostScreen, ScheduleTable scheduleTable)
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
