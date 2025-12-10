using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.Network;
using CTUScheduler.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.AppServices.Services.WebDriver
{
    public class WebDriverService : IWebDriverService, IDisposable , IAsyncDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ILogger<WebDriverService> _logger;
        private readonly IConnectivityService _connectivityService;
        private IPlaywright _playwright = null!;
        private IBrowser _browser = null!;
        private IPage _page = null!;
        private Subject<JsonElement?> _jsonResponseSubject = new ();
        protected bool _isHasInternet;
        
        private readonly ReplaySubject<string> _installationStatusSubject = new ReplaySubject<string>(1);
        private readonly BehaviorSubject<bool> _isInstallingSubject = new (false);
        private readonly ReplaySubject<string> _installationProgressSubject = new ();

        public IObservable<string> InstallationStatus => _installationStatusSubject.AsObservable();
        public IObservable<bool> IsInstalling => _isInstallingSubject.AsObservable();
        public IObservable<string> InstallationProgress => _installationProgressSubject.AsObservable();
        
        public IObservable<JsonElement?> JsonResponse => _jsonResponseSubject.AsObservable();

        public WebDriverService(IConnectivityService connectivityService, ILogger<WebDriverService> logger)
        { 
            _connectivityService = connectivityService;
            _logger = logger;
            InitObservable();
        }

        public async Task InitWebDriverService()
        {
            await EnsureBrowserInstalledAsync();
            await CreatePlayWrightChromiumAsync();
            await ConfigPageAsync();
        }

        public async Task EnsureBrowserInstalledAsync()
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
                await RunInstallerProcessAsync(scriptPath, browserDir);
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

        private async Task RunInstallerProcessAsync(string scriptPath, string browserDir)
        {
            _logger.LogInformation("Launching PowerShell installer...");

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe", // Hoặc "pwsh"
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" install chromium",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                WorkingDirectory = Path.GetDirectoryName(scriptPath)
            };

            // Truyền biến môi trường trực tiếp cho Process con
            startInfo.EnvironmentVariables["PLAYWRIGHT_BROWSERS_PATH"] = browserDir;

            using var process = new Process { StartInfo = startInfo };

            // Đăng ký nhận Log
            process.OutputDataReceived += (s, e) => ForwardLogToSubject(e.Data);
            process.ErrorDataReceived += (s, e) => ForwardLogToSubject(e.Data, isError: true);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Installer exited with code {process.ExitCode}");
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
        /// Xóa folder cũ an toàn
        /// </summary>
        private void CleanupOldInstallation(string path)
        {
            if (Directory.Exists(path))
            {
                try 
                { 
                    _installationProgressSubject.OnNext("> Đang dọn dẹp dữ liệu cũ...\n");
                    Directory.Delete(path, true); 
                } 
                catch (Exception ex)
                { 
                    // Warning nhẹ, không chặn quy trình
                    _logger.LogWarning($"Không thể xóa folder cũ: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Helper đẩy log vào Subject (tránh code lặp lại)
        /// </summary>
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
                
                // Thử launch trình duyệt (Headless = true để không hiện UI)
                // Nếu file lỗi, dòng này sẽ throw exception ngay
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions 
                { 
                    Headless = true 
                });
                
                // Nếu code chạy đến đây nghĩa là browser ngon
                await browser.CloseAsync();
                return true;
            }
            catch (Exception)
            {
                // Bất cứ lỗi gì (thiếu file, lỗi version, permission) đều trả về false
                return false;
            }
        }

       

        protected async Task CreatePlayWrightChromiumAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            _page = await _browser.NewPageAsync();
        }

        protected virtual async Task ConfigPageAsync()
        {
            //await _page.RouteAsync("**/*.css", async route => await route.AbortAsync());
            await _page.RouteAsync("**/*", async route =>
            {
                var type = route.Request.ResourceType;
                if (type == "stylesheet" || type == "font")
                    await route.AbortAsync();
                else
                    await route.ContinueAsync();
            });

            _page.Dialog += async (sender, e) =>
            {
                if (e.Type == DialogType.Alert)
                {
                    _logger.LogInformation(e.Message);
                    await e.DismissAsync();
                }
                else if (e.Type == DialogType.Confirm)
                {
                    _logger.LogInformation(e.Message);
                    await e.AcceptAsync();
                }
            };
            _page.Response += async (sender, e) =>
            {
                if (e.Status == 200)
                {
                    var contentType = e.Headers["content-type"];
                    try
                    {
                        if (contentType.Contains("application/json"))
                        {
                            var jsonResponse = await e.JsonAsync();
                            _jsonResponseSubject.OnNext(jsonResponse);
                        }
                        else if (contentType.Contains("image"))
                        {
                        }   
                    }
                    catch 
                    {
                        // ignore
                    }
                  
                }
            };
        }

        private void InitObservable()
        {
            _connectivityService.IsInternetAvailable
            .DistinctUntilChanged()
            .Subscribe(status =>
            {
                _isHasInternet = status;
            }).DisposeWith(_disposables);
        }

        ///<summary>
        /// Throw Exeption when no Internet 
        /// </summary>
        /// <exception cref="NoInternetException">
        /// </exception>
        public void EnsureInternetConnection()
        {
            if (!_isHasInternet) throw new NoInternetException("No Internet");
        }

        public string GetPageUrl()
        {
            return _page.Url ?? string.Empty;
        }

        public async Task<bool> TryWaitForUrlAsync(string url,int timeout = 10000)
        {
            try
            {
                await _page.WaitForURLAsync(url, new PageWaitForURLOptions() { Timeout = timeout });
                return true;
            }
            catch
            {
                return false;
            }

        }

        public async Task WaitForTimeoutAsync(float milisecond)
        {
            await _page.WaitForTimeoutAsync(milisecond);
        }

        public async Task GoToPageAsync(string url)
        {
            try
            {
                EnsureInternetConnection();
                await _page.GotoAsync(url, new PageGotoOptions { Timeout = 10000 , WaitUntil = WaitUntilState.NetworkIdle });
                await _page.WaitForLoadStateAsync(LoadState.Load);
            }
            catch
            {
                Debug.WriteLine("Go to page fail! " + url);
                throw;
            }
        }

        public async Task RefreshPageAsync()
        {
            await _page.ReloadAsync(new PageReloadOptions() { WaitUntil = WaitUntilState.Load });
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        public ILocator LocatorElement(string selector)
        {
            try
            {
                return _page.Locator(selector);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Locator element fail! " + selector + " " + ex.Message);
                throw;
            }
        }

        public async Task FillElementAsync(ILocator element, string strValue)
        {
            try
            {
                EnsureInternetConnection();
                await element.FillAsync(strValue);
            } 
            catch
            {
                Debug.WriteLine("Fill element fail! " + element + " " + strValue);
                throw;
            }
        }

        public async Task FillElementAsync(string selector, string strValue)
        {
            try
            {
                ILocator element = this.LocatorElement(selector);
                await FillElementAsync(element, strValue);
            }
            catch
            {
                throw;
            }
        }

        public async Task ClickElementAsync(ILocator element,LocatorClickOptions? options = null)
        {
            try
            {
                EnsureInternetConnection();
                await element.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
                await element.ClickAsync(options);
            }
            catch
            {
                Debug.WriteLine("Click element fail!");
                throw;
            }
        }
        public async Task ClickElementAsync(string selector, LocatorClickOptions? options = null)
        {
            try
            {
                ILocator element = this.LocatorElement(selector);
                await ClickElementAsync(element,options);
            }
            catch
            {
                throw;
            }
        }

        public async Task ClickNavigateElementAsync(ILocator element, LocatorClickOptions? options = null, LoadState loadState = LoadState.NetworkIdle)
        {
            try
            {
                await element.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
                if (!await _connectivityService.CheckInternetAccessAsync())
                    throw new Exception("Can't Navigate because no internet!");

                await Task.WhenAll(
                        _page.WaitForLoadStateAsync(loadState),
                        element.ClickAsync(options)
                    );
                 
            }
            catch
            {
                Debug.WriteLine("Click navigate element fail!");
                throw;
            }
        }


        public async Task ClickNavigateElementAsync(string selector, LocatorClickOptions? options = null, LoadState loadState = LoadState.NetworkIdle)
        {
            try
            {
                ILocator element = this.LocatorElement(selector);
                await ClickNavigateElementAsync(element,options,loadState);
            }
            catch
            {
                throw;
            }
        }

        public async Task<byte[]> GetImageToByteArrayAsync(ILocator element)
        {
            try
            {
                EnsureInternetConnection();
                await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
                return await element.ScreenshotAsync();
            }
            catch
            {
                Debug.WriteLine("Get image fail!");
                return Array.Empty<byte>();
            }
        }
        public async Task<byte[]> GetImageToByteArrayAsync(string selector)
        {
            try
            {
                EnsureInternetConnection();
                ILocator element = this.LocatorElement(selector);

                await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
                return await element.ScreenshotAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e,"Get image fail!");
                return Array.Empty<byte>();
            }
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
        public async ValueTask DisposeAsync()
        {
            if (_page is not null)
                await _page.CloseAsync();

            if (_browser is not null)
                await _browser.DisposeAsync();

            _playwright?.Dispose();
            _disposables?.Dispose();
        }
    }
}
