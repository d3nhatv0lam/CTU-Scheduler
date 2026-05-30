using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Helpers;
using CTUScheduler.AppServices.Services.CtuSessions;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.AppServices.Services.UserSettingService;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Core.Models.TeachingPlan;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using CTUScheduler.Presentation.Shells.MainShell.ViewModels;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Authentication.ViewModels
{
    public partial class LoginViewModel : ViewModelBase, IDisposable, IRoutableViewModel, IActivatableViewModel
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly IUserInteractionService _userInteractionService;
        private readonly INavigationRegionManager _navigationRegionManager;
        private readonly IUserSettingService _userSettingService;
        private readonly ILogger<LoginViewModel> _logger;

        private string _userName = string.Empty;
        private string _password = string.Empty;
        private bool _isSaveUsername;
        [ObservableAsProperty] private bool _isLoadedTeachingPlan;

        public string UrlPathSegment => nameof(LoginViewModel);
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

        public ReactiveCommand<Unit, OperationResult<TeachingPlanData>> LoadTeachingPlanCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenTeachingPlanCommand { get; }
        public ReactiveCommand<Unit, Unit> SignInCommand { get; }


        public LoginViewModel(IScreen hostScreen,
            IUserInteractionService userInteractionService,
            INavigationRegionManager navigationRegionManager,
            IUserSettingService userSettingService,
            ITeachingPlanLoaderService teachingPlanLoaderService,
            ITeachingPlanPdfService pdfService,
            ITeachingPlanStore teachingPlanStore,
            ISessionCoordinator sessionCoordinator,
            IConnectivityService connectivityService,
            ILogger<LoginViewModel> logger)
        {
            HostScreen = hostScreen;
            _userInteractionService = userInteractionService;
            _navigationRegionManager = navigationRegionManager;
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

            var canSignin = connectivityService.IsInternetAvailable
                .ObserveOn(RxSchedulers.MainThreadScheduler);

            SignInCommand = ReactiveCommand.CreateFromTask(async ct =>
                {
                    var result = await sessionCoordinator.StartSessionAsync(UserName, Password, ct);

                    result.Match(OnLoggedIn
                        , (errors, _) =>
                        {
                            var errorTexts = errors.Select(e => e.FormattedMessage);
                            _userInteractionService.Notification.Light.Error($"{string.Join('\n', errorTexts)}");
                        });
                }, canSignin)
                .DisposeWith(_disposables);

            LoadTeachingPlanCommand = ReactiveCommand.CreateFromTask(teachingPlanLoaderService.LoadLatestAsync)
                .DisposeWith(_disposables);

            LoadTeachingPlanCommand
                .Where(x => x.IsSuccess)
                .Select(x => x.Content!)
                .Subscribe(teachingPlanStore.Update)
                .DisposeWith(_disposables);

            LoadTeachingPlanCommand.WhenFailed()
                .Subscribe(_ =>
                {
                    var options = new NotificationOptions()
                    {
                        ShowIcon = true,
                        Expiration = TimeSpan.FromSeconds(30),
                    };
                    _userInteractionService.Notification.Light.Error(
                        title: "Không tại được kế hoạch giảng dạy!",
                        content: "Bạn nên bật vpn và khởi động lại!",
                        options: options);
                })
                .DisposeWith(_disposables);

            _isLoadedTeachingPlanHelper = LoadTeachingPlanCommand
                .Select(x => x.IsSuccess)
                .ToProperty(this, nameof(IsLoadedTeachingPlan), scheduler: RxSchedulers.MainThreadScheduler)
                .DisposeWith(_disposables);

            var canOpenTeachingPlan = this.WhenAnyValue(x => x.IsLoadedTeachingPlan);
            OpenTeachingPlanCommand = ReactiveCommand.Create(() =>
                    {
                        var pdfUrl = teachingPlanStore.Current?.PdfUrl;
                        if (string.IsNullOrEmpty(pdfUrl))
                            return;

                        var cachedPath = pdfService.GetCachedPdfPath(pdfUrl);
                        ProcessHelper.OpenUrl(File.Exists(cachedPath) ? cachedPath : pdfUrl);
                    },
                    canOpenTeachingPlan)
                .DisposeWith(_disposables);

            this.WhenActivated(disposable =>
            {
                LoadTeachingPlanCommand.Execute()
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
            _logger.LogDebug("Disposed");
        }
    }
}