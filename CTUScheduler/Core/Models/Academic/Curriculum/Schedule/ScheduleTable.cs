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
        public static readonly string DEFAULT_NAME = "UNNAMED";
        private string _name = DEFAULT_NAME;
        private string _description = string.Empty;
        private readonly ObservableCollection<ScheduleCell> _scheduleCells = new ObservableCollection<ScheduleCell>();
        private DateTime _lastUpdated = DateTime.Now;
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
        public int TotalCredit
        {
            get => _totalCredit;
            set => this.RaiseAndSetIfChanged(ref _totalCredit, value);
        }
        public Dictionary<string,string> ScheduleData { get; set; } = new Dictionary<string, string>();

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => this.RaiseAndSetIfChanged(ref _lastUpdated, value);
        }
        
        [JsonIgnore]
        public ObservableCollection<ScheduleCell> ScheduleCells => _scheduleCells;

        public ScheduleTable()
        {

        }

        // private bool IsDuplicateModule(ScheduleCell cell)
        // {
        //     throw new DuplicateModuleException();
        //     return true;
        // }
        //
        // private bool IsMaxCreditReached(ScheduleCell cell)
        // {
        //     bool isReachMax = false;
        //     if (cell.Credit + TotalCredit > 30)
        //         isReachMax = true;
        //     return isReachMax;
        // }
        // private bool IsConflictModule(ScheduleCell cell)
        // {
        //     bool isConflict = false;
        //     throw new ConflictModuleException();
        //     return isConflict;
        // }
        //
        // public bool CanAddCell(ScheduleCell cell)
        // {
        //     try
        //     {
        //         if (IsMaxCreditReached(cell) && IsDuplicateModule(cell) && IsConflictModule(cell))
        //             return true;
        //     }
        //     catch
        //     {
        //         throw;
        //     }
        //     return false;
        // }
        //
        // public void TryAddCell(ScheduleCell cell)
        // {
        //     try
        //     {
        //         if (CanAddCell(cell))
        //             _scheduleCells.Add(cell);
        //     }
        //     catch
        //     {
        //         throw;
        //     }
        // }
    }
}
