using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Base;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Timetable.ViewModels
{
    public class TimetableViewModel : ViewModelBase
    {
        public ObservableCollection<ScheduleCellViewModel> ScheduleCells { get; set; } = new();
        public TimetableViewModel() {}
    }
}
