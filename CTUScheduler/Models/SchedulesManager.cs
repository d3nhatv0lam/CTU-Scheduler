using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CTUScheduler.Models
{
    public class SchedulesManager: ReactiveObject
    {
        private readonly ObservableCollection<ScheduleTable> _scheduleTables;

        public ObservableCollection<ScheduleTable> ScheduleTables
        {
            get => _scheduleTables;
        }

        [JsonIgnore]
        public int ScheduleTableCount => _scheduleTables.Count;

        public SchedulesManager()
        {
            _scheduleTables = new ObservableCollection<ScheduleTable>();
        }
        public SchedulesManager(ObservableCollection<ScheduleTable> scheduleTables)
        {
            _scheduleTables = scheduleTables;
        }
        public void AddScheduleTable(ScheduleTable scheduleTable)
        {
            _scheduleTables.Add(scheduleTable);
        }
    }
}
