using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CTUScheduler.AppServices;
using CTUScheduler.AppServices.Helpers;
using CTUScheduler.AppServices.Services.Implementations;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Presentation.ViewModels.Base;
using CTUScheduler.Presentation.ViewModels.Shells;
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
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.Sign
{
    public class SignInViewModel : ViewModelBase, IDisposable, IRoutableViewModel
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ICTUWebDriverService _CTUWebDriverService;
        private readonly Subject<Bitmap> _captchaImageUpdated = new Subject<Bitmap>();
        private string _userName;
        private string _password;
        private Bitmap _captchaImage;
        private bool _isSaveUsername;


        public string? UrlPathSegment => "SignInView";
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
        public Bitmap CaptchaImage
        {
            get => _captchaImage;
            set => this.RaiseAndSetIfChanged(ref _captchaImage, value);
        }
        public bool IsSaveUsername
        {
            get => _isSaveUsername;
            set => this.RaiseAndSetIfChanged(ref _isSaveUsername, value);
        }

        public ReactiveCommand<Unit, Unit> SignInCommand { get; private set; }

#pragma warning disable CS8618
        public SignInViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            _CTUWebDriverService = App.ServiceProvider!.GetRequiredService<ICTUWebDriverService>();
            _userName = string.Empty;
            _password = string.Empty;
            _captchaImage = BitmapHelper.CreateEmptyBitmap();

            LoadSignInData();
            _disposables.Add(_captchaImageUpdated);

            // auto try Goto Signin until success
            Observable.Defer(() => Observable.StartAsync(GoToSignPage))
                .Catch<Unit, Exception>(ex =>
                {
                    return Observable.Timer(TimeSpan.FromSeconds(3)).SelectMany(_ => Observable.Throw<Unit>(ex));
                })
                .Retry() // retry vô hạn
                .Subscribe(_ =>
                {
                    //await FillCapchaImage();
                })
                .DisposeWith(_disposables);

            InitObservable();
            InitCommand();
        }
#pragma warning restore CS8618


        private void InitObservable()
        {

            _captchaImageUpdated
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(bitMapImage =>
                {
                    var oldImage = CaptchaImage;
                    CaptchaImage = bitMapImage;
                    oldImage.Dispose();
                }).DisposeWith(_disposables);
        }

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
                    CleanCaptcha();
                    //await FillCapchaImage();
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
            RxApp.MainThreadScheduler.Schedule(NavigateToHome);
            Dispose();
        }

        private void CleanCaptcha()
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                //Captcha = string.Empty;
            });
        }

        //private async Task FillCapchaImage()
        //{
        //    var bitMapImage = await _CTUWebDriverService.TryGetCaptchaImageAsync();
        //    if (bitMapImage != null)
        //        _captchaImageUpdated.OnNext(bitMapImage);
        //}

        private void NavigateToHome()
        {
            HostScreen.Router.NavigateAndReset.Execute(new MainLayoutViewModel(HostScreen));
        }

        private void SaveSignInData()
        {
            string path = string.Concat(AppConstants.PWD, "/", AppConstants.USERCONFG_FILENAME);
            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write(IsSaveUsername);
                writer.Write(UserName);
            }
        }
        private void LoadSignInData()
        {
            string path = string.Concat(AppConstants.PWD,"/", AppConstants.USERCONFG_FILENAME);
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
