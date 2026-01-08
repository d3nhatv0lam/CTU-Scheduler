using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

using CTUScheduler.AppServices.Helpers;
using CTUScheduler.AppServices.Services.Registration;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Presentation.Base;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;

namespace CTUScheduler.Presentation.Features.Home.ViewModels
{
    public class HomeViewModel : ViewModelBase, IRoutableViewModel, IActivatableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        private readonly IUserSessionService _userSessionService;
        private readonly IRegistrationRulesService _registrationRulesService;
        private readonly ObservableAsPropertyHelper<RegistrationInformation> _registrationInfo;
        public string? UrlPathSegment => "HomeViewModel";
        
        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        public RegistrationInformation RegistrationInfo => _registrationInfo.Value;
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
            _userSessionService = App.ServiceProvider.GetRequiredService<IUserSessionService>();
            _registrationRulesService = App.ServiceProvider.GetRequiredService<IRegistrationRulesService>();
            
            _registrationRulesService.StartSync();
            HostScreen = hostScreen;

            
            _registrationInfo = _userSessionService.RegistrationInfo
                .Where(x => x is not null)
                .Select(x => x!)
                .ToProperty(this, nameof(RegistrationInfo), scheduler: RxApp.MainThreadScheduler);

            var ownerContributor = AppConstants.AppCredits.AllContributors[0];
            OpenFacebookCommand = ReactiveCommand.Create(() => OpenUrl(ownerContributor.SocialLinks[SocialPlatform.Facebook])).DisposeWith(_disposable);
            OpenYoutubeCommand = ReactiveCommand.Create(() => OpenUrl(ownerContributor.SocialLinks[SocialPlatform.YouTube])).DisposeWith(_disposable);
            OpenGithubCommand = ReactiveCommand.Create(() => OpenUrl(ownerContributor.SocialLinks[SocialPlatform.GitHub])).DisposeWith(_disposable);
            OpenCTUHTQLCommand = ReactiveCommand.Create(() => OpenUrl(AppConstants.Urls.CtuSignIn)).DisposeWith(_disposable);

            this.WhenActivated((CompositeDisposable disposable) =>
            {
                disposable.Add(_registrationInfo);
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);    
            }
        }

        public void Dispose()
        {
            _registrationRulesService.StopSync();
            (_registrationRulesService as IDisposable)?.Dispose();
            _disposable.Dispose();
            Log.Information("HomeViewModel: Disposed");
        }
    }
}
