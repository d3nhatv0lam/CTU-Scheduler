using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.DriverCore.Refactor;

public class PlaywrightService: IWebDriverService, IAsyncDisposable
{
    private readonly ILogger<PlaywrightService> _logger;
    private readonly IWebDriverInstallerService _installer;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;

    public PlaywrightService(IWebDriverInstallerService installer, ILogger<PlaywrightService> logger)
    {
        _logger = logger;
        _installer = installer;
    }

    public async Task InitBrowserAsync()
    {
        await _installer.EnsureBrowserInstalledAsync();

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = false,
            });
        _context = await _browser.NewContextAsync();
    }

    public async Task<IWebTab> CreateTabAsync()
    {
        if (_context is null) throw new InvalidOperationException("Browser Context not ready.");
        return new WebTab(await _context.NewPageAsync());
    }

    public async ValueTask DisposeAsync()
    {
        if (_context != null) await _context.DisposeAsync();
        if (_browser != null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        _logger.LogInformation("PlaywrightService Disposed!");
    }
}