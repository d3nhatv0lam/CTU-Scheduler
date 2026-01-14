
using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CTUScheduler.AppServices.Services.Network;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Infrastructure.DriverCore;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.SplashScreen.Components.Installation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.SplashScreen.ViewModels;

public partial class TestSpashWindowViewModel : ViewModelBase, IDisposable, IRequestClose
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IConnectivityService _connectivityService;
    private readonly IWebDriverService _webDriverService;

    // --- 1. CONSTANTS CHO VIỆC RESIZE ---
    private const double SMALL_WIDTH = 300;
    private const double SMALL_HEIGHT = 400;
    private const double LARGE_WIDTH = 850;
    private const double LARGE_HEIGHT = 500;

    // --- 2. PROPERTIES CHO VIEW BINDING ---
    
    // VM con để hứng Log chi tiết (Binding vào phần bên phải của Window)
    public InstallationViewModel InstallationViewModel { get; }

    [Reactive] private string _message  = "Đang kiểm tra kết nối mạng";
    
    // Binding kích thước cửa sổ
    [Reactive] 
    private double _windowWidth = SMALL_WIDTH;
    [Reactive] 
    private double _windowHeight = SMALL_HEIGHT;
    
    [Reactive] 
    private bool _isExpanded = false; 

    public string Version => AppConstants.AppVersion;

    // --- 3. COMMANDS ---
    public ReactiveCommand<Unit, Unit> CloseAppCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleConsoleCommand { get; }
    
    public event Action<object?>? RequestClose;

    public TestSpashWindowViewModel()
    {
        // Dependency Injection
        _connectivityService = App.ServiceProvider.GetRequiredService<IConnectivityService>();
        _webDriverService = App.ServiceProvider.GetRequiredService<IWebDriverService>();

        // Init Commands
        CloseAppCommand = ReactiveCommand.Create(CloseApplication).DisposeWith(_disposables);
        ToggleConsoleCommand = ReactiveCommand.Create(ToggleConsole).DisposeWith(_disposables);

        // A. Khởi tạo VM con (InstallationViewModel) ngay lập tức
        // Truyền Observable log vào để nó tự xử lý text
        InstallationViewModel = new InstallationViewModel(_webDriverService.InstallationProgress);

        // B. Lắng nghe trạng thái cài đặt để cập nhật Message vắn tắt trên Splash
        _webDriverService.InstallationStatus
            .Delay(TimeSpan.FromSeconds(0.1), RxApp.MainThreadScheduler) // Delay nhẹ cho mượt
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(message => Message = message)
            .DisposeWith(_disposables);

        // C. Logic tự động mở rộng (Optional - Nếu bạn muốn khi cài đặt thì tự to ra thì uncomment)
        // Hiện tại ta để User tự bấm nút như bạn yêu cầu
        /*
        _webDriverService.IsInstalling
            .Where(x => x)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => 
            {
                if (!IsExpanded) ToggleConsole();
            })
            .DisposeWith(_disposables);
        */

        // D. Bắt đầu quy trình kiểm tra Startup
        InitializeStartup();
    }

    /// <summary>
    /// Hàm xử lý logic "Biến hình" cửa sổ
    /// </summary>
    public void ToggleConsole()
    {
        IsExpanded = !IsExpanded;
        if (IsExpanded)
        {
            WindowWidth = LARGE_WIDTH;
            WindowHeight = LARGE_HEIGHT;
        }
        else
        {
            WindowWidth = SMALL_WIDTH;
            WindowHeight = SMALL_HEIGHT;
        }
    }

    /// <summary>
    /// Quy trình khởi động: Mạng -> WebDriver -> Vào App
    /// </summary>
    private void InitializeStartup()
    {
        _connectivityService.IsInternetAvailable
            .Where(status => status)
            .Take(1)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => Message = "Mạng đã được kết nối!")
            .Delay(TimeSpan.FromSeconds(1.0), RxApp.MainThreadScheduler) // Giảm delay chút cho nhanh
            .Do(_ => Message = "Đang kiểm tra dịch vụ web...")
            .ObserveOn(RxApp.TaskpoolScheduler)
            .SelectMany(async _ =>
            {
                // Playwright sẽ chạy ngầm ở đây. 
                // InstallationViewModel sẽ tự hứng log từ _webDriverService.InstallationProgress
                await _webDriverService.InitWebDriverService();
                return Unit.Default;
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => Message = "Dịch vụ web đã hoạt động!")
            .Delay(TimeSpan.FromSeconds(1.0), RxApp.MainThreadScheduler)
            .Do(_ => Message = "Đang khởi động ứng dụng...")
            .Delay(TimeSpan.FromSeconds(1.0), RxApp.MainThreadScheduler)
            .Subscribe(
                _ => Close(), // Thành công -> Đóng Splash -> Vào Main
                ex =>
                {
                    if (!IsExpanded) ToggleConsole();
                    
                    Message = "Lỗi khởi động! Xem chi tiết bên phải.";
                })
            .DisposeWith(_disposables);
    }

    public void Close(object? result = null)
    {
        RequestClose?.Invoke(null);
    }

    private void CloseApplication()
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    public void Dispose()
    {
        InstallationViewModel.Dispose(); 
        _disposables.Dispose();
    }
}