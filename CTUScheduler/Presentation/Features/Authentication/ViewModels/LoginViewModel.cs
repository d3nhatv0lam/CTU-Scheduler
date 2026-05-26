using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
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
        private readonly ILoginService _loginService;
        private readonly IUserInteractionService _userInteractionService;
        private readonly INavigationRegionManager _navigationRegionManager;
        private readonly IUserSettingService _userSettingService;
        private readonly ITeachingPlanPdfService _pdfService;
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

        public ReactiveCommand<Unit, Unit> PrewarmBrowserCommand { get; }
        public ReactiveCommand<Unit, OperationResult<TeachingPlanData>> LoadTeachingPlanCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenTeachingPlanCommand { get; }

        public ReactiveCommand<Unit, Unit> SignInCommand { get; }

        public ReactiveCommand<Unit, Unit> TestCommand { get; }


        public LoginViewModel(IScreen hostScreen,
            ILoginService loginService,
            IUserInteractionService userInteractionService,
            INavigationRegionManager navigationRegionManager,
            IUserSettingService userSettingService,
            ITeachingPlanLoaderService teachingPlanLoaderService,
            ITeachingPlanPdfService pdfService,
            ITeachingPlanStore teachingPlanStore,
            IAuthClient authClient,
            ICtuSessionStore sessionStore,
            ILogger<LoginViewModel> logger)
        {
            HostScreen = hostScreen;
            _userInteractionService = userInteractionService;
            _navigationRegionManager = navigationRegionManager;
            _loginService = loginService;
            _userSettingService = userSettingService;
            _pdfService = pdfService;
            _logger = logger;

            TestCommand = ReactiveCommand.CreateFromTask(async ct =>
                {
                    var sessionAuth = await authClient.AuthenticateAsync(UserName, Password, ct);

                    sessionStore.Update(sessionAuth);

                    try
                    {
                        var session = sessionStore.CurrentSession;
                        if (session != null)
                        {
                            Debug.WriteLine("====================================================");
                            Debug.WriteLine("=== BẮT ĐẦU LIVE TEST LUỒNG CỨU PHIÊN THỰC TẾ ===");
                            Debug.WriteLine($"[1] Đăng nhập thành công! Sinh viên: {session.StudentName}");
                            Debug.WriteLine($"[2] PHPSESSID gốc: {session.LegacyWebCookies["PHPSESSID"]}");
                            Debug.WriteLine($"[3] SESSISID (SSO): {session.LegacyWebCookies["SESSISID"]}");
                            // MÔ PHỎNG SỰ CỐ: Xóa PHPSESSID khỏi RAM để giả lập hết hạn 20 phút

                            var simulatedCookies = new Dictionary<string, string>(session.LegacyWebCookies);
                            simulatedCookies.Remove("PHPSESSID"); // Chỉ giữ lại SESSISID
                            var simulatedSession = session with { LegacyWebCookies = simulatedCookies };
                            sessionStore.Update(simulatedSession);
                            Debug.WriteLine("\n[4] Đã xóa PHPSESSID khỏi Store. Đang chạy TrySilentReAuthAsync...");
                            // Bắt đầu gọi hàm cứu phiên ngầm
                            var refreshedSession = await authClient.TrySilentReAuthAsync(simulatedSession, ct);
                            if (refreshedSession != null)
                            {
                                Debug.WriteLine("\n🎉 🎉 🎉 CỨU PHIÊN THÀNH CÔNG 🎉 🎉 🎉");
                                Debug.WriteLine(
                                    $"[5] PHPSESSID mới nhận được từ trường: {refreshedSession.LegacyWebCookies["PHPSESSID"]}");

                                // Cập nhật lại phiên đã được cứu vào hệ thống để dùng tiếp
                                sessionStore.Update(refreshedSession);
                            }
                            else
                            {
                                Debug.WriteLine("\n❌ THẤT BẠI: Cổng trường không cấp lại PHPSESSID!");
                            }

                            Debug.WriteLine("====================================================");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"LỖI LIVE TEST: {ex.Message}");
                    }
                })
                .DisposeWith(_disposables);

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
                .ObserveOn(RxSchedulers.MainThreadScheduler);

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
                        var pdfUrl = teachingPlanStore.CurrentTeachingPlan?.PdfUrl;
                        if (string.IsNullOrEmpty(pdfUrl))
                            return;

                        var cachedPath = _pdfService.GetCachedPdfPath(pdfUrl);
                        ProcessHelper.OpenUrl(File.Exists(cachedPath) ? cachedPath : pdfUrl);
                    },
                    canOpenTeachingPlan)
                .DisposeWith(_disposables);

            this.WhenActivated(disposable =>
            {
                PrewarmBrowserCommand.Execute()
                    .Subscribe()
                    .DisposeWith(disposable);

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