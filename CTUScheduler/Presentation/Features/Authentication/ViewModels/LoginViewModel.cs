using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Services.Auth;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.UserInteractionService.Abstractions.Interfaces;
using CTUScheduler.Presentation.Shared.Models.Regions;
using CTUScheduler.Presentation.Shells.MainShell.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Authentication.ViewModels
{
    public class LoginViewModel : ViewModelBase, IDisposable, IRoutableViewModel, INeedArgs<IScreen>
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IUserInteractionService _userInteractionService;
        private readonly INavigationRegionManager _navigationRegionManager;
       
        private string _userName = string.Empty;
        private string _password = string.Empty;
        private bool _isSaveUsername;
        
        public string? UrlPathSegment => "LoginViewModel";
        public IScreen HostScreen { get; }

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

        public ReactiveCommand<Unit, Unit> SignInCommand { get; private set; }

#pragma warning disable CS8618
        public LoginViewModel(IScreen hostScreen, IUserInteractionService userInteractionService, INavigationRegionManager navigationRegionManager)
        {
            HostScreen = hostScreen;
            _userInteractionService = userInteractionService;
            _navigationRegionManager = navigationRegionManager;

            var loginService = App.ServiceProvider.GetRequiredService<ILoginService>();
            LoadSignInData();

            Observable.StartAsync(() => loginService.EnsureReadyAsync());
            
            SignInCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await loginService.LoginAsync(UserName, Password);
                
                result.Match(() =>
                    {
                        _userInteractionService.Notification.Light.Success("Đăng nhập thành công");
                        OnLoggedIn();
                    }
                    , (errors, reason) =>
                    {
                        var errorTexts = errors.Select(e => e.FormattedMessage);
                        _userInteractionService.Notification.Light.Error($"{string.Join('\n',errorTexts)}");
                    });
            })
            .DisposeWith(_disposables);
        }
#pragma warning restore CS8618
        

        private void OnLoggedIn()
        {
            SaveSignInData();
            Password = string.Empty;
            RxApp.MainThreadScheduler.Schedule(NavigateToHome);
            Dispose();
        }
        
        private void NavigateToHome()
        {
            _navigationRegionManager.NavigateAndResetTo<MainShellViewModel>(RegionIds.Root);
        }

        private void SaveSignInData()
        {
            var pwd = AppDomain.CurrentDomain.BaseDirectory;
            string path = string.Concat(pwd, "/", AppConstants.Files.UserConfig);
            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write(IsSaveUsername);
                writer.Write(UserName);
            }
        }
        private void LoadSignInData()
        {
            var pwd = AppDomain.CurrentDomain.BaseDirectory;
            string path = string.Concat(pwd,"/", AppConstants.Files.UserConfig);
            if (!File.Exists(path))
                return;
            using (FileStream fs = new FileStream(path, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                IsSaveUsername = reader.ReadBoolean();
                if (IsSaveUsername)
                    UserName = reader.ReadString();
            }
        }
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
