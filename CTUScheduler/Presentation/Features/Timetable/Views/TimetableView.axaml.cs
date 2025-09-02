using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Timetable.Views;

public partial class TimetableView : ReactiveUserControl<TimetableViewModel>
{
    public TimetableView()
    {
        InitializeComponent();
    }
}