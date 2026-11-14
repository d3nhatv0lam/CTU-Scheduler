using System;
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
            OpenEmergencyWindow(exception, isFatal);
        }
        else
        {
            Dispatcher.UIThread.Post(() => OpenEmergencyWindow(exception, isFatal));
        }
    }

    private static void OpenEmergencyWindow(Exception exception, bool isFatal)
    {
        var vm = new CrashReportViewModel(exception, isFatal);
        var window = new CrashReportWindow { DataContext = vm };
        if (isFatal)
        {
            window.Closed += (_, _) => Environment.Exit(1);
        }
        window.Show();
    }
}
