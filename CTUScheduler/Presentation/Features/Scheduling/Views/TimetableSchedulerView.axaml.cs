using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels;

namespace CTUScheduler.Presentation.Features.Scheduling.Views;

public partial class TimetableSchedulerView : ReactiveUserControl<TimetableSchedulerViewModel>
{
    public TimetableSchedulerView()
    {
        InitializeComponent();
    }
}