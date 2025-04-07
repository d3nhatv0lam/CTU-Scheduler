using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace CTUScheduler.Views;

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