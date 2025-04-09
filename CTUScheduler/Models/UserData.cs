using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CTUScheduler.Models;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;

namespace CTUScheduler.Models
{
    public class UserData : ReactiveObject , IDisposable
    {
        private CompositeDisposable _disposables = new CompositeDisposable();
        public ObservableCollection<Course> Courses { get; set; } = new ObservableCollection<Course>();
        public SchedulesManager SchedulesManager { get; set; } = new SchedulesManager();
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        public UserData()
        {
            _disposables.Add(SchedulesManager);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
