using System;
using Avalonia.Controls;
using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Components;

namespace CTUScheduler.Presentation.Features.Scheduling.Views.Components;

public partial class AvoidanceFilterView : ReactiveUserControl<AvoidanceFilterViewModel>
{
    public AvoidanceFilterView()
    {
        InitializeComponent();
    }

    private void OnFlyoutOpened(object? sender, EventArgs e)
    {
        ViewModel?.NotifyFlyoutOpened();
    }

    private void OnFlyoutClosed(object? sender, EventArgs e)
    {
        ViewModel?.NotifyFlyoutClosed();
    }
}
