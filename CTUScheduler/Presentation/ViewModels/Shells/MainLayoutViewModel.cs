using CTUScheduler.Presentation.ViewModels.Base;
using CTUScheduler.Presentation.ViewModels.Shells.Components;
using DialogHostAvalonia;
using Material.Icons;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.Shells
{
    public class MainLayoutViewModel: ViewModelBase, IRoutableViewModel, IDisposable
    {
        private CompositeDisposable _disposables = new CompositeDisposable();
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
        

        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }

        public MainLayoutViewModel()
        {

        }

        public MainLayoutViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            NavigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem("Trang chủ",MaterialIconKind.HomeOutline,null),
                new NavigationItem("Học phần", MaterialIconKind.TableCog,null),
                new NavigationItem("Cài đặt", MaterialIconKind.CogOutline,null)
            };

            this.WhenAnyValue(x => x.SelectedItem).Subscribe(item =>
            {
                Title = item?.Title ?? "UnLoaded";
            }).DisposeWith(_disposables);

            LogoutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var dialogVM = new LogoutDialogViewModel("MainLayoutDialog");
                object? result = await DialogHost.Show(dialogVM, "MainLayoutDialog");

                if (result is bool isLogout && isLogout)
                {
                    try
                    {
                        HostScreen.Router.NavigateAndReset.Execute(new SignInViewModel(HostScreen));
                    }
                    catch
                    {
                        // navigate to signin fail by internet connection
                    }
                }

            }).DisposeWith(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
