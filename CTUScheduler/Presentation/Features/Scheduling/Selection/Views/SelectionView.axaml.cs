using System;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Features.Scheduling.Selection.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Selection.Views;

public partial class SelectionView : ReactiveUserControl<SelectionViewModel>
{
    public SelectionView()
    {
        InitializeComponent();
    }
}