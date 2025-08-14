using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable;
using ReactiveUI;
using System.Reactive.Disposables;

namespace CTUScheduler.Presentation.Views.CoursePage.AddScheduleTable;

public partial class AddScheduleTableDialogView : ReactiveUserControl<DialogViewModel>
{

    private readonly double _heightScale = 0.8;
    private readonly double _widthScale = 0.85;
    public AddScheduleTableDialogView()
    {
        InitializeComponent();
        this.WhenActivated(disposeables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Height, v => v.CardBorder.Height, height => height * _heightScale).DisposeWith(disposeables);
            this.OneWayBind(ViewModel, vm => vm.Width, v => v.CardBorder.Width, width => width * _widthScale).DisposeWith(disposeables);
        });
    }
}