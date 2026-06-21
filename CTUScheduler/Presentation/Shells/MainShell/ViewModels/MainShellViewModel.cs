using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.CtuSessions;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using CTUScheduler.Presentation.Features.Contact.ViewModels;
using CTUScheduler.Presentation.Features.Home.ViewModels;
using CTUScheduler.Presentation.Features.TimetableManager.ViewModels;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models.Dialogs;
using CTUScheduler.Presentation.Shared.Dialogs.ViewModels;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using CTUScheduler.Presentation.Shells.MainShell.Models;
using Material.Icons;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Shells.MainShell.ViewModels
{
    public partial class MainShellViewModel : SessionSyncViewModelBase, IScreen, IRoutableViewModel
    {
        [Reactive] private NavigationItem? _selectedItem;
        [Reactive] private string _userName = "họ tên";
        [Reactive] private string _userMSSV = "MSSV";
        [ObservableAsProperty] private string _title = "CTU Scheduler";

        public string UrlPathSegment => nameof(MainShellViewModel);
        public RoutingState Router { get; } = new();
        public IScreen HostScreen { get; }
        public IReadOnlyList<NavigationItem> NavigationItems { get; }
        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }


        public MainShellViewModel(IScreen hostScreen,
            INavigationRegionManager navigationRegionManager,
            ISessionCoordinator sessionCoordinator,
            IUserInteractionService userInteractionService,
            ICtuSessionAccessor ctuSessionAccessor,
            IConnectivityService connectivityService,
            ILogger<MainShellViewModel> logger) : base(userInteractionService,
            navigationRegionManager, connectivityService, logger)
        {
            HostScreen = hostScreen;

            NavigationRegionManager.Register(RegionIds.Main, this)
                .DisposeWith(Disposables);

            NavigationItems =
            [
                new NavigationItem("Trang chủ", MaterialIconKind.HomeOutline, typeof(HomeViewModel)),
                new NavigationItem("Học phần", MaterialIconKind.TableCog, typeof(TimetableManagerViewModel)),
                new NavigationItem("Liên hệ", MaterialIconKind.EmailOutline, typeof(ContactViewModel))
                // new NavigationItem("Cài đặt", MaterialIconKind.CogOutline,typeof(SettingViewModel))
            ];

            ctuSessionAccessor.Changed
                .WhereNotNull()
                .Select(x => x.Profile)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(profile =>
                {
                    UserName = profile.Name;
                    UserMSSV = profile.Mssv;
                })
                .DisposeWith(Disposables);

            ctuSessionAccessor.Changed
                .Where(x => x is null)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    UserName = _userName;
                    UserMSSV = _userMSSV;
                })
                .DisposeWith(Disposables);

            this.WhenAnyValue(x => x.SelectedItem)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Subscribe(OnNavigatePage)
                .DisposeWith(Disposables);

            _titleHelper = this.WhenAnyValue(x => x.SelectedItem)
                .Where(x => x.HasValue)
                .Select(x => x!.Value.Title)
                .ToProperty(this, nameof(Title))
                .DisposeWith(Disposables);

            SelectedItem = NavigationItems[0];
            // SelectedItem = NavigationItems[1];

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
                    IsCloseButtonVisible = false,
                    HostId = DialogIds.Root
                };

                bool isAcceptLogout =
                    await UserInteractionService.Dialog.ShowModal<ConfirmDialogViewModel, bool>(confirmViewModel,
                        options);

                if (isAcceptLogout)
                {
                    await NavigationRegionManager.NavigateAndResetTo<LoginViewModel>(RegionIds.Root);
                    await sessionCoordinator.EndSessionAsync();
                }
            }).DisposeWith(Disposables);
        }


        private void OnNavigatePage(NavigationItem item)
        {
            var oldPage = Router.GetCurrentViewModel();
            NavigationRegionManager.NavigateAndResetTo(RegionIds.Main, item.ViewModelType);

            if (oldPage is IDisposable disposable)
                disposable.Dispose();
        }

        protected override Task<OperationResult> ExecuteSyncTaskAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(OperationResult.Success());
        }
    }
}