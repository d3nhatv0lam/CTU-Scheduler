using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable;
using ReactiveUI;
using System.Reactive.Disposables;

namespace CTUScheduler.Presentation.Views.CoursePage.AddScheduleTable;

public partial class AddScheduleTableDialogView : ReactiveUserControl<AddScheduleTableDialogViewModel>
{
    public AddScheduleTableDialogView()
    {
        InitializeComponent();
        this.WhenActivated(disposeables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Height, v => v.CardBorder.Height, height => height * 0.8).DisposeWith(disposeables);
            this.OneWayBind(ViewModel, vm => vm.Width, v => v.CardBorder.Width, Width => Width * 0.85).DisposeWith(disposeables);
        });
    }
}