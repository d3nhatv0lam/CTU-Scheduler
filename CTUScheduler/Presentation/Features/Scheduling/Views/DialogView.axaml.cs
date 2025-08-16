using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Views;

public partial class DialogView : ReactiveUserControl<DialogViewModel>
{

    private readonly double _heightScale = 0.8;
    private readonly double _widthScale = 0.85;
    public DialogView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Height, v => v.CardBorder.Height, height => height * _heightScale).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.Width, v => v.CardBorder.Width, width => width * _widthScale).DisposeWith(disposables);
        });
    }
}