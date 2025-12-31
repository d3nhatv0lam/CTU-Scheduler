using System;
using System.Globalization;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Features.TimetableManager.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimetableManager.Views;

public partial class TimetableManagerView : ReactiveUserControl<TimetableManagerViewModel>
{
    public TimetableManagerView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.OneWayBind(ViewModel,
                vm => vm.LastSaved,
                v => v.txtLastSaved.Text)
                .DisposeWith(disposable);
        });
    }
}