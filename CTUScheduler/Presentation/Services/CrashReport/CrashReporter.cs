using System;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CTUScheduler.Presentation.Features.CrashReport.ViewModels;
using CTUScheduler.Presentation.Features.CrashReport.Views;
using Serilog;

namespace CTUScheduler.Presentation.Services.CrashReport;

public static class CrashReporter
{
    public static ICrashReportService? Current { get; set; }

    public static void Show(Exception? exception, bool isFatal = false)
    {
        if (exception is null) return;

        try
        {
            if (Current != null)
            {
                Current.ShowCrashReport(exception, isFatal);
            }
            else
            {
                ShowEmergency(exception, isFatal);
            }
        }
        catch (Exception ex)
        {
            Log.ForContext("ShortTypeName", "Host")
                .Fatal(ex, "Failed to handle crash via CrashReporter");
        }
    }

    private static void ShowEmergency(Exception exception, bool isFatal)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            OpenEmergencyWindow(exception, isFatal, null);
        }
        else
        {
            var waitHandle = new ManualResetEventSlim(false);
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    OpenEmergencyWindow(exception, isFatal, waitHandle);
                }
                catch
                {
                    waitHandle.Set();
                }
            });

            if (isFatal)
            {
                waitHandle.Wait();
            }
        }
    }

    private static async void OpenEmergencyWindow(Exception exception, bool isFatal, ManualResetEventSlim? waitHandle)
    {
        var vm = new CrashReportViewModel(exception, isFatal);
        var window = new CrashReportWindow { DataContext = vm };
        window.Closed += (_, _) =>
        {
            waitHandle?.Set();
            if (isFatal)
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown(1);
                }
                else
                {
                    Environment.Exit(1);
                }
            }
        };

        Window? owner = null;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            owner = desktop.Windows.FirstOrDefault(w => w.IsVisible && w is not CrashReportWindow) ?? desktop.MainWindow;
        }

        if (owner != null && owner.IsVisible)
        {
            await window.ShowDialog(owner);
        }
        else
        {
            window.Show();
            window.Activate();
        }
    }
}
