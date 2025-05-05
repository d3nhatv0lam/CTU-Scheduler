using CTUScheduler.AppServices;
using CTUScheduler.AppServices.Services.Implementations;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.HomePage
{
    public class HomePageViewModel : ViewModelBase, IRoutableViewModel, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ICTUWebDriverService _CTUWebDriverService;
        private readonly ObservableAsPropertyHelper<RegistrationInformation> _registrationInfor;
        public string? UrlPathSegment => "HomeViewModel";
        
        public RegistrationInformation RegistrationInfo => _registrationInfor.Value;

        public IScreen HostScreen { get; }

        

        public HomePageViewModel()
        {
        }

        public HomePageViewModel(IScreen hostScreen)
        {
            _CTUWebDriverService = App.ServiceProvider!.GetRequiredService<ICTUWebDriverService>();
            HostScreen = hostScreen;

            _CTUWebDriverService.RegistrationInformationResponse
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this,x => x.RegistrationInfo, out _registrationInfor);


            LoadPage();
        }

        private async void LoadPage()
        {
            try
            {
                await _CTUWebDriverService.GoToRegistrationRulesPage();
            }
            catch
            {

            }
        }
    }
}
