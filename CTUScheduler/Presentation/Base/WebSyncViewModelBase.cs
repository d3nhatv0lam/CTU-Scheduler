using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Base;

public abstract partial class WebSyncViewModelBase : ViewModelBase, IActivatableViewModel
{
    protected readonly IUserInteractionService UserInteractionService;
    protected readonly INavigationRegionManager NavigationRegionManager;
    protected readonly IConnectivityService ConnectivityService;
    
    [Reactive] private bool _isLoading;

    public ViewModelActivator Activator { get; } = new();
    public ReactiveCommand<Unit, OperationResult> SyncWebSessionCommand { get; }

    protected WebSyncViewModelBase(
        IUserInteractionService userInteractionService,
        INavigationRegionManager navigationRegionManager,
        IConnectivityService connectivityService)
    {
        UserInteractionService = userInteractionService;
        NavigationRegionManager = navigationRegionManager;
        ConnectivityService = connectivityService;

        var canSync = ConnectivityService.IsInternetAvailable
            .ObserveOn(RxSchedulers.MainThreadScheduler);

        SyncWebSessionCommand = ReactiveCommand.CreateFromTask(ExecuteWebSyncTaskAsync, canSync);

        SyncWebSessionCommand.IsExecuting
            .BindTo(this, x => x.IsLoading);
        

        SyncWebSessionCommand.Subscribe(result =>
        {
            result.Match(
                onSuccess: OnWebSyncSuccess,
                onFailure: (errors, reason) =>
                {
                    // var errorsString = string.Join('\n', errors.Select(e => e.FormattedMessage));
                    // if (!string.IsNullOrEmpty(errorsString))
                    // {
                    //     UserInteractionService.Notification.Light.Error(errorsString);
                    // }

                    if (reason == OperationFailureReason.Unauthorized ||
                        errors.Any(e => e.Code.Contains("Auth") || e.Code.Contains("Session")))
                    {
                        NavigationRegionManager.NavigateAndResetTo<LoginViewModel>(RegionIds.Root);
                    }

                    OnWebSyncFailed(result);
                },
                onException: _ => OnWebSyncFailed(result)
            );
        });

        this.WhenActivated(disposables =>
        {
            OnWebSyncStarted();
            
            Observable.Return(Unit.Default)
                .InvokeCommand(SyncWebSessionCommand)
                .DisposeWith(disposables);

            SetupSubscriptions(disposables);
        });
    }

    protected abstract Task<OperationResult> ExecuteWebSyncTaskAsync();

    protected virtual void OnWebSyncStarted()
    {
    }

    protected virtual void OnWebSyncSuccess()
    {
    }

    protected virtual void OnWebSyncFailed(OperationResult result)
    {
    }

    protected virtual void SetupSubscriptions(CompositeDisposable disposables)
    {
    }
}