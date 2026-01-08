using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using ReactiveUI.Avalonia;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.Views;

public partial class TimetableView : ReactiveUserControl<TimetableViewModel>
{
    public TimetableView()
    {
        InitializeComponent();
    }
}