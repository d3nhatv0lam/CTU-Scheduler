using Avalonia;
using Avalonia.Controls;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Shells;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace CTUScheduler.Presentation.Features.Scheduling.Views.Shells;

public partial class SchedulingDialogView : ReactiveUserControl<SchedulingDialogViewModel>
{
    public SchedulingDialogView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            var top = TopLevel.GetTopLevel(this);
            if (top is null || ViewModel is null) return;

            var isSelectionModeObservable = ViewModel.WhenAnyValue(x => x.IsSelectionMode);
            var clientSizeObservable = top.GetObservable(BoundsProperty);

            isSelectionModeObservable.CombineLatest(clientSizeObservable, (isSelection, bounds) => (isSelection, bounds.Size))
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(tuple =>
                {
                    if (tuple.isSelection)
                    {
                        CardBorder.Width = 760;
                        CardBorder.Height = 480;
                    }
                    else
                    {
                        CardBorder.Width = Math.Max(850, tuple.Size.Width * 0.95 - 60);
                        CardBorder.Height = Math.Max(520, tuple.Size.Height * 0.95 - 60);
                    }
                })
                .DisposeWith(disposables);
        });
    }
}