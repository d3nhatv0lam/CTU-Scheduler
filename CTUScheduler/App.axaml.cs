using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CTUScheduler.Presentation.Services.ApplicationStartup;
using CTUScheduler.Presentation.Services.CrashReport;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Serilog;
using SkiaSharp;

namespace CTUScheduler;

public class App : Application
{
    public IAppStartup? Startup { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ConfigureLiveCharts();
        SetupGlobalExceptionHandling();
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    private static void ConfigureLiveCharts()
    {
        var vietnameseTypeface = SKFontManager.Default.MatchCharacter('Đ')
            ?? SKTypeface.FromFamilyName("Segoe UI")
            ?? SKTypeface.Default;

        LiveCharts.Configure(config => config
            .UseDefaults()
            .HasTextSettings(new TextSettings { DefaultTypeface = vietnameseTypeface }));
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Dispatcher.UIThread.UnhandledException += (sender, e) =>
        {
            var uiLog = Log.ForContext("ShortTypeName", "UI");
            uiLog.Error(e.Exception, "Avalonia UI Thread Unhandled Exception");
            e.Handled = true;
            CrashReporter.Show(e.Exception, isFatal: false);
        };

        if (ApplicationLifetime is not null)
        {
            Startup?.Initialize(ApplicationLifetime);
        }

        base.OnFrameworkInitializationCompleted();
    }


    private void SetupGlobalExceptionHandling()
    {
        // Bắt lỗi của ReactiveUI
        // RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex => 
        // {
        //     var rxLog = Log.ForContext("ShortTypeName", "UI");
        //     rxLog.Error(ex, "ReactiveUI Pipeline/Command Exception");
        //     // có thể hiển thị Dialog báo lỗi cho User tại đây
        // });
    }
}