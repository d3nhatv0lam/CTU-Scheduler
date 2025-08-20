using CTUScheduler.Core.Exceptions;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;

namespace CTUScheduler.Core.Models.Academic.Curriculum.Schedule
{
    public class ScheduleTable
    {
        public static readonly string DEFAULT_NAME = "UNNAMED";
        public string Name { get; set; } = DEFAULT_NAME;
        public string Description { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public int TotalCredit { get; set; }
        public Dictionary<string,string> ScheduleData { get; set; } = new();

        public ScheduleTable()
        {
        }
        
        public void Add(ScheduleCell scheduleCell)
        {
            ScheduleData.Add(scheduleCell.CourseCode, scheduleCell.Group);
        }

        public void Add(IEnumerable<ScheduleCell> scheduleCells)
        {
            foreach (var cell in scheduleCells)
                Add(cell);
        }

    }
}
