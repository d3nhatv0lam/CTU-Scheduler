using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CTUScheduler.Models;
using CTUScheduler.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
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
