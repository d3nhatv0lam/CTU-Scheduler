using System;
using System.Collections.Generic;

namespace CTUScheduler.Core.Models.Academic.Curriculum.Schedule
{
    public class ScheduleProfile
    {
        private static readonly string DEFAULT_NAME = "Unnamed";
        public string Name { get; set; } = DEFAULT_NAME;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string,string> SavedCourseGroupKeys { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
