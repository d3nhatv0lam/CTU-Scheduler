using System;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTUScheduler.Presentation.Features.TimeTable.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimeTable.Views;

public partial class TimeTableLayoutView : ReactiveUserControl<TimeTableLayoutViewModel>
{
    public TimeTableLayoutView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.OneWayBind(ViewModel,
                x => x.Name,
                v => v.TitleTextBlock.Text)
                .DisposeWith(disposable);
            this.OneWayBind(ViewModel,
                x => x.Description,
                v => v.DescriptionTextBox.Text
                ).DisposeWith(disposable);
            this.OneWayBind(ViewModel,
                x => x.SubjectsCount,
                v => v.SubjectsCountTextBlock.Text)
                .DisposeWith(disposable);
            this.OneWayBind(ViewModel,
                    x => x.TotalCredit,
                    v => v.TotalCreditTextBlock.Text)
                .DisposeWith(disposable);
            this.OneWayBind(ViewModel,
                x => x.LastUpdated,
                v => v.LastUpdateTextBlock.Text)
                .DisposeWith(disposable);
        });
    }
}