using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Services.MainHomeService;
using CTUScheduler.AppServices.Services.WebDriver;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using CTUScheduler.Presentation.Features.Home.ViewModels;
using CTUScheduler.Presentation.Features.Setting.ViewModels;
using CTUScheduler.Presentation.Features.TimetableManager.ViewModels;
using CTUScheduler.Presentation.Services.Dialogs;
using CTUScheduler.Presentation.Shells.MainShell.Models;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Shells.MainShell.ViewModels
{
    public class MainShellViewModel: ViewModelBase, IScreen , IRoutableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        // private ICTUWebDriverService _CTUWebDriverService;
        private IDialogHostService _dialogHostService;
        private NavigationItem _selectedItem;
        private readonly IMainHomeService _mainHomeService;
        private string _userName = "họ tên";
        private string _userMSSV = "MSSV";
        private string _title;

        public string? UrlPathSegment => "MainLayout view";
        public RoutingState Router { get; }
        public IScreen HostScreen { get; }
        public ObservableCollection<NavigationItem> NavigationItems { get; }
        public NavigationItem SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        public string UserName
        {
            get => _userName;
            set => this.RaiseAndSetIfChanged(ref _userName, value);
        }
        public string UserMSSV
        {
            get => _userMSSV;
            set => this.RaiseAndSetIfChanged(ref _userMSSV, value);
        }

        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }
        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }

        public MainShellViewModel()
        {

        }

        public MainShellViewModel(IScreen hostScreen)
        {
            Router = new RoutingState();
            HostScreen = hostScreen;
            // _CTUWebDriverService = App.ServiceProvider!.GetRequiredService<ICTUWebDriverService>();
            _dialogHostService = App.ServiceProvider!.GetRequiredService<IDialogHostService>();
            _mainHomeService = App.ServiceProvider.GetRequiredService<IMainHomeService>();

            _mainHomeService.StudentIdChanges
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(userText =>
                {
                    string[] userInfoArray = userText.Split(" ");
                    UserName = string.Join(' ', userInfoArray[0..^1]);
                    UserMSSV = userInfoArray[^1];
                }).DisposeWith(_disposables);
            
            NavigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem("Trang chủ",MaterialIconKind.HomeOutline,typeof(HomeViewModel)),
                new NavigationItem("Học phần", MaterialIconKind.TableCog,typeof(TimetableManagerViewModel)),
                new NavigationItem("Cài đặt", MaterialIconKind.CogOutline,typeof(SettingViewModel))
            };

            this.WhenAnyValue(x => x.SelectedItem)
                .WhereNotNull()
                .Subscribe(item => OnNavigatePage(item))
                .DisposeWith(_disposables);

            SelectedItem = NavigationItems[0];
            // SelectedItem = NavigationItems[1];

            LogoutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                bool isAcceptLogout = await _dialogHostService.ShowDialogAsync<LogoutDialogViewModel,bool>(new LogoutDialogViewModel(), DialogIdentifier.MainLayout);
                if (isAcceptLogout)
                {
                    HostScreen.Router.NavigateAndReset.Execute(new LoginViewModel(HostScreen));
                    Dispose();
                }
            }).DisposeWith(_disposables);
        }
        
        private void OnNavigatePage(NavigationItem item)
        {
            Title = item.Title;
            var page = (IRoutableViewModel)Activator.CreateInstance(item.ViewModelType,HostScreen)!;
            var oldPage = Router.GetCurrentViewModel();
            Router.NavigateAndReset.Execute(page);
                    
            if (oldPage is IDisposable disposable)
                disposable.Dispose();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
