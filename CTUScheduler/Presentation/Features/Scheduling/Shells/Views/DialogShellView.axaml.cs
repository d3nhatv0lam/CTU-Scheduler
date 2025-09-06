using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Features.Scheduling.Shells.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Shells.Views;

public partial class DialogShellView : ReactiveUserControl<DialogShellViewModel>
{

    private readonly double _heightScale = 0.8;
    private readonly double _widthScale = 0.85;
    public DialogShellView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Height, v => v.CardBorder.Height, height => height * _heightScale).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.Width, v => v.CardBorder.Width, width => width * _widthScale).DisposeWith(disposables);
        });
    }
}