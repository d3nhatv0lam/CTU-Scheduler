using System;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.DriverCore;

public class PlaywrightService : IWebDriverService, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<PlaywrightService> _logger;
    private readonly IWebDriverInstallerService _installer;
    private bool _isDisposed;
    private bool _isInitialized;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IWebTab? _mainPage;

    public IWebTab MainTab
    {
        get => _mainPage ??
               throw new InvalidOperationException("Browser chưa được khởi tạo. Hãy gọi InitBrowserAsync trước!");
        private set => _mainPage = value;
    }

    public PlaywrightService(IWebDriverInstallerService installer, ILogger<PlaywrightService> logger)
    {
        _logger = logger;
        _installer = installer;
    }

    public async Task InitBrowserAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_isInitialized) return;
            await InitInternalAsync();
            _isInitialized = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ResetBrowserAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!_isInitialized)
            {
                await InitInternalAsync();
                _isInitialized = true;
                return;
            }

            if (_context is not null)
            {
                await MainTab.DisposeAsync();
                await _context.CloseAsync();
            }

            _context = await _browser!.NewContextAsync();
            MainTab = await CreateTabInternalAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IWebTab> CreateTabAsync()
    {
        if (_context is null) throw new InvalidOperationException("Browser Context not ready.");
        return await CreateTabInternalAsync();
    }

    private async Task InitInternalAsync()
    {
        await _installer.EnsureBrowserInstalledAsync();
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = false });
        _context = await _browser.NewContextAsync();
        MainTab = await CreateTabInternalAsync();
        _logger.LogInformation("PlaywrightService Initialized!");
    }

    private async Task<IWebTab> CreateTabInternalAsync()
    {
        return await WebTab.CreateAsync(await _context!.NewPageAsync());
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_isDisposed) return;

            if (_mainPage is not null) await MainTab.DisposeAsync().ConfigureAwait(false);
            if (_context is not null) await _context.DisposeAsync().ConfigureAwait(false);
            if (_browser is not null) await _browser.DisposeAsync().ConfigureAwait(false);

            _playwright?.Dispose();

            _isInitialized = false;
            _isDisposed = true;
            _logger.LogInformation("PlaywrightService Disposed!");
        }
        finally
        {
            _lock.Release();
        }
    }
}