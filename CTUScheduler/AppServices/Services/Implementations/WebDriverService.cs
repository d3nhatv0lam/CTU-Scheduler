using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Core.Exceptions;
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
    public class WebDriverService : IWebDriverService, IDisposable , IAsyncDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IInternetStatusService _internetStatusService;
        private IPlaywright _playwright = null!;
        private IBrowser _browser = null!;
        private IPage _page = null!;
        private Subject<JsonElement?> _jsonResponse = new Subject<JsonElement?>();
        protected bool _isHasInternet;

        public Subject<JsonElement?> JsonResponse => _jsonResponse;

        public event EventHandler? AlertBoxOpened;
        public event EventHandler? ConfirmBoxOpened;

        protected virtual void OnAlertBoxOpened()
        {
            AlertBoxOpened?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnConfimBoxOpened()
        {
            ConfirmBoxOpened?.Invoke(this, EventArgs.Empty);
        }

        public WebDriverService(IInternetStatusService internetStatusService)
        { 
            _internetStatusService = internetStatusService;

            //_playwright =  Playwright.CreateAsync().GetAwaiter().GetResult();
            //_browser =  _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions() { Headless = false }).GetAwaiter().GetResult();
            //_page =  _browser.NewPageAsync().GetAwaiter().GetResult();
            CreatePlayWrightChromiumAsync().GetAwaiter().GetResult();

            ConfigPageAsync().GetAwaiter().GetResult();

            InitObservable();
        }

        protected async Task CreatePlayWrightChromiumAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions() { Headless = false });
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
                            var jsonResponse = await e.JsonAsync();
                            JsonResponse.OnNext(jsonResponse);
                        }
                        else if (contentType.Contains("image"))
                        {
                        }
                    }
                    catch
                    {

                    }
                }
            };
        }

        private void InitObservable()
        {
            _internetStatusService.InternetStatusOnRefresh
            .DistinctUntilChanged()
            .Subscribe(status =>
            {
                _isHasInternet = status;
            }).DisposeWith(_disposables);

            //Observable.FromEventPattern<IResponse>(_page, nameof(_page.Response))
            //.Subscribe(async e =>
            //{
            //    string strResponse = await e.EventArgs.TextAsync();
            //}).DisposeWith(_disposables);
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
            return _page.Url;
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

        public async Task ClickElementAsync(ILocator element)
        {
            try
            {
                EnsureInternetConnection();
                await element.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
                await element.ClickAsync();
            }
            catch
            {
                Debug.WriteLine("Click element fail!");
                throw;
            }
        }
        public async Task ClickElementAsync(string selector)
        {
            try
            {
                ILocator element = this.LocatorElement(selector);
                await ClickElementAsync(element);
            }
            catch
            {
                throw;
            }
        }

        public async Task ClickNavigateElementAsync(ILocator element, LoadState loadState = LoadState.Load)
        {
            try
            {
                await element.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
                if (!await _internetStatusService.GetInternetStatus())
                    throw new Exception("Can't Navigate because no internet!");

                await element.ClickAsync();
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            catch
            {
                Debug.WriteLine("Click navigate element fail!");
                throw;
            }
        }
        public async Task ClickNavigateElementAsync(string selector, LoadState loadState = LoadState.Load)
        {
            try
            {
                ILocator element = this.LocatorElement(selector);
                await ClickNavigateElementAsync(element);
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
            catch
            {
                Debug.WriteLine("Get image fail!");
                return Array.Empty<byte>();
            }
        }

        public void Dispose()
        {
            _page.CloseAsync().GetAwaiter().GetResult();
            _browser.DisposeAsync().GetAwaiter().GetResult();
            _playwright.Dispose();
            _disposables.Dispose();
        }

        public async ValueTask DisposeAsync()
        {

        }
    }
}
