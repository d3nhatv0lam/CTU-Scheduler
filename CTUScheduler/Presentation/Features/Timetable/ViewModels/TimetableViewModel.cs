using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Timetable.Models;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Timetable.ViewModels
{
    public class TimetableViewModel : ViewModelBase
    {
        public ObservableCollection<ScheduleCellUi> ScheduleCells { get; set; } = new();
        public TimetableViewModel() {}
    }
}
