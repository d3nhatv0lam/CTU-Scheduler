using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.AppServices;
using CTUScheduler.AppServices.Helpers;
using CTUScheduler.AppServices.Services.Registration;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using CTUScheduler.Presentation.Base;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;

namespace CTUScheduler.Presentation.Features.Home.ViewModels
{
    public class HomeViewModel : ViewModelBase, IRoutableViewModel, IActivatableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        // private readonly IRegistrationInformationService _registrationInformationService;
        private readonly IAppState _appState;
        private readonly IRegistrationRulesService _registrationRulesService;
        private readonly ObservableAsPropertyHelper<RegistrationInformation> _registrationInfor;
        
        private readonly ICtuSitePageFactory _ctuSitePageFactory;
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
            _appState = App.ServiceProvider.GetRequiredService<IAppState>();
            _registrationRulesService = App.ServiceProvider.GetRequiredService<IRegistrationRulesService>();
            // _registrationInformationService = App.ServiceProvider.GetRequiredService<IRegistrationInformationService>();
            HostScreen = hostScreen;

            
            // _registrationInfor = _registrationRulesService.RegistrationInfoChanges
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .ToProperty(this, nameof(RegistrationInfo));
            _registrationInfor = _appState.RegistrationInfo
                .Where(x => x is not null)
                .Select(x => x!)
                .ToProperty(this, nameof(RegistrationInfo), scheduler: RxApp.MainThreadScheduler);

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
                await _registrationRulesService.NavigateToAsync();
                // await _ctuSitePageFactory.RegistrationRulesPage.NavigateToAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);    
            }
        }

        public void Dispose()
        {
            (_registrationRulesService as IDisposable)?.Dispose();
            _disposable.Dispose();
            Log.Information("HomeViewModel: Disposed");
        }
    }
}
