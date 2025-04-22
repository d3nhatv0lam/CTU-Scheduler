using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace CTUScheduler.Presentation.Views;

public partial class ScheduleView : ReactiveUserControl<ScheduleViewModel>
{
    public ScheduleView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.ScheduleTable.ScheduleCells, x => x.scheduleTable.ItemsSource).DisposeWith(disposables);
        });
    }
}