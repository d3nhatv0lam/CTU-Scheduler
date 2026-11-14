using System;

namespace CTUScheduler.Presentation.Services.CrashReport;

public interface ICrashReportService
{
    void ShowCrashReport(Exception exception, bool isFatal = false);
    bool IsMuted(Exception exception);
    void Mute(Exception exception);
}
