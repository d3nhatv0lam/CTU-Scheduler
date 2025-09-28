using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.AppServices;
using CTUScheduler.AppServices.Helpers;
using CTUScheduler.AppServices.Services.RegistrationInfor;
using CTUScheduler.AppServices.Services.WebDriver;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Presentation.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Serilog;

namespace CTUScheduler.Presentation.Features.Home.ViewModels
{
    public class HomeViewModel : ViewModelBase, IRoutableViewModel, IActivatableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        private readonly ICTUWebDriverService _CTUWebDriverService;
        private readonly IRegistrationInformationService _registrationInformationService;
        private readonly ObservableAsPropertyHelper<RegistrationInformation> _registrationInfor;
        public string? UrlPathSegment => "HomeViewModel";
        
        public RegistrationInformation RegistrationInfo => _registrationInfor.Value;
        public IScreen HostScreen { get; }

        public ReactiveCommand<Unit,Unit> OpenFacebookCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenYoutubeCommand { get; }
        public ReactiveCommand<Unit,Unit> OpenGithubCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenCTUHTQLCommand { get; }

        public HomeViewModel()
        {
        }

        public HomeViewModel(IScreen hostScreen)
        {
            _CTUWebDriverService = App.ServiceProvider.GetRequiredService<ICTUWebDriverService>();
            _registrationInformationService = App.ServiceProvider.GetRequiredService<IRegistrationInformationService>();
            
            HostScreen = hostScreen;

            _registrationInfor = _registrationInformationService.RegistrationInformationResponse
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, nameof(RegistrationInfo));

            OpenFacebookCommand = ReactiveCommand.Create(() => OpenUrl(AppConstants.FACEBOOK_URL)).DisposeWith(_disposable);
            OpenYoutubeCommand = ReactiveCommand.Create(() => OpenUrl(AppConstants.YOUTUBE_URL)).DisposeWith(_disposable);
            OpenGithubCommand = ReactiveCommand.Create(() => OpenUrl(AppConstants.GITHUB_URL)).DisposeWith(_disposable);
            OpenCTUHTQLCommand = ReactiveCommand.Create(() => OpenUrl(AppConstants.CTU_SIGN_IN_URL)).DisposeWith(_disposable);

            this.WhenActivated((CompositeDisposable disposable) =>
            {
                disposable.Add(_registrationInfor);
                disposable.Add(_disposable);
            });

            LoadPage();
        }

        private void OpenUrl(string url)
        {
            ProcessHelper.OpenUrl(url);
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

        public void Dispose()
        {
            _disposable.Dispose();
            Log.Information("HomeViewModel: Disposed");
        }
    }
}
