using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.Views;

public partial class TimetableView : ReactiveUserControl<TimetableViewModel>
{
    public TimetableView()
    {
        InitializeComponent();
    }
}