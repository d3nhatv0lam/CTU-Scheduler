using System;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Steps;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Views.Steps;

public partial class SelectionView : ReactiveUserControl<SelectionViewModel>
{
    public SelectionView()
    {
        InitializeComponent();
    }
}