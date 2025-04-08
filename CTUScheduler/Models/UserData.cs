using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CTUScheduler.Models;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace CTUScheduler.Models
{
    public class UserData : ReactiveObject
    {
        public ObservableCollection<Course> Courses { get; set; }
        public SchedulesManager SchedulesManager { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
