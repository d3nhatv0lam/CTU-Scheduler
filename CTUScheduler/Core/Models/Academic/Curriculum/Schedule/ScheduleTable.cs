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
        public static readonly string DEFAULT_NAME = "Unnamed";
        public string Name { get; set; } = DEFAULT_NAME;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string,string> ScheduleData { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        public ScheduleTable()
        {
        }
    }
}
