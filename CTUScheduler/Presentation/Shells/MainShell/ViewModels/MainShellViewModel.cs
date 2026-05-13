using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Core.Models.Shared;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Abstractions;
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
    public partial class MainShellViewModel : WebSyncViewModelBase, IScreen, IRoutableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly IMainHomeService _mainHomeService;

        [Reactive] private NavigationItem? _selectedItem;
        [Reactive] private string _userName = "họ tên";
        [Reactive] private string _userMSSV = "MSSV";
        [ObservableAsProperty] private string _title = "CTU Scheduler";

        public string UrlPathSegment => nameof(MainShellViewModel);
        public RoutingState Router { get; } = new();
        public IScreen HostScreen { get; }
        public IReadOnlyList<NavigationItem> NavigationItems { get; }

        public ReactiveCommand<Unit, OperationResult<StudentProfile>> LoadStudentInfoCommand { get; }
        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }


        public MainShellViewModel(IScreen hostScreen,
            IMainHomeService mainHomeService,
            INavigationRegionManager navigationRegionManager,
            IUserInteractionService userInteractionService,
            IConnectivityService connectivityService) : base(userInteractionService, navigationRegionManager, connectivityService)
        {
            HostScreen = hostScreen;
            _mainHomeService = mainHomeService;


            NavigationRegionManager.Register(RegionIds.Main, this)
                .DisposeWith(_disposables);

            NavigationItems =
            [
                new NavigationItem("Trang chủ", MaterialIconKind.HomeOutline, typeof(HomeViewModel)),
                new NavigationItem("Học phần", MaterialIconKind.TableCog, typeof(TimetableManagerViewModel)),
                new NavigationItem("Liên hệ", MaterialIconKind.EmailOutline, typeof(ContactViewModel))
                // new NavigationItem("Cài đặt", MaterialIconKind.CogOutline,typeof(SettingViewModel))
            ];

            this.WhenAnyValue(x => x.SelectedItem)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Subscribe(OnNavigatePage)
                .DisposeWith(_disposables);

            _titleHelper = this.WhenAnyValue(x => x.SelectedItem)
                .Where(x => x.HasValue)
                .Select(x => x!.Value.Title)
                .ToProperty(this, nameof(Title))
                .DisposeWith(_disposables);

            SelectedItem = NavigationItems[0];
            // SelectedItem = NavigationItems[1];

            LoadStudentInfoCommand = ReactiveCommand.CreateFromObservable(() =>
                    Observable.FromAsync(ct => _mainHomeService.GetStudentProfileAsync(ct))
                        .Expand(result =>
                        {
                            if (result.IsSuccess || result.Kind == OperationFailureReason.Unauthorized)
                                return Observable.Empty<OperationResult<StudentProfile>>();
                            
                            return Observable.Timer(TimeSpan.FromSeconds(1), RxSchedulers.TaskpoolScheduler)
                                .SelectMany(_ =>
                                    Observable.FromAsync(ct => _mainHomeService.GetStudentProfileAsync(ct)));
                        })
                        .Take(10)
                        .LastAsync())
                .DisposeWith(_disposables);

            LoadStudentInfoCommand
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(result =>
                {
                    result.Match(
                        onSuccess: profile =>
                        {
                            UserName = profile.Name;
                            UserMSSV = profile.Mssv;
                        },
                        onFailure: (errors, _) =>
                        {
                            var errorStr = string.Join('\n', errors.Select(x => x.FormattedMessage));
                            Debug.WriteLine(errorStr);
                        }
                    );
                })
                .DisposeWith(_disposables);

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