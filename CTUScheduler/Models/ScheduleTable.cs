using CTUScheduler.Exceptions;
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
        private readonly ObservableCollection<ScheduleCell> _scheduleCells;
        private int _totalCredit; 

        public ObservableCollection<ScheduleCell> ScheduleCells
        {
            get => _scheduleCells;
        }
        public int TotalCredit
        {
            get => _totalCredit;
            set => this.RaiseAndSetIfChanged(ref _totalCredit, value);
        }


        public ScheduleTable()
        {
            _scheduleCells = new ObservableCollection<ScheduleCell>();
        }

        private bool IsDuplicateModule(ScheduleCell cell)
        {

            throw new DuplicateModuleException();
            return true;
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
                if (IsDuplicateModule(cell) && IsConflictModule(cell))
                    return true;
            }
            catch
            {
                throw;
            }
            return false;
        }
        
        public void AddCell(ScheduleCell cell)
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
