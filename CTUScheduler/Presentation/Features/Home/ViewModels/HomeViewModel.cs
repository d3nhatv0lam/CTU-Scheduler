using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Helpers;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Core.Models.Contributors;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Shared.Models.Regions;
using ReactiveUI;
using Serilog;

namespace CTUScheduler.Presentation.Features.Home.ViewModels
{
    public class HomeViewModel : ViewModelBase, IRoutableViewModel, IActivatableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        private readonly IUserSessionService _userSessionService;
        private readonly IUserInteractionService _userInteractionService;
        private readonly IRegistrationRulesService _registrationRulesService;
        private readonly ObservableAsPropertyHelper<RegistrationInformation> _registrationInfo;
        public string? UrlPathSegment => "HomeViewModel";
        public IScreen HostScreen { get; }
        public ViewModelActivator Activator { get; } = new();
        public RegistrationInformation RegistrationInfo => _registrationInfo.Value;

        public ReactiveCommand<Unit, Unit> OpenFacebookCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenYoutubeCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenGithubCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenCTUHTQLCommand { get; }

        public HomeViewModel(IScreen hostScreen,
            IUserSessionService userSessionService,
            IRegistrationRulesService registrationRulesService,
            IUserInteractionService userInteractionService,
            INavigationRegionManager navigationRegionManager)
        {
            HostScreen = hostScreen;
            _userSessionService = userSessionService;
            _registrationRulesService = registrationRulesService;
            _userInteractionService = userInteractionService;

            _registrationRulesService.RegistrationInfoChanged
                .Subscribe(info => _userSessionService.UpdateServerInfo(info))
                .DisposeWith(_disposable);

            Observable.StartAsync(async _ => await _registrationRulesService.EnsureReadyAsync())
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(result =>
                {
                    result.Match(
                        () => {  },
                        (errors, _) =>
                        {
                            var errorsString = String.Join('\n', errors.Select(x => x.FormattedMessage));
                            _userInteractionService.Notification.Light.Error(errorsString);
                            navigationRegionManager.NavigateAndResetTo<LoginViewModel>(RegionIds.Root);
                        },
                        ex => { Debug.WriteLine(ex, "Lỗi khi _registrationRulesService.EnsureReadyAsync"); }
                    );
                })
                .DisposeWith(_disposable);

            _registrationInfo = _userSessionService.RegistrationInfoChanged
                .Where(x => x is not null)
                .Select(x => x!)
                .ToProperty(this, nameof(RegistrationInfo), scheduler: RxApp.MainThreadScheduler);

            var ownerContributor = AppConstants.AppCredits.AllContributors[0];
            OpenFacebookCommand = ReactiveCommand
                .Create(() => OpenUrl(ownerContributor.SocialLinks[SocialPlatform.Facebook])).DisposeWith(_disposable);
            OpenYoutubeCommand = ReactiveCommand
                .Create(() => OpenUrl(ownerContributor.SocialLinks[SocialPlatform.YouTube])).DisposeWith(_disposable);
            OpenGithubCommand = ReactiveCommand
                .Create(() => OpenUrl(ownerContributor.SocialLinks[SocialPlatform.GitHub])).DisposeWith(_disposable);
            OpenCTUHTQLCommand = ReactiveCommand.Create(() => OpenUrl(AppConstants.Urls.CtuSignIn))
                .DisposeWith(_disposable);

            this.WhenActivated((CompositeDisposable disposable) =>
            {
                disposable.Add(_registrationInfo);
                disposable.Add(_disposable);
            });

            // LoadPage();
        }

        private void OpenUrl(string url)
        {
            ProcessHelper.OpenUrl(url);
        }

        private async void LoadPage()
        {
            try
            {
                await _registrationRulesService.EnsureReadyAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Dispose()
        {
            _disposable.Dispose();
            Log.Debug(nameof(HomeViewModel) + ": Disposed");
        }
    }
}