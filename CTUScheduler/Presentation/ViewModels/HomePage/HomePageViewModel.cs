using CTUScheduler.AppServices;
using CTUScheduler.AppServices.Services.Implementations;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Presentation.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Microsoft.VisualBasic;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.HomePage
{
    public class HomePageViewModel : ViewModelBase, IRoutableViewModel
    {
        private IWebDriverService _webDriverService;
        public string? UrlPathSegment => "HomeViewModel";

        public IScreen HostScreen { get; }

        public HomePageViewModel()
        {
        }

        public HomePageViewModel(IScreen hostScreen)
        {
            _webDriverService = App.ServiceProvider!.GetRequiredService<IWebDriverService>();
            HostScreen = hostScreen;

            LoadPage();
        }

        private async void LoadPage()
        {
            string currentURL = _webDriverService.GetPageUrl();
            if (currentURL.Contains(AppConstants.CTU_DKMH_URL_KEY))
            {
                try
                {
                    ILocator navigateElement = _webDriverService.LocatorElement(AppConstants.CTU_DKMH_QUYDINHDANGKY_BUTTON);
                    await _webDriverService.ClickNavigateElement(navigateElement);
                }
                catch
                {
                    Debug.WriteLine("exeption roi!");
                }
            }
            else
            {
                if (currentURL.Contains(AppConstants.CTU_HOME_URL))
                {
                    try
                    {
                        ILocator navigateElement = _webDriverService.LocatorElement(AppConstants.CTU_HOME_DKMH_BUTTON);
                        await _webDriverService.ClickNavigateElement(navigateElement);
                    }
                    catch
                    {
                        Debug.WriteLine("exeption roi!");
                    }
                }
                else
                {
                    Debug.WriteLine("url dang o dau zayyyyyy " + currentURL);
                }
            }
            
        }
    }
}
