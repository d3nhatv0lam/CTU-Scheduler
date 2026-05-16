using Avalonia;
using Avalonia.Markup.Xaml;
using Serilog;
using Avalonia.Threading;
using CTUScheduler.Presentation.Services.ApplicationStartup;

namespace CTUScheduler;

public class App : Application
{
    public IAppStartup? Startup { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        SetupGlobalExceptionHandling();
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Dispatcher.UIThread.UnhandledException += (sender, e) =>
        {
            var uiLog = Log.ForContext("ShortTypeName", "UI");
            uiLog.Error(e.Exception, "Avalonia UI Thread Unhandled Exception");
            // e.Handled = true;   // Chỉ bật nếu bạn chắc chắn muốn app tiếp tục (rủi ro cao)
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