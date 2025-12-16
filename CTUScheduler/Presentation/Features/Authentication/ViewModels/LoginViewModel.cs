using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.AppServices;
using CTUScheduler.AppServices.Services.WebDriver;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Shells.MainShell.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Authentication.ViewModels
{
    public class LoginViewModel : ViewModelBase, IDisposable, IRoutableViewModel
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ICTUWebDriverService _CTUWebDriverService;
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
        public LoginViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            _CTUWebDriverService = App.ServiceProvider!.GetRequiredService<ICTUWebDriverService>();

            var adapter = App.ServiceProvider.GetRequiredService<ICtuSitePageFactory>();
            LoadSignInData();

            Observable.StartAsync(() => adapter.LoginPage.NavigateToAsync());
            
            SignInCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await adapter.LoginPage.TryLoginAsync(UserName, Password);
                if (result.IsSuccess)
                {
                    OnLoggedIn();
                }
                else
                {
                    await Task.Delay(1000);
                }
            })
            .DisposeWith(_disposables);

            // auto try Goto Signin until success
            // Observable.Defer(() => Observable.StartAsync(GoToSignPage))
            //     .Catch<Unit, Exception>(ex =>
            //     {
            //         return Observable.Timer(TimeSpan.FromSeconds(3)).SelectMany(_ => Observable.Throw<Unit>(ex));
            //     })
            //     .Retry() // retry vô hạn
            //     .Subscribe(_ =>
            //     {
            //         //await FillCapchaImage();
            //     })
            //     .DisposeWith(_disposables);
            
            // InitCommand();
        }
#pragma warning restore CS8618

        

        private void InitCommand()
        {
            SignInCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var isLogged = await _CTUWebDriverService.TrySignInAsync(UserName, Password);
                if (isLogged)
                {
                    OnLoggedIn();
                }
                else
                {
                    // wait 1sec => reduce CPU usage & Lag
                    await Task.Delay(1000);
                }
            }).DisposeWith(_disposables);
        }

        private async Task GoToSignPage()
        {
            await _CTUWebDriverService.GoToSignInPageAsync();
        }

        private void OnLoggedIn()
        {
            SaveSignInData();
            Password = string.Empty;
            RxApp.MainThreadScheduler.Schedule(NavigateToHome);
            Dispose();
        }
        
        private void NavigateToHome()
        {
            HostScreen.Router.NavigateAndReset.Execute(new MainShellViewModel(HostScreen));
        }

        private void SaveSignInData()
        {
            string path = string.Concat(AppConstants.Pwd, "/", AppConstants.USERCONFIG_FILENAME);
            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write(IsSaveUsername);
                writer.Write(UserName);
            }
        }
        private void LoadSignInData()
        {
            string path = string.Concat(AppConstants.Pwd,"/", AppConstants.USERCONFIG_FILENAME);
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
