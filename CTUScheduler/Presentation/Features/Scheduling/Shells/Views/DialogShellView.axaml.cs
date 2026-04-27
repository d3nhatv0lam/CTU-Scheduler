using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.Scheduling.Shells.ViewModels;

namespace CTUScheduler.Presentation.Features.Scheduling.Shells.Views;

public partial class DialogShellView : ReactiveUserControl<DialogShellViewModel>
{
    public DialogShellView()
    {
        InitializeComponent();
    }
}