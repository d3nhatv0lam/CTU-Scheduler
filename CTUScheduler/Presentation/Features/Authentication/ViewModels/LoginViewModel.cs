using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.UserSettingService;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using CTUScheduler.Presentation.Shells.MainShell.ViewModels;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Authentication.ViewModels
{
    public class LoginViewModel : ViewModelBase, IDisposable, IRoutableViewModel, IActivatableViewModel
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ILoginService _loginService;
        private readonly IUserInteractionService _userInteractionService;
        private readonly INavigationRegionManager _navigationRegionManager;
        private readonly IUserSettingService _userSettingService;
        private readonly ILogger<LoginViewModel> _logger;

        private string _userName = string.Empty;
        private string _password = string.Empty;
        private bool _isSaveUsername;

        public string? UrlPathSegment => nameof(LoginViewModel);
        public IScreen HostScreen { get; }
        public ViewModelActivator Activator { get; } = new();

        public string UserName
        {
            get => _userName;
            set => this.RaiseAndSetIfChanged(ref _userName, value);
        }

        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        public bool IsSaveUsername
        {
            get => _isSaveUsername;
            set => this.RaiseAndSetIfChanged(ref _isSaveUsername, value);
        }

        public ReactiveCommand<Unit, Unit> PrewarmBrowserCommand { get; }
        public ReactiveCommand<Unit, Unit> SignInCommand { get; }

        public LoginViewModel(IScreen hostScreen,
            ILoginService loginService,
            IUserInteractionService userInteractionService,
            INavigationRegionManager navigationRegionManager,
            IUserSettingService userSettingService,
            ILogger<LoginViewModel> logger)
        {
            HostScreen = hostScreen;
            _userInteractionService = userInteractionService;
            _navigationRegionManager = navigationRegionManager;
            _loginService = loginService;
            _userSettingService = userSettingService;
            _logger = logger;

            _userSettingService.AuthSettingsChanged
                .Subscribe(authSettings =>
                {
                    IsSaveUsername = authSettings.IsSaveUsername;
                    if (IsSaveUsername && !string.IsNullOrEmpty(authSettings.SavedUserName))
                        UserName = authSettings.SavedUserName;
                })
                .DisposeWith(_disposables);

            PrewarmBrowserCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await _loginService.EnsureReadyAsync();
            }).DisposeWith(_disposables);


            var canSignIn = PrewarmBrowserCommand.IsExecuting
                .Select(isPrewarming => !isPrewarming)
                .ObserveOn(RxApp.MainThreadScheduler);

            SignInCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    var result = await loginService.LoginAsync(UserName, Password);

                    result.Match(OnLoggedIn
                        , (errors, _) =>
                        {
                            var errorTexts = errors.Select(e => e.FormattedMessage);
                            _userInteractionService.Notification.Light.Error($"{string.Join('\n', errorTexts)}");
                        });
                }, canSignIn)
                .DisposeWith(_disposables);

            this.WhenActivated((CompositeDisposable disposable) =>
            {
                PrewarmBrowserCommand.Execute()
                    .Subscribe()
                    .DisposeWith(disposable);
            });
        }


        private void OnLoggedIn()
        {
            _userInteractionService.Toast.Light.Success("Đăng nhập thành công!");
            _userSettingService.UpdateSettings(preferences => preferences with
            {
                Auth = new AuthSettings { IsSaveUsername = IsSaveUsername, SavedUserName = UserName }
            });
            Password = string.Empty;
            _navigationRegionManager.NavigateAndResetTo<MainShellViewModel>(RegionIds.Root);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _logger.LogDebug("{this}: Disposed", nameof(LoginViewModel));
        }
    }
}