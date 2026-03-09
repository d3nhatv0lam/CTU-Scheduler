using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Helpers;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Infrastructure.DriverCore.Response;
using CTUScheduler.Infrastructure.Services.Network;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.DriverCore;

public record DialogInfo(string Message, string DefaultValue = "");

public class WebDriverService : IWebDriverService, IAsyncDisposable
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly ILogger<WebDriverService> _logger;
    private readonly IConnectivityService _connectivityService;
    
    private readonly Subject<DialogInfo> _alertSubject = new();
    private readonly Subject<DialogInfo> _confirmSubject = new();
    private readonly Subject<DialogInfo> _promptSubject = new();
    private readonly ReplaySubject<string> _installationStatusSubject = new(1);
    private readonly BehaviorSubject<bool> _isInstallingSubject = new(false);
    private readonly ReplaySubject<string> _installationProgressSubject = new();
    private readonly Subject<NetworkPacket> _jsonResponseSubject = new();
    private readonly BehaviorSubject<string> _navigationSubject = new("");

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;

    private IPage Page => _page ??
                          throw new InvalidOperationException(
                              "Browser chưa khởi tạo. Vui lòng gọi InitWebDriverService() trước.");

    public IPage? CurrentPage => _page;
    public string PageUrl => _page?.Url ?? string.Empty;
    public IObservable<string> MainFrameUrlChanges { get; }
    public IObservable<string> InstallationStatus { get; }
    public IObservable<bool> IsInstalling { get; }
    public IObservable<string> InstallationProgress { get; }
    public IObservable<DialogInfo> AlertReceived { get; }
    public IObservable<DialogInfo> ConfirmReceived { get; }
    public IObservable<DialogInfo> PromptReceived { get; }
    public IObservable<NetworkPacket> JsonResponse { get; }

    public WebDriverService(IConnectivityService connectivityService,IHostApplicationLifetime appLifetime, ILogger<WebDriverService> logger)
    {
        _connectivityService = connectivityService;
        _logger = logger;

        _alertSubject.DisposeWith(_disposables);
        _confirmSubject.DisposeWith(_disposables);
        _promptSubject.DisposeWith(_disposables);
        _installationProgressSubject.DisposeWith(_disposables);
        _installationStatusSubject.DisposeWith(_disposables);
        _isInstallingSubject.DisposeWith(_disposables);
        _jsonResponseSubject.DisposeWith(_disposables);
        _navigationSubject.DisposeWith(_disposables);
        
        MainFrameUrlChanges = _navigationSubject.AsObservable();
        InstallationStatus = _installationStatusSubject.AsObservable();
        IsInstalling = _isInstallingSubject.AsObservable();
        InstallationProgress = _installationProgressSubject.AsObservable();
        AlertReceived = _alertSubject.AsObservable();
        ConfirmReceived = _confirmSubject.AsObservable();
        PromptReceived = _promptSubject.AsObservable();
        JsonResponse = _jsonResponseSubject.AsObservable();
    }

    public async Task InitWebDriverService(CancellationToken cancellationToken = default)
    {
        await EnsureBrowserInstalledAsync(cancellationToken);
        await CreatePlayWrightChromiumAsync();
        await ConfigPageAsync();
    }

    protected virtual async Task ConfigPageAsync()
    {
        //await _page.RouteAsync("**/*.css", async route => await route.AbortAsync());
        await Page.RouteAsync("**/*", async route =>
        {
            if (route.Request.ResourceType is "stylesheet" or "font")
                await route.AbortAsync();
            else
                await route.ContinueAsync();
        });

        // Navigate
        Observable.FromEventPattern<EventHandler<IFrame>, IFrame>(
                h => Page.FrameNavigated += h,
                h => Page.FrameNavigated -= h)
            .Select(e => e.EventArgs)
            .Where(frame => frame == Page.MainFrame)
            .Select(frame => frame.Url)
            .Subscribe(url => _navigationSubject.OnNext(url))
            .DisposeWith(_disposables);

        Observable.FromEventPattern<EventHandler<IDialog>, IDialog>(
                h => Page.Dialog += h,
                h => Page.Dialog -= h)
            .Select(x => x.EventArgs)
            .SelectMany(async dialog =>
            {
                var info = new DialogInfo(dialog.Message, dialog.DefaultValue);

                _logger.LogInformation("[Dialog] {Type}: {Message}", dialog.Type, dialog.Message);

                Task actionTask = dialog.Type switch
                {
                    DialogType.Alert => HandleAlert(dialog, info),
                    DialogType.Confirm => HandleConfirm(dialog, info),
                    DialogType.Prompt => HandlePrompt(dialog, info),
                    _ => dialog.DismissAsync()
                };

                await actionTask;

                return Unit.Default;
            })
            .Subscribe()
            .DisposeWith(_disposables);

        Observable.FromEventPattern<EventHandler<IResponse>, IResponse>(
                h => Page.Response += h,
                h => Page.Response -= h)
            .Select(x => x.EventArgs)
            .Where(e => e.Request.ResourceType == "fetch" || e.Request.ResourceType == "xhr")
            .Where(e =>
            {
                try {
                    return e.Headers.TryGetValue("content-type", out var ct) && 
                           ct.Contains("json", StringComparison.OrdinalIgnoreCase);
                } catch { return false; }
            })
            .SelectMany(e => Observable.FromAsync(async () =>
                {
                    try
                    {
                        var jsonString = await e.TextAsync();
                        if (string.IsNullOrWhiteSpace(jsonString)) return null;
                        return new NetworkPacket
                        {
                            Url = e.Request.Url,
                            Method = e.Request.Method,
                            RawBody = jsonString
                        };
                    }
                    catch { return null; }
                }).Catch(Observable.Return<NetworkPacket?>(null))
            )
            .Where(packet => packet != null)
            .Subscribe(packet => _jsonResponseSubject.OnNext(packet!))
            .DisposeWith(_disposables);
    }

    public async Task<bool> TryWaitForUrlAsync(string url, int timeout = 10000)
    {
        try
        {
            await Page.WaitForURLAsync(url, new PageWaitForURLOptions() { Timeout = timeout });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task WaitForTimeoutAsync(float milisecond)
    {
        await Page.WaitForTimeoutAsync(milisecond);
    }

    public async Task GoToPageAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url));

        await EnsureInternetConnection();
        
        await Page.GotoAsync(url, new PageGotoOptions { Timeout = 10000, WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForLoadStateAsync(LoadState.Load);
    }

    public async Task RefreshPageAsync()
    {
        await Page.ReloadAsync(new PageReloadOptions() { WaitUntil = WaitUntilState.NetworkIdle });
    }

    public ILocator GetLocator(string selector)
    {
        return Page.Locator(selector);
    }

    public async Task ClickNavigateElementAsync(ILocator element, LocatorClickOptions? options = null,
        LoadState loadState = LoadState.NetworkIdle)
    {
        try
        {
            await EnsureInternetConnection();
            var waitForLoadTask = Page.WaitForLoadStateAsync(loadState);
            await element.ClickAsync(options);
            await waitForLoadTask;
        }
        catch (Exception e)
        {
            _logger.LogWarning("Click navigate element fail! Reason: {Message}", e.Message);
            throw;
        }
    }


    public async Task ClickNavigateElementAsync(string selector, LocatorClickOptions? options = null,
        LoadState loadState = LoadState.NetworkIdle)
    {
        ILocator element = GetLocator(selector);
        await ClickNavigateElementAsync(element, options, loadState);
    }

    private async Task EnsureInternetConnection()
    {
        if (!await _connectivityService.CheckInternetAccessAsync())
            throw new NoInternetException("Can't access because no internet!");
    }

    private async Task HandleAlert(IDialog dialog, DialogInfo info)
    {
        _alertSubject.OnNext(info);

        await dialog.DismissAsync();
    }

    private async Task HandleConfirm(IDialog dialog, DialogInfo info)
    {
        _confirmSubject.OnNext(info);

        await dialog.AcceptAsync();
    }

    private async Task HandlePrompt(IDialog dialog, DialogInfo info)
    {
        _promptSubject.OnNext(info);
        await dialog.AcceptAsync("Giá trị mặc định từ AutoTool");
    }

    private async Task EnsureBrowserInstalledAsync(CancellationToken cancellationToken = default)
    {
        var (scriptPath, browserDir) = GetInstallationPaths();

        // Cấu hình Environment ngay từ đầu để CheckIntegrity hoạt động đúng
        Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", browserDir);

        try
        {
            // Kiểm tra nhanh 
            _installationStatusSubject.OnNext("Đang kiểm tra trạng thái trình duyệt...");
            if (await CheckBrowserIntegrity())
            {
                _installationStatusSubject.OnNext("Trình duyệt đã sẵn sàng!");
                return;
            }

            // cài đặt
            _isInstallingSubject.OnNext(true);
            _installationStatusSubject.OnNext("Đang tải tài nguyên...");

            // Dọn dẹp rác cũ trước khi cài
            CleanupOldInstallation(browserDir);

            // Gọi hàm chuyên dụng để chạy Process
            await RunInstallerProcessAsync(scriptPath, browserDir, cancellationToken);
            _installationStatusSubject.OnNext("Tải tài nguyên thành công!");

            // 4. Kiểm tra lại
            _installationStatusSubject.OnNext("Đang xác thực lại...");
            if (!await CheckBrowserIntegrity())
            {
                throw new Exception("Quá trình cài đặt báo thành công nhưng không khởi động được trình duyệt.");
            }

            _installationStatusSubject.OnNext("Cài đặt hoàn tất! Sẵn sàng sử dụng.");
        }
        catch (Exception ex)
        {
            _installationStatusSubject.OnNext($"Lỗi: {ex.Message}");
            _logger.LogError(ex, "Playwright installation failed");
            throw;
        }
        finally
        {
            _isInstallingSubject.OnNext(false);
        }
    }


    private async Task CreatePlayWrightChromiumAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
        _page = await _browser.NewPageAsync();
    }

    private async Task RunInstallerProcessAsync(string scriptPath, string browserDir, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Launching playwright installer...");

        string[] installArgs = ["install", "chromium"];

        var command = ProcessHelper.PrepareShellCommand(scriptPath, installArgs);

        string workingDir = Path.GetDirectoryName(scriptPath) ?? AppDomain.CurrentDomain.BaseDirectory;

        var envVars = new Dictionary<string, string>
        {
            { "PLAYWRIGHT_BROWSERS_PATH", browserDir }
        };

        try
        {
            int exitCode = await ProcessHelper.RunScriptAsync(
                fileName: command.FileName,
                arguments: command.Args,
                workingDir: workingDir,
                envVars: envVars,
                onOutput: (msg) => ForwardLogToSubject(msg),
                onError: (msg) => ForwardLogToSubject(msg, isError: true),
                cancellationToken: cancellationToken
            );

            if (exitCode != 0)
            {
                throw new Exception($"Installer exited with code {exitCode}");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Playwright installation cancelled");
        }

        _isInstallingSubject.OnNext(false);
    }

    /// <summary>
    /// Lấy và kiểm tra đường dẫn file script/folder
    /// </summary>
    private (string ScriptPath, string BrowserDir) GetInstallationPaths()
    {
        string rootPath = AppDomain.CurrentDomain.BaseDirectory;
        string browserDir = Path.Combine(rootPath, "browser_bin");
        string scriptPath = Path.Combine(rootPath, "playwright.ps1");

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Không tìm thấy file cài đặt tại: {scriptPath}");
        }

        return (scriptPath, browserDir);
    }

    /// <summary>
    /// Xóa folder cũ
    /// </summary>
    private void CleanupOldInstallation(string path)
    {
        if (Directory.Exists(path))
        {
            try
            {
                _installationProgressSubject.OnNext("> Đang dọn dẹp dữ liệu cũ...\n");
                Directory.Delete(path, true);
                _installationProgressSubject.OnNext("> Dọn dẹp dữ liệu cũ thành công!\n");
            }
            catch (Exception ex)
            {
                // Warning nhẹ, không chặn quy trình
                _logger.LogWarning(ex,$"Fail to delete old folder!");
            }
        }
    }

    private void ForwardLogToSubject(string? message, bool isError = false)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        string prefix = isError ? "[SYSTEM ERROR] " : "";
        _installationProgressSubject.OnNext($"{prefix}{message}{Environment.NewLine}");
    }

    /// <summary>
    /// Thử khởi động trình duyệt ẩn danh nhanh để xem file có hỏng không
    /// </summary>
    private async Task<bool> CheckBrowserIntegrity()
    {
        try
        {
            // Tạo instance Playwright nhẹ
            using var playwright = await Playwright.CreateAsync();

            // Nếu file lỗi,throw exception
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            await browser.CloseAsync();
            return true;
        }
        catch (Exception)
        {
            // Bất cứ lỗi gì (thiếu file, lỗi version, permission) trả về false
            return false;
        }
    }


    public async ValueTask DisposeAsync()
    {
        if (_page is not null)
            await _page.CloseAsync();
        if (_browser is not null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
        }
        _playwright?.Dispose();
        _disposables.Dispose();
        _logger.LogInformation("WebDriverService Disposed!");
    }
}