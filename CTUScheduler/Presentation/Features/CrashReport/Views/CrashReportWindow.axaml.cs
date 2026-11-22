using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using CTUScheduler.Presentation.Features.CrashReport.ViewModels;
using CTUScheduler.Presentation.Shared.Interfaces;
using ReactiveUI;
using Ursa.ReactiveUIExtension;

namespace CTUScheduler.Presentation.Features.CrashReport.Views;

public partial class CrashReportWindow : ReactiveUrsaWindow<CrashReportViewModel>
{
    public CrashReportWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            if (ViewModel is IRequestClose requestClose)
            {
                Action<object?> handler = _ => Close();
                requestClose.RequestClose += handler;
                Disposable.Create(() => requestClose.RequestClose -= handler)
                    .DisposeWith(disposables);
            }
        });
    }
}
