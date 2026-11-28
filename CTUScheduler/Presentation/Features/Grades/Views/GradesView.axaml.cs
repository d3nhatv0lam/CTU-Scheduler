using CTUScheduler.Presentation.Features.Grades.ViewModels;
using ReactiveUI.Avalonia;

namespace CTUScheduler.Presentation.Features.Grades.Views;

public partial class GradesView : ReactiveUserControl<GradesViewModel>
{
    public GradesView()
    {
        InitializeComponent();
    }
}
