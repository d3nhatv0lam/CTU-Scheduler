using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Base;

public abstract partial class SessionSyncViewModelBase : ViewModelBase, IActivatableViewModel, IDisposable
{
    protected readonly CompositeDisposable Disposables = new();
    protected readonly IUserInteractionService UserInteractionService;
    protected readonly INavigationRegionManager NavigationRegionManager;
    protected readonly IConnectivityService ConnectivityService;
    protected readonly ILogger Logger;

    private bool _isDisposed;

    [Reactive] private bool _isLoading;

    public ViewModelActivator Activator { get; } = new();
    public ReactiveCommand<Unit, OperationResult> SyncSessionCommand { get; }

    protected SessionSyncViewModelBase(
        IUserInteractionService userInteractionService,
        INavigationRegionManager navigationRegionManager,
        IConnectivityService connectivityService,
        ILogger logger)
    {
        UserInteractionService = userInteractionService;
        NavigationRegionManager = navigationRegionManager;
        ConnectivityService = connectivityService;
        Logger = logger;
        
        var canSync = ConnectivityService.IsInternetAvailable
            .ObserveOn(RxSchedulers.MainThreadScheduler);

        SyncSessionCommand = ReactiveCommand.CreateFromTask(async (cancellationToken) =>
        {
            try
            {
                return await ExecuteSyncTaskAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Logger.LogDebug("Tác vụ đồng bộ phiên bị hủy.");
                return OperationResult.Failed("Tác vụ đồng bộ phiên bị hủy.", "Sync.Canceled", OperationFailureReason.UserAction);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi không xác định phát sinh trong tác vụ đồng bộ phiên.");
                return OperationResult.FromException(ex, "Lỗi không xác định", "Sync.Unexpected", OperationFailureReason.System);
            }
        }, canSync).DisposeWith(Disposables);
        

        this.WhenActivated(disposables =>
        {
            SyncSessionCommand.IsExecuting
                .BindTo(this, x => x.IsLoading)
                .DisposeWith(disposables);

            SyncSessionCommand.Subscribe(result =>
                {
                    result.Match(
                        onSuccess: OnSyncSuccess,
                        onFailure: (errors, reason) =>
                        {
                            if (reason == OperationFailureReason.Unauthorized ||
                                errors.Any(e => e.Code.Contains("Auth") || e.Code.Contains("Session")))
                            {
                                NavigationRegionManager.NavigateAndResetTo<LoginViewModel>(RegionIds.Root);
                            }

                            OnSyncFailed(result);
                        },
                        onException: _ => OnSyncFailed(result)
                    );
                })
                .DisposeWith(disposables);

            OnSyncStarted();

            Observable.Return(Unit.Default)
                .InvokeCommand(SyncSessionCommand)
                .DisposeWith(disposables);

            SetupSubscriptions(disposables);
        });
    }

    protected abstract Task<OperationResult> ExecuteSyncTaskAsync(CancellationToken cancellationToken);

    protected virtual void OnSyncStarted()
    {
    }

    protected virtual void OnSyncSuccess()
    {
    }

    protected virtual void OnSyncFailed(OperationResult result)
    {
    }

    protected virtual void SetupSubscriptions(CompositeDisposable disposables)
    {
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            Disposables.Dispose();
            Logger.LogDebug("Disposed");
        }

        _isDisposed = true;
    }
}