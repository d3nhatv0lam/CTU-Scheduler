using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using CTUScheduler.Core.Helpers;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Shared.Interfaces;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.CrashReport.ViewModels;

public partial class CrashReportViewModel : ViewModelBase, IRequestClose
{
    public event Action<object?>? RequestClose;

    public bool IsFatal { get; }
    public string WindowTitle { get; }
    public string HeaderTitle { get; }
    public string HeaderSubtitle { get; }
    public string ErrorDetailsText { get; }

    [Reactive] private bool _muteThisError;
    [Reactive] private string _copyButtonText = "Sao chép lỗi";

    public ReactiveCommand<Unit, Unit> CopyErrorCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenLogFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> RestartAppCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }

    public CrashReportViewModel(Exception? exception = null, bool isFatal = false)
    {
        IsFatal = isFatal;

        WindowTitle = isFatal
            ? "CTU Scheduler - Sự cố ứng dụng nghiêm trọng"
            : "CTU Scheduler - Đã xảy ra lỗi tác vụ";

        HeaderTitle = isFatal
            ? "SỰ CỐ ỨNG DỤNG NGHIÊM TRỌNG"
            : "ĐÃ XẢY RA LỖI TÁC VỤ";

        HeaderSubtitle = isFatal
            ? "Ứng dụng gặp sự cố ngoài dự kiến và không thể tiếp tục hoạt động."
            : "Có lỗi phát sinh trong quá trình xử lý tác vụ này. Bạn có thể tiếp tục sử dụng ứng dụng.";

        ErrorDetailsText = BuildErrorDetails(exception);

        CopyErrorCommand = ReactiveCommand.CreateFromTask(CopyErrorAsync);
        OpenLogFolderCommand = ReactiveCommand.Create(OpenLogFolder);
        RestartAppCommand = ReactiveCommand.Create(RestartApp);
        CloseCommand = ReactiveCommand.Create(() => Close());
    }

    private static string BuildErrorDetails(Exception? ex)
    {
        var asm = Assembly.GetEntryAssembly() ?? typeof(CrashReportViewModel).Assembly;
        var infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? asm.GetName().Version?.ToString(3) ?? "1.1.0";

        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var os = Environment.OSVersion.ToString();

        if (ex is null)
        {
            return $"Thời gian: {time}\nPhiên bản: {infoVer}\nHệ điều hành: {os}\nKhông có thông tin ngoại lệ.";
        }

        return $"Thời gian: {time}\n" +
               $"Phiên bản: {infoVer}\n" +
               $"Hệ điều hành: {os}\n" +
               $"Ngoại lệ: {ex.GetType().FullName}\n" +
               $"Thông điệp: {ex.Message}\n\n" +
               $"Vết ngăn xếp (StackTrace):\n{ex.StackTrace}";
    }

    private async Task CopyErrorAsync()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var topLevel = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
                var clipboard = topLevel?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(ErrorDetailsText);
                    await clipboard.FlushAsync();
                    CopyButtonText = "Đã chép!";
                    await Task.Delay(2000);
                    CopyButtonText = "Sao chép lỗi";
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi clipboard
        }
    }

    private static void OpenLogFolder()
    {
        try
        {
            var logFolder = PathProvider.GetLogFolder();
            if (Directory.Exists(logFolder))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = logFolder,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Bỏ qua nếu không thể mở Explorer
        }
    }

    private static void RestartApp()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // ignored
        }
        finally
        {
            Environment.Exit(1);
        }
    }

    public void Close(object? result = null)
    {
        RequestClose?.Invoke(result);
        if (IsFatal)
        {
            Environment.Exit(1);
        }
    }
}
