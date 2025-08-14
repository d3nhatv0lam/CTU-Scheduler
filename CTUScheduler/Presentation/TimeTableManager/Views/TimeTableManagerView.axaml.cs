using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.TimeTableManager.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.TimeTableManager.Views;

public partial class TimeTableManagerView : ReactiveUserControl<TimeTableManagerViewModel>
{
    public TimeTableManagerView()
    {
        InitializeComponent();

        this.WhenActivated(disposeables => { });
    }
}