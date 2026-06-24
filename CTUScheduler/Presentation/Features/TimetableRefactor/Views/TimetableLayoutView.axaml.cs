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
            ViewModel?.CopyToClipboardInteraction.RegisterHandler(async context =>
            {
                TimetableLayoutView? tempView = null;
                try
                {
                    var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                    if (clipboard == null)
                    {
                        context.SetOutput(false);
                        return;
                    }

                    // 1. Tạo một bản sao ẩn của View dưới nền
                    tempView = new TimetableLayoutView
                    {
                        DataContext = this.DataContext,
                        Width = 1600,
                        Height = 1000
                    };

                    var exportPanel = tempView.FindControl<Control>("ExportPanel");
                    exportPanel?.IsVisible = false;

                    // không using ở đây vì bitmap đưa ra clipboard phải do avalonia quản lý
                    // docs: https://docs.avaloniaui.net/docs/services/clipboard
                    var bitmap = await ViewModel.ControlRendererService.RenderToBitmapAsync(
                        tempView,
                        width: 1600,
                        height: 1000,
                        scale: 1.5,
                        dpi: 144);

                    await clipboard.SetBitmapAsync(bitmap);
                    // Hoạt động kém ở macos
                    await clipboard.FlushAsync();

                    context.SetOutput(true);
                }
                catch
                {
                    context.SetOutput(false);
                }
                finally
                {
                    tempView?.DataContext = null;
                }
            }).DisposeWith(disposable);
        });
    }
}