using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Models
{
    public class ScheduleTable: ReactiveObject
    {
        private ObservableCollection<ScheduleCell> _scheduleCells;

        public ObservableCollection<ScheduleCell> ScheduleCells
        {
            get => _scheduleCells;
            set => this.RaiseAndSetIfChanged(ref _scheduleCells, value);
        }

        public ScheduleTable()
        {
            _scheduleCells = new ObservableCollection<ScheduleCell>();
        }

    }
}
