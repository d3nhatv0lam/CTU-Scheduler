using System;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Scheduling.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Scheduling.Views;

public partial class SelectionView : ReactiveUserControl<SelectionViewModel>
{
    public SelectionView()
    {
        InitializeComponent();

        ViewForMixins.WhenActivated((IActivatableView)this, (Action<IDisposable> disposeables) =>
        {
            this.BindCommand<SelectionView, SelectionViewModel, ReactiveCommand<Unit, Unit>, Button>(ViewModel, vm => vm.HandmadeSelectionCommand, v => v.btnHandmadeSelection);
        });
    }
}