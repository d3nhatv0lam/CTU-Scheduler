using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;
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
    
    [Reactive] private bool _isLoading = false;

    public ViewModelActivator Activator { get; } = new();
    public ReactiveCommand<Unit, OperationResult> SyncWebSessionCommand { get; }

    protected WebSyncViewModelBase(
        IUserInteractionService userInteractionService, 
        INavigationRegionManager navigationRegionManager)
    {
        UserInteractionService = userInteractionService;
        NavigationRegionManager = navigationRegionManager;

        SyncWebSessionCommand = ReactiveCommand.CreateFromTask(ExecuteWebSyncTaskAsync);

        SyncWebSessionCommand.IsExecuting
            .BindTo(this, x => x.IsLoading);

        SyncWebSessionCommand.Subscribe(result => 
        {
            result.Match(
                onSuccess: OnWebSyncSuccess,
                onFailure: (errors, reason) =>
                {
                    var errorsString = string.Join('\n', errors.Select(e => e.FormattedMessage));
                    if (!string.IsNullOrEmpty(errorsString))
                    {
                        UserInteractionService.Notification.Light.Error(errorsString);
                    }
                    
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

        this.WhenActivated((CompositeDisposable disposables) =>
        {
            OnWebSyncStarted();
            SyncWebSessionCommand.Execute().Subscribe().DisposeWith(disposables);
            SetupSubscriptions(disposables);
        });
    }

    protected abstract Task<OperationResult> ExecuteWebSyncTaskAsync();
    
    protected virtual void OnWebSyncStarted() { }
    
    protected virtual void OnWebSyncSuccess() { }
    
    protected virtual void OnWebSyncFailed(OperationResult result) { }

    protected virtual void SetupSubscriptions(CompositeDisposable disposables) { }
}
