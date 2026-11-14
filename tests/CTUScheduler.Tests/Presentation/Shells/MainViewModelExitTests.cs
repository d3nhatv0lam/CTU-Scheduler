using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.UserSettingService;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Presentation.Services.ApplicationLifetime;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.Navigation.Models;
using CTUScheduler.Presentation.Services.Theme;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models.Dialogs;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;
using CTUScheduler.Presentation.Shared.Dialogs.ViewModels;
using CTUScheduler.Presentation.Shells.AppShell.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ReactiveUI;
using Xunit;

namespace CTUScheduler.Tests.Presentation.Shells;

public class MainViewModelExitTests
{
    private readonly IConnectivityService _connectivityService;
    private readonly INavigationRegionManager _navigationRegionManager;
    private readonly IViewContextService _viewContextService;
    private readonly IUserInteractionService _userInteractionService;
    private readonly IDialogService _dialogService;
    private readonly IUserSettingService _userSettingService;
    private readonly IAppLifecycleService _appLifecycleService;
    private readonly IThemeService _themeService;
    private readonly ILogger<MainViewModel> _logger;

    public MainViewModelExitTests()
    {
        _connectivityService = Substitute.For<IConnectivityService>();
        _connectivityService.IsInternetAvailable.Returns(Observable.Return(true));

        _navigationRegionManager = Substitute.For<INavigationRegionManager>();
        _navigationRegionManager.Register(Arg.Any<RegionId>(), Arg.Any<IScreen>())
            .Returns(Substitute.For<IDisposable>());
        var cachingNav = Substitute.For<ICachingNavigationService>();
        _navigationRegionManager.Get(Arg.Any<RegionId>()).Returns(cachingNav);

        _viewContextService = Substitute.For<IViewContextService>();
        
        _dialogService = Substitute.For<IDialogService>();
        _userInteractionService = Substitute.For<IUserInteractionService>();
        _userInteractionService.Dialog.Returns(_dialogService);

        _userSettingService = Substitute.For<IUserSettingService>();
        _userSettingService.CurrentPreferences.Returns(new UserPreferences
        {
            Behavior = new BehaviorSettings { ConfirmBeforeExit = true }
        });

        _appLifecycleService = Substitute.For<IAppLifecycleService>();
        _appLifecycleService.ApplicationStopping.Returns(CancellationToken.None);

        _themeService = Substitute.For<IThemeService>();
        _logger = Substitute.For<ILogger<MainViewModel>>();
    }

    private MainViewModel CreateViewModel()
    {
        return new MainViewModel(
            _connectivityService,
            _navigationRegionManager,
            _viewContextService,
            _userInteractionService,
            _userSettingService,
            _appLifecycleService,
            _themeService,
            _logger);
    }

    [Fact]
    public async Task ConfirmExitAsync_WhenApplicationStopping_ReturnsTrueWithoutDialog()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _appLifecycleService.ApplicationStopping.Returns(cts.Token);

        var vm = CreateViewModel();

        // Act
        var result = await vm.ConfirmExitAsync();

        // Assert
        result.Should().BeTrue();
        await _dialogService.DidNotReceiveWithAnyArgs()
            .ShowModal<ConfirmDialogViewModel, bool>(default!, default);
    }

    [Fact]
    public async Task ConfirmExitAsync_WhenConfirmBeforeExitIsFalse_ReturnsTrueWithoutDialog()
    {
        // Arrange
        _userSettingService.CurrentPreferences.Returns(new UserPreferences
        {
            Behavior = new BehaviorSettings { ConfirmBeforeExit = false }
        });

        var vm = CreateViewModel();

        // Act
        var result = await vm.ConfirmExitAsync();

        // Assert
        result.Should().BeTrue();
        await _dialogService.DidNotReceiveWithAnyArgs()
            .ShowModal<ConfirmDialogViewModel, bool>(default!, default);
    }

    [Fact]
    public async Task ConfirmExitAsync_WhenUserCancels_ReturnsFalseAndDoesNotUpdateSettings()
    {
        // Arrange
        _dialogService.ShowModal<ConfirmDialogViewModel, bool>(
                Arg.Any<ConfirmDialogViewModel>(),
                Arg.Any<DialogOptions>())
            .Returns(Task.FromResult(false));

        var vm = CreateViewModel();

        // Act
        var result = await vm.ConfirmExitAsync();

        // Assert
        result.Should().BeFalse();
        _userSettingService.DidNotReceive().UpdateSettings(Arg.Any<Func<UserPreferences, UserPreferences>>());
    }

    [Fact]
    public async Task ConfirmExitAsync_WhenUserConfirms_WithoutTickingDoNotAskAgain_ReturnsTrueAndDoesNotUpdateSettings()
    {
        // Arrange
        _dialogService.ShowModal<ConfirmDialogViewModel, bool>(
                Arg.Any<ConfirmDialogViewModel>(),
                Arg.Any<DialogOptions>())
            .Returns(callInfo =>
            {
                var dialogVm = callInfo.Arg<ConfirmDialogViewModel>();
                dialogVm.ShowDoNotAskAgain.Should().BeTrue();
                dialogVm.DoNotAskAgain = false;
                return Task.FromResult(true);
            });

        var vm = CreateViewModel();

        // Act
        var result = await vm.ConfirmExitAsync();

        // Assert
        result.Should().BeTrue();
        _userSettingService.DidNotReceive().UpdateSettings(Arg.Any<Func<UserPreferences, UserPreferences>>());
    }

    [Fact]
    public async Task ConfirmExitAsync_WhenUserConfirms_AndTicksDoNotAskAgain_UpdatesSettingsAndReturnsTrue()
    {
        // Arrange
        _dialogService.ShowModal<ConfirmDialogViewModel, bool>(
                Arg.Any<ConfirmDialogViewModel>(),
                Arg.Any<DialogOptions>())
            .Returns(callInfo =>
            {
                var dialogVm = callInfo.Arg<ConfirmDialogViewModel>();
                dialogVm.ShowDoNotAskAgain.Should().BeTrue();
                dialogVm.DoNotAskAgain = true;
                return Task.FromResult(true);
            });

        var vm = CreateViewModel();

        // Act
        var result = await vm.ConfirmExitAsync();

        // Assert
        result.Should().BeTrue();
        _userSettingService.Received(1).UpdateSettings(Arg.Any<Func<UserPreferences, UserPreferences>>());

        // Verify updateFunc modifies ConfirmBeforeExit to false
        var updateCall = _userSettingService.ReceivedCalls()
            .First(c => c.GetMethodInfo().Name == nameof(IUserSettingService.UpdateSettings));
        var updateFunc = (Func<UserPreferences, UserPreferences>)updateCall.GetArguments()[0]!;

        var initialPrefs = new UserPreferences
        {
            Behavior = new BehaviorSettings { ConfirmBeforeExit = true }
        };
        var updatedPrefs = updateFunc(initialPrefs);
        updatedPrefs.Behavior.ConfirmBeforeExit.Should().BeFalse();
    }
}
