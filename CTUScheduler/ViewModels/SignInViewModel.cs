using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CTUScheduler.Services;
using CTUScheduler.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CTUScheduler.ViewModels
{
    public class SignInViewModel : ViewModelBase, IDisposable, IRoutableViewModel
    {
        private readonly WebDriverService _webDriverService;
        private string _username;
        private string _password;
        private string _capcha;
        private Bitmap _capchaImage;
        private bool _isSaveUsername;
        private bool _isLoginButtonEnabled = true;
        private IObservable<bool> CanLogin;
        private bool _isLoginSuccessful;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public string? UrlPathSegment => "SignIn View";
        public IScreen HostScreen { get; }

        public string Username
        {
            get => _username;
            set => this.RaiseAndSetIfChanged(ref _username, value);
        }
        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }
        public string Capcha
        {
            get => _capcha;
            set => this.RaiseAndSetIfChanged(ref _capcha, value);
        }
        public Bitmap CapchaImage
        {
            get => _capchaImage;
            set => this.RaiseAndSetIfChanged(ref _capchaImage, value);
        }
        public bool IsSaveUsername
        {
            get => _isSaveUsername;
            set => this.RaiseAndSetIfChanged(ref _isSaveUsername, value);
        }
        public bool IsLoginButtonEnabled
        {
            get => _isLoginButtonEnabled;
            set => this.RaiseAndSetIfChanged(ref _isLoginButtonEnabled, value);
        }
        

        public ReactiveCommand<Unit, Unit> SignInCommand { get; private set; }

#pragma warning disable CS8618
        public SignInViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            _webDriverService = App.ServiceProvider!.GetRequiredService<WebDriverService>();
            _username = string.Empty;
            _password = string.Empty;
            _capcha = string.Empty;
            _capchaImage = BitmapHelper.CreateEmptyBitmap();
            _isLoginSuccessful = false;
            LoadSignInData();

            FillCapchaImage().GetAwaiter().GetResult();
            InitObservable();
            InitCommand();
        }
#pragma warning restore CS8618




        private void InitObservable()
        {
            Observable.FromEventPattern(_webDriverService, nameof(_webDriverService.AlertBoxOpened))
                    .Subscribe(_ =>
                    {
                        _isLoginSuccessful = false;
                    }).DisposeWith(_disposables);

            CanLogin = this.WhenAnyValue(x => x.IsLoginButtonEnabled, x => x == true);
        }

        private void InitCommand()
        {
            SignInCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                RxApp.MainThreadScheduler.Schedule(() => { IsLoginButtonEnabled = false; });
                _isLoginSuccessful = true;
                bool isInteractedOnWeb = await TrySignIn();

                if (!isInteractedOnWeb)
                {
                    // enable button
                    await Task.Delay(500);
                    RxApp.MainThreadScheduler.Schedule(() => { IsLoginButtonEnabled = true; });
                    return;
                }

                await Task.Delay(200);

                if (_isLoginSuccessful)
                {
                    OnLoggedIn();
                }
                else
                {
                    await FillCapchaImage();
                }

                await Task.Delay(500);
                RxApp.MainThreadScheduler.Schedule(() => { IsLoginButtonEnabled = true; });

            }, CanLogin).DisposeWith(_disposables);
        }

        private void OnLoggedIn()
        {
            SaveSignInData();
            RxApp.MainThreadScheduler.Schedule(NavigateToHome);
            Dispose();
        }

        private Task FillCapchaImage()
        {
            Observable.StartAsync(async () => await GetCapchaImage(), RxApp.TaskpoolScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(bitMapImage => CapchaImage = bitMapImage).DisposeWith(_disposables);
            return Task.FromResult(0);
        }

        private async Task<Bitmap> GetCapchaImage()
        {
            Bitmap imageSource;
            try
            {
                ILocator imageLocator = _webDriverService.LocatorElement(AppConstants.CTU_LOGIN_CAPCHA_IMAGE);
                byte[] image = await _webDriverService.GetImageToByteArray(imageLocator);
                using var stream = new MemoryStream(image);
                {
                    imageSource = new Bitmap(stream);
                }
            }
            catch
            {
                imageSource = new Bitmap(Stream.Null);
            }
            return imageSource;
        }


        private async Task<bool> TrySignIn()
        {
            try
            {
                ILocator usernameInput = _webDriverService.LocatorElement(AppConstants.CTU_LOGIN_USERNAME);
                await _webDriverService.FillElement(usernameInput, Username);
                ILocator passwordInput = _webDriverService.LocatorElement(AppConstants.CTU_LOGIN_PASSWORD);
                await _webDriverService.FillElement(passwordInput, Password);
                ILocator capchaInput = _webDriverService.LocatorElement(AppConstants.CTU_LOGIN_CAPCHA);
                await _webDriverService.FillElement(capchaInput, Capcha);
                ILocator loginButton = _webDriverService.LocatorElement(AppConstants.CTU_LOGIN_BUTTON);
                await _webDriverService.ClickNavigateElement(loginButton);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void NavigateToHome()
        {
            HostScreen.Router.NavigateAndReset.Execute(new MainHomeViewModel(HostScreen));
        }

        private void SaveSignInData()
        {
            string path = AppConstants.PWD + "/" + AppConstants.USERCONFG_FILENAME;
            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write(IsSaveUsername);
                writer.Write(Username);
            }
        }
        private void LoadSignInData()
        {
            string path = AppConstants.PWD + "/" + AppConstants.USERCONFG_FILENAME;
            if (!File.Exists(path))
                return;
            using (FileStream fs = new FileStream(path, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                IsSaveUsername = reader.ReadBoolean();
                if (IsSaveUsername)
                    Username = reader.ReadString();
            }
        }
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
