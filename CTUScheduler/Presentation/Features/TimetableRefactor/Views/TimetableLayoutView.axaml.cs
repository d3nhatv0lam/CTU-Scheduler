using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.Views;

public partial class TimetableLayoutView : ReactiveUserControl<TimetableLayoutBaseViewModel>
{
    public TimetableLayoutView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.OneWayBind(ViewModel,
                x => x.Name,
                v => v.TitleTextBlock.Text,
                 title => $"Thời khóa biểu: {title}")
                .DisposeWith(disposable);
            // this.Bind(ViewModel,
            //     x => x.Description,
            //     v => v.DescriptionTextBox.Text
            //     ).DisposeWith(disposable);
            this.OneWayBind(ViewModel,
                x => x.SubjectsCount,
                v => v.SubjectsCountTextBlock.Text,
                text => $"Số môn: {text}")
                .DisposeWith(disposable);
            this.OneWayBind(ViewModel,
                    x => x.TotalCredits,
                    v => v.TotalCreditTextBlock.Text,
                    text => $"Số tín chỉ: {text}")
                .DisposeWith(disposable);
            this.OneWayBind(ViewModel,
                x => x.LastUpdated,
                v => v.LastUpdateTextBlock.Text,
                updateTime => $"Lần cuối cập nhật: {updateTime}")
                .DisposeWith(disposable);
        });
    }
}