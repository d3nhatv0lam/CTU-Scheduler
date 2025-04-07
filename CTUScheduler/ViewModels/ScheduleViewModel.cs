using Avalonia.Controls;
using CTUScheduler.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.ViewModels
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
                ThuDihoc = 2,
                TietBatDau = 1,
                RowSpan = 2
            });

            ScheduleTable.ScheduleCells.Add(new ScheduleCell
            {
                ThuDihoc = 5,
                TietBatDau = 6,
                RowSpan = 4
            });
            ScheduleTable.ScheduleCells.Add(new ScheduleCell
            {
                ThuDihoc = 6,
                TietBatDau = 1,
                RowSpan = 5
            });
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
