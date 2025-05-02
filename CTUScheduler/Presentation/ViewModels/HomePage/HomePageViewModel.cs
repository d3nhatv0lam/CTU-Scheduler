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
        private ICTUWebDriverService _CTUWebDriverService;
        public string? UrlPathSegment => "HomeViewModel";

        public IScreen HostScreen { get; }

        public HomePageViewModel()
        {
        }

        public HomePageViewModel(IScreen hostScreen)
        {
            _CTUWebDriverService = App.ServiceProvider!.GetRequiredService<ICTUWebDriverService>();
            HostScreen = hostScreen;

            LoadPage();
        }

        private async void LoadPage()
        {
            await _CTUWebDriverService.GoToRegistrationRulesPage();
        }
    }
}
