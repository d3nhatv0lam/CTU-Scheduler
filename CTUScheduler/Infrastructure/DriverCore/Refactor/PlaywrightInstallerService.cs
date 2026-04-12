using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.DriverCore.Refactor;

public class PlaywrightInstallerService: IWebDriverInstallerService, IDisposable
{
    private readonly ILogger<PlaywrightInstallerService> _logger;
    private readonly BehaviorSubject<string> _statusMessageSubject = new("...");
    private readonly BehaviorSubject<double?> _progressPercentageSubject = new(null);
    private readonly BehaviorSubject<bool> _isBusySubject = new(false);
    private readonly ReplaySubject<string> _logStreamSubject = new();
    
    private bool _isDisposed;
    
    public IObservable<string> StatusMessage { get; }
    public IObservable<bool> IsBusy { get; }
    public IObservable<double?> ProgressPercentage { get; }
    public IObservable<string> LogStream { get; }

    public PlaywrightInstallerService(ILogger<PlaywrightInstallerService> logger)
    {
        _logger = logger;
        StatusMessage = _statusMessageSubject.AsObservable();
        IsBusy = _isBusySubject.AsObservable();
        ProgressPercentage = _progressPercentageSubject.AsObservable();
        LogStream = _logStreamSubject.AsObservable();
    }


    public async Task EnsureBrowserInstalledAsync(CancellationToken cancellationToken = default)
    {
        var (scriptPath, browserDir) = GetInstallationPaths();

        // Cấu hình Environment ngay từ đầu để CheckIntegrity hoạt động đúng
        Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", browserDir);

        try
        {
            // 1. Kiểm tra nhanh 
            _statusMessageSubject.OnNext("Đang kiểm tra trạng thái trình duyệt Chromium...");
            if (await CheckBrowserIntegrity())
            {
                _statusMessageSubject.OnNext("Trình duyệt đã sẵn sàng!");
                return;
            }

            // 2. Bắt đầu cài đặt
            _isBusySubject.OnNext(true);
            _statusMessageSubject.OnNext("Đang tải tài nguyên Chromium...");

            // Dọn dẹp rác cũ trước khi cài
            CleanupOldInstallation(browserDir);

            // 3. Chạy Installer
            await RunInstallerProcessAsync(scriptPath, browserDir, cancellationToken);
            _statusMessageSubject.OnNext("Tải tài nguyên thành công!");

            // 4. Kiểm tra lại lần cuối
            _statusMessageSubject.OnNext("Đang xác thực lại hệ thống...");
            if (!await CheckBrowserIntegrity())
            {
                throw new Exception("Quá trình cài đặt báo thành công nhưng không khởi động được trình duyệt.");
            }

            _statusMessageSubject.OnNext("Cài đặt hoàn tất! Sẵn sàng sử dụng.");
        }
        catch (Exception ex)
        {
            _statusMessageSubject.OnNext($"Lỗi: {ex.Message}");
            _logger.LogError(ex, "Playwright installation failed");
            throw;
        }
        finally
        {
            _isBusySubject.OnNext(false);
        }
    }

    private async Task RunInstallerProcessAsync(string scriptPath, string browserDir, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Launching playwright installer for Chromium...");

        // Hardcode Chromium như cũ
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
    }

    private async Task<bool> CheckBrowserIntegrity()
    {
        try
        {
            using var playwright = await Playwright.CreateAsync();
            
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

            await browser.CloseAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

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

    private void CleanupOldInstallation(string path)
    {
        if (Directory.Exists(path))
        {
            try
            {
                _logStreamSubject.OnNext("> Đang dọn dẹp dữ liệu cũ...\n");
                Directory.Delete(path, true);
                _logStreamSubject.OnNext("> Dọn dẹp dữ liệu cũ thành công!\n");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fail to delete old folder!");
            }
        }
    }

    private void ForwardLogToSubject(string? message, bool isError = false)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        string prefix = isError ? "[SYSTEM ERROR] " : "";
        _logStreamSubject.OnNext($"{prefix}{message}{Environment.NewLine}");
    }
    
    
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
       
        _statusMessageSubject.Dispose();
        _isBusySubject.Dispose();
        _progressPercentageSubject.Dispose();
        _logStreamSubject.Dispose();
        _logger.LogInformation("Playwright installer service disposed!");
    }
}