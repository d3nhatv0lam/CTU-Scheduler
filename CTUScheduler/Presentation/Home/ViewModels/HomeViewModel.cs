using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.AppServices;
using CTUScheduler.AppServices.Helpers;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Presentation.Base;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Home.ViewModels
{
    public class HomeViewModel : ViewModelBase, IRoutableViewModel, IActivatableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        private readonly ICTUWebDriverService _CTUWebDriverService;
        private readonly ObservableAsPropertyHelper<RegistrationInformation> _registrationInfor;
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
            _CTUWebDriverService = App.ServiceProvider!.GetRequiredService<ICTUWebDriverService>();
            HostScreen = hostScreen;

            _registrationInfor = _CTUWebDriverService.RegistrationInformationResponse
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, nameof(RegistrationInfo));

            OpenFacebookCommand = ReactiveCommand.Create(() => OpenURL(AppConstants.FACEBOOK_URL)).DisposeWith(_disposable);
            OpenYoutubeCommand = ReactiveCommand.Create(() => OpenURL(AppConstants.YOUTUBE_URL)).DisposeWith(_disposable);
            OpenGithubCommand = ReactiveCommand.Create(() => OpenURL(AppConstants.GITHUB_URL)).DisposeWith(_disposable);
            OpenCTUHTQLCommand = ReactiveCommand.Create(() => OpenURL(AppConstants.CTU_SIGN_IN_URL)).DisposeWith(_disposable);

            this.WhenActivated((CompositeDisposable disposable) =>
            {
                disposable.Add(_registrationInfor);
                disposable.Add(_disposable);
            });

            LoadPage();
        }

        private void OpenURL(string url)
        {
            ProcessHelper.OpenUrl(url);
        }

        private async void LoadPage()
        {
            try
            {
                await _CTUWebDriverService.GoToRegistrationRulesPage();
            }
            catch
            {

            }
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
