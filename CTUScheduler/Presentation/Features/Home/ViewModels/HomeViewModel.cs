using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.Features.Home.ViewModels;

public partial class HomeViewModel : WebSyncViewModelBase, IRoutableViewModel, IDisposable
{
    private readonly IRegistrationRulesService _registrationRulesService;
    private readonly CompositeDisposable _disposable = new();
    private readonly ObservableAsPropertyHelper<RegistrationInformation?> _registrationInfo;

    public string UrlPathSegment => nameof(HomeViewModel);
    public IScreen HostScreen { get; }
    public RegistrationInformation? RegistrationInfo => _registrationInfo.Value;

    public HomeViewModel(IScreen hostScreen,
        IUserSessionService userSessionService,
        IRegistrationRulesService registrationRulesService,
        IUserInteractionService userInteractionService,
        INavigationRegionManager navigationRegionManager) : base(userInteractionService, navigationRegionManager)
    {
        HostScreen = hostScreen;
        _registrationRulesService = registrationRulesService;

        registrationRulesService.RegistrationInfoChanged
            .Subscribe(userSessionService.UpdateServerInfo)
            .DisposeWith(_disposable);

        _registrationInfo = userSessionService.RegistrationInfoChanged
            .ToProperty(this, nameof(RegistrationInfo), scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(_disposable);
    }

    protected override async Task<OperationResult> ExecuteWebSyncTaskAsync()
    {
        return await _registrationRulesService.EnsureReadyAsync();
    }

    public void Dispose()
    {
        _disposable.Dispose();
        Log.Debug(nameof(HomeViewModel) + ": Disposed");
    }
}