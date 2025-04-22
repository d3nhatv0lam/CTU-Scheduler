using Microsoft.Playwright;
using Splat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Implementations
{
    public class WebDriverService : IDisposable
    {
        private readonly InternetStatusService _internetStatusService;
        private bool _isHasInternet;
        private IPlaywright _playwright { get; set; }
        private IBrowser _browser { get; set; }
        private IPage _page { get; set; }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public Subject<JsonElement?> JsonResponse = new Subject<JsonElement?>();

        private event EventHandler? _alertBoxOpened;

        public event EventHandler AlertBoxOpened
        {
            add
            {
                _alertBoxOpened += value;
            }
            remove
            {
                _alertBoxOpened -= value;
            }
        }

        private event EventHandler? _confirmBoxOpened;

        public event EventHandler ConfirmBoxOpened
        {
            add
            {
                _confirmBoxOpened += value;
            }
            remove
            {
                _confirmBoxOpened -= value;
            }
        }

        private void OnAlertBoxOpened()
        {
            _alertBoxOpened?.Invoke(this, EventArgs.Empty);
        }

        private void OnConfimBoxOpened()
        {
            _confirmBoxOpened?.Invoke(this, EventArgs.Empty);
        }

        //private WebDriverService()
        //{
        //    _playwright = Playwright.CreateAsync().GetAwaiter().GetResult();
        //    _browser = _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions() { Headless = false }).GetAwaiter().GetResult();
        //    _page = _browser.NewPageAsync().GetAwaiter().GetResult();
        //    ConfigPage();
        //}

        public WebDriverService(InternetStatusService internetStatusService)
        { 
            _internetStatusService = internetStatusService;

            _playwright =  Playwright.CreateAsync().GetAwaiter().GetResult();
            _browser =  _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions() { Headless = true }).GetAwaiter().GetResult();
            _page =  _browser.NewPageAsync().GetAwaiter().GetResult();

            ConfigPage();

            _internetStatusService.InternetStatusOnRefesh
                .DistinctUntilChanged()
                .Subscribe(status =>
                {
                    _isHasInternet = status;
                }).DisposeWith(_disposables);
        }

        //private async Task InitializeAsync()
        //{
        //    _playwright = await Playwright.CreateAsync();
        //    _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions() { Headless = false });
        //    _page = await _browser.NewPageAsync();
        //    ConfigPage();
        //}


        //public static async Task<WebDriverService> WebDriverServiceFactory(InternetStatusService internetStatusService)
        //{
        //    WebDriverService webDriverService = new WebDriverService(internetStatusService);
        //    await webDriverService.InitializeAsync();
        //    return webDriverService;
        //}


        private async void ConfigPage()
        {
            await _page.RouteAsync("**/*.css", async route => await route.AbortAsync());
            _isHasInternet = await _internetStatusService.CheckInternetStatus();
            _page.Dialog += async (sender, e) =>
            {
                if (e.Type == DialogType.Alert)
                {
                    Debug.WriteLine("Alert: " + e.Message);
                    OnAlertBoxOpened();
                    await e.DismissAsync();
                }
                else if (e.Type == DialogType.Confirm)
                {
                    Debug.WriteLine("Confirm: " + e.Message);
                    OnConfimBoxOpened();
                    await e.AcceptAsync();
                }
            };
            _page.Response += async (sender, e) =>
            {
                if (e.Status == 200)
                {       
                    try
                    {
                        var contentType = e.Headers["content-type"];
                        if (contentType.Contains("application/json"))
                        {
                            Debug.WriteLine("Response là JSON");
                            var jsonResponse = await e.JsonAsync();
                            JsonResponse.OnNext(jsonResponse);
                        }
                        else if (contentType.Contains("image"))
                        {
                            Debug.WriteLine("Response là hình ảnh");
                        }
                    }
                    catch
                    {

                    }
                }
            };
            _page.RequestFailed += (sender, e) =>
            {
                Debug.WriteLine("Request failed: " + e.Url);
            };
        }

        ///<summary>
        /// Throw Exeption when no Internet 
        /// </summary>
        private bool IsHasInternet()
        {
            if (!_isHasInternet) throw new Exception("No Internet");
            return _isHasInternet;
        }

        public async Task GoToPage(string strLink)
        {
            try
            {
                IsHasInternet();
                await _page.GotoAsync(strLink, new PageGotoOptions { Timeout = 10000 });
            }
            catch
            {
                Debug.WriteLine("Go to page fail! " + strLink);
                throw;
            }
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

        public async Task FillElement(ILocator element, string strValue)
        {
            try
            {
                IsHasInternet();
                await element.FillAsync(strValue);
            } 
            catch
            {
                Debug.WriteLine("Fill element fail! " + element + " " + strValue);
                throw;
            }
        }

        public async Task ClickElement(ILocator element)
        {
            try
            {
                IsHasInternet();
                await element.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
                await element.ClickAsync();
            }
            catch
            {
                Debug.WriteLine("Click element fail!");
                throw;
            }
        }

        public async Task ClickNavigateElement(ILocator element)
        {
            try
            {
                await element.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
                if (!await _internetStatusService.CheckInternetStatus())
                    throw new Exception("Can't Navigate because no internet!");

                await element.ClickAsync();
            }
            catch
            {
                Debug.WriteLine("Click navigate element fail!");
                throw;
            }
        }

        public async Task<byte[]> GetImageToByteArray(ILocator element)
        {
            if (!IsHasInternet())
                return Array.Empty<byte>();
            try
            {
                await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
                return await element.ScreenshotAsync();
            }
            catch
            {
                Debug.WriteLine("Get image fail!");
                throw;
            }
        }

        public void Dispose()
        {
            _page.CloseAsync().GetAwaiter().GetResult();
            _browser.DisposeAsync().GetAwaiter().GetResult();
            _playwright.Dispose();
            _disposables.Dispose();
        }
    }
}
