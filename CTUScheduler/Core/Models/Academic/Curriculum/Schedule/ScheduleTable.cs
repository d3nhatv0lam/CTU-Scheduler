using CTUScheduler.Core.Exceptions;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Academic.Curriculum.Schedule
{
    public class ScheduleTable: ReactiveObject
    {
        public static string DEFAULT_NAME = "UNNAMED";
        private string _name;
        private string _description = string.Empty;
        private readonly ObservableCollection<ScheduleCell> _scheduleCells;
        private readonly Dictionary<string, string> _scheduleData;
        private DateTime _lastUpdate = DateTime.Now;
        private int _totalCredit;
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }
        public string Description
        {
            get => _description;
            set => this.RaiseAndSetIfChanged(ref _description, value);
        }
        [JsonIgnore]
        public ObservableCollection<ScheduleCell> ScheduleCells
        {
            get => _scheduleCells;
        }
        public int TotalCredit
        {
            get => _totalCredit;
            set => this.RaiseAndSetIfChanged(ref _totalCredit, value);
        }
        public Dictionary<string,string> ScheduleData
        {
            get => _scheduleData;
        }
        public DateTime LastUpdate
        {
            get => _lastUpdate;
            set => this.RaiseAndSetIfChanged(ref _lastUpdate, value);
        }

        public ScheduleTable()
        {
            _scheduleCells = new ObservableCollection<ScheduleCell>();
            _scheduleData = new Dictionary<string, string>();
        }

        private bool IsDuplicateModule(ScheduleCell cell)
        {
            throw new DuplicateModuleException();
            return true;
        }

        private bool IsMaxCreditReached(ScheduleCell cell)
        {
            bool isReachMax = false;
            if (cell.Credit + TotalCredit > 30)
                isReachMax = true;
            return isReachMax;
        }
        private bool IsConflictModule(ScheduleCell cell)
        {
            bool isConflict = false;
            throw new ConflictModuleException();
            return isConflict;
        }

        private bool CanAddCell(ScheduleCell cell)
        {
            try
            {
                if (IsMaxCreditReached(cell) && IsDuplicateModule(cell) && IsConflictModule(cell))
                    return true;
            }
            catch
            {
                throw;
            }
            return false;
        }
        
        public void TryAddCell(ScheduleCell cell)
        {
            try
            {
                if (CanAddCell(cell))
                    _scheduleCells.Add(cell);
            }
            catch
            {
                throw;
            }
        }
    }
}
