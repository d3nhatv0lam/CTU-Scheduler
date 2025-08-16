using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Features.TimeTableManager.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimeTableManager.Views;

public partial class TimeTableManagerView : ReactiveUserControl<TimeTableManagerViewModel>
{
    public TimeTableManagerView()
    {
        InitializeComponent();

        this.WhenActivated(disposables => { });
    }
}