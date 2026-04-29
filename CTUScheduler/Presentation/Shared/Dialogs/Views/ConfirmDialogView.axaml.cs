using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Shared.Dialogs.ViewModels;

namespace CTUScheduler.Presentation.Shared.Dialogs.Views
{
    public partial class ConfirmDialogView : ReactiveUserControl<ConfirmDialogViewModel>
    {
        public ConfirmDialogView()
        {
            InitializeComponent();
        }
    }
}