using CTUScheduler.AppServices.Services.Implementations;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Presentation.ViewModels.Base;
using CTUScheduler.Presentation.ViewModels.CoursePage;
using CTUScheduler.Presentation.ViewModels.HomePage;
using CTUScheduler.Presentation.ViewModels.SettingsPage;
using CTUScheduler.Presentation.ViewModels.Shells.Components;
using CTUScheduler.Presentation.ViewModels.Sign;
using DialogHostAvalonia;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.Shells
{
    public class MainLayoutViewModel: ViewModelBase, IScreen , IRoutableViewModel, IDisposable
    {
        private CompositeDisposable _disposables = new CompositeDisposable();
        private IDialogHostService _dialogHostService;
        private NavigationItem _selectedItem;
        private string _title;

        public string? UrlPathSegment => "MainLayout view";
        public IScreen HostScreen { get; }
        public ObservableCollection<NavigationItem> NavigationItems { get; }
        public NavigationItem SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }
        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }
        public RoutingState Router { get; }

        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }

        public MainLayoutViewModel()
        {

        }

        public MainLayoutViewModel(IScreen hostScreen)
        {
            Router = new RoutingState();
            HostScreen = hostScreen;
            _dialogHostService = App.ServiceProvider!.GetRequiredService<IDialogHostService>();
            NavigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem("Trang chủ",MaterialIconKind.HomeOutline,typeof(HomePageViewModel)),
                new NavigationItem("Học phần", MaterialIconKind.TableCog,typeof(CoursePageViewModel)),
                new NavigationItem("Cài đặt", MaterialIconKind.CogOutline,typeof(SettingsPageViewModel))
            };

            this.WhenAnyValue(x => x.SelectedItem).WhereNotNull().Subscribe(item =>
            {
                Title = item.Title;
                var page = (IRoutableViewModel)Activator.CreateInstance(item.ViewModelType,HostScreen)!;
                Router.NavigateAndReset.Execute(page);
            }).DisposeWith(_disposables);

            SelectedItem = NavigationItems.First();

            LogoutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                bool isAcceptLogout = await _dialogHostService.ShowDialog<bool>(new LogoutDialogViewModel("MainLayoutDialog"), "MainLayoutDialog");
                if (isAcceptLogout)
                {
                    HostScreen.Router.NavigateAndReset.Execute(new SignInViewModel(HostScreen));
                    Dispose();
                }
            }).DisposeWith(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
