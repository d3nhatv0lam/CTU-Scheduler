using System;
using System.Reactive.Disposables.Fluent;
using ReactiveUI.Avalonia;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using ReactiveUI;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;

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

            ViewModel!.CopyToClipboardInteraction.RegisterHandler(async context =>
            {
                try
                {
                    var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                    if (clipboard == null)
                    {
                        context.SetOutput(false);
                        return;
                    }

                    var bounds = this.Bounds;
                    double width = bounds.Width > 0 ? bounds.Width : 1200;
                    double height = bounds.Height > 0 ? bounds.Height : 800;

                    var dpi = 96.0;
                    var pixelSize = new PixelSize((int)width, (int)height);

                    // bitmap này do avalonia quản lý, không phải memleak của dev
                    // docs: https://docs.avaloniaui.net/docs/services/clipboard
                    var bitmap = new RenderTargetBitmap(pixelSize, new Vector(dpi, dpi));
                    bitmap.Render(this);
                    await clipboard.SetBitmapAsync(bitmap);
                    await clipboard.FlushAsync();

                    context.SetOutput(true);
                }
                catch
                { 
                    context.SetOutput(false);
                }
            }).DisposeWith(disposable);
        });
    }
}