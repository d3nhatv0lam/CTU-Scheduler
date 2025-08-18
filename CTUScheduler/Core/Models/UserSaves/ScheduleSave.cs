using System;
using System.Collections.ObjectModel;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.Core.Models.UserSaves
{
    public class ScheduleSave
    {
        public ObservableCollection<Course> Courses { get; set; } = new ();
        public ObservableCollection<ScheduleTable> ScheduleTables { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
