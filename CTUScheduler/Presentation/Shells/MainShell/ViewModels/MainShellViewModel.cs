using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using CTUScheduler.Presentation.Features.Home.ViewModels;
using CTUScheduler.Presentation.Features.TimetableManager.ViewModels;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models.Dialogs;
using CTUScheduler.Presentation.Shared.Dialogs.ViewModels;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using CTUScheduler.Presentation.Shells.MainShell.Models;
using Material.Icons;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Shells.MainShell.ViewModels
{
    public partial class MainShellViewModel : WebSyncViewModelBase, IScreen, IRoutableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly IMainHomeService _mainHomeService;

        [Reactive] private NavigationItem _selectedItem;
        [Reactive] private string _userName = "họ tên";
        [Reactive] private string _userMSSV = "MSSV";
        [ObservableAsProperty] private string _title = "CTU Scheduler";

        public string UrlPathSegment => nameof(MainShellViewModel);
        public RoutingState Router { get; } = new();
        public IScreen HostScreen { get; }
        public IReadOnlyList<NavigationItem> NavigationItems { get; }

        public ReactiveCommand<Unit, string> LoadStudentInfoCommand { get; }
        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }


        public MainShellViewModel(IScreen hostScreen,
            IMainHomeService mainHomeService,
            INavigationRegionManager navigationRegionManager,
            IUserInteractionService userInteractionService) : base(userInteractionService, navigationRegionManager)
        {
            HostScreen = hostScreen;
            _mainHomeService = mainHomeService;

            NavigationRegionManager.Register(RegionIds.Main, this)
                .DisposeWith(_disposables);

            NavigationItems =
            [
                new NavigationItem("Trang chủ", MaterialIconKind.HomeOutline, typeof(HomeViewModel)),
                new NavigationItem("Học phần", MaterialIconKind.TableCog, typeof(TimetableManagerViewModel))
                // new NavigationItem("Cài đặt", MaterialIconKind.CogOutline,typeof(SettingViewModel))
            ];

            this.WhenAnyValue(x => x.SelectedItem)
                .WhereNotNull()
                .Subscribe(OnNavigatePage)
                .DisposeWith(_disposables);

            _titleHelper = this.WhenAnyValue(x => x.SelectedItem)
                .WhereNotNull()
                .Select(x => x.Title)
                .ToProperty(this, nameof(Title))
                .DisposeWith(_disposables);

            SelectedItem = NavigationItems[0];
            // SelectedItem = NavigationItems[1];

            LoadStudentInfoCommand = ReactiveCommand.CreateFromTask(_mainHomeService.GetStudentIdAsync)
                .DisposeWith(_disposables);

            LoadStudentInfoCommand
                .Catch((Exception _) => Observable.Return(string.Empty))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Split(" "))
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(arr =>
                {
                    UserName = string.Join(' ', arr[..^1]);
                    UserMSSV = arr[^1];
                }).DisposeWith(_disposables);

            LogoutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                using var confirmViewModel = new ConfirmDialogViewModel
                {
                    Title = "Đăng xuất",
                    Message = "Bạn có chắc chắn muốn đăng xuất ?",
                    ConfirmText = "Đăng xuất",
                    CancelText = "Không",
                    IsDestructive = true
                };

                var options = new DialogOptions()
                {
                    SizeMode = DialogSizeMode.Content,
                    CanLightDismiss = true,
                    HostId = DialogIds.Root
                };

                bool isAcceptLogout =
                    await UserInteractionService.Dialog.ShowModal<ConfirmDialogViewModel, bool>(confirmViewModel,
                        options);

                if (isAcceptLogout)
                {
                    await NavigationRegionManager.NavigateAndResetTo<LoginViewModel>(RegionIds.Root);
                }
            }).DisposeWith(_disposables);
        }


        private void OnNavigatePage(NavigationItem item)
        {
            var oldPage = Router.GetCurrentViewModel();
            NavigationRegionManager.NavigateAndResetTo(RegionIds.Main, item.ViewModelType);

            if (oldPage is IDisposable disposable)
                disposable.Dispose();
        }


        protected override async Task<OperationResult> ExecuteWebSyncTaskAsync()
        {
            return await _mainHomeService.EnsureReadyAsync();
        }

        protected override void OnWebSyncSuccess()
        {
            LoadStudentInfoCommand.Execute().Subscribe().DisposeWith(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}