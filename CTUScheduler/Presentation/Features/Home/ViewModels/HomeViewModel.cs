using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Core.Models.TeachingPlan;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog;

namespace CTUScheduler.Presentation.Features.Home.ViewModels;

public partial class HomeViewModel : ViewModelBase, IRoutableViewModel, IActivatableViewModel, IDisposable
{
    private readonly CompositeDisposable _disposable = new();
    private readonly ObservableAsPropertyHelper<RegistrationInformation?> _registrationInfo;
    private readonly ObservableAsPropertyHelper<IReadOnlyList<RegistrationTimelineItem>> _teachingPlanTimeline;

    public string UrlPathSegment => nameof(HomeViewModel);
    public IScreen HostScreen { get; }
    public ViewModelActivator Activator { get; } = new();
    public RegistrationInformation? RegistrationInfo => _registrationInfo.Value;
    public IReadOnlyList<RegistrationTimelineItem> TeachingPlanTimeline => _teachingPlanTimeline.Value;

    [Reactive] private bool _isLoading;

    public HomeViewModel(IScreen hostScreen,
        IUserSessionService userSessionService,
        IRegistrationRulesService registrationRulesService,
        IUserInteractionService userInteractionService,
        INavigationRegionManager navigationRegionManager,
        ICourseRegistrationService courseRegistrationService,
        AppState appState)
    {
        HostScreen = hostScreen;

        registrationRulesService.RegistrationInfoChanged
            .Subscribe(userSessionService.UpdateServerInfo)
            .DisposeWith(_disposable);

        IsLoading = true;

        Observable.StartAsync(async _ => await registrationRulesService.EnsureReadyAsync())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(
                result =>
                {
                    IsLoading = false;

                    result.Match(
                        () => { },
                        (errors, _) =>
                        {
                            var errorsString = String.Join('\n', errors.Select(x => x.FormattedMessage));
                            userInteractionService.Notification.Light.Error(errorsString);
                            navigationRegionManager.NavigateAndResetTo<LoginViewModel>(RegionIds.Root);

                            if (RegistrationInfo?.UserPeriod is { } period)
                            {
                                Console.WriteLine(
                                    $"Group: '{string.Join(", ", period.AllowedGroups)}' | Titles: {string.Join(", ", RegistrationInfo.Groups?.Select(g => $"'{g.Name}'") ?? [])}");
                            }
                        },
                        ex => { Debug.WriteLine(ex, "Lỗi khi _registrationRulesService.EnsureReadyAsync"); }
                    );
                },
                ex =>
                {
                    IsLoading = false;
                    Debug.WriteLine(ex, "Lỗi Runtime khi chạy EnsureReadyAsync");
                }
            )
            .DisposeWith(_disposable);

        _registrationInfo = userSessionService.RegistrationInfoChanged
            .ToProperty(this, nameof(RegistrationInfo), scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(_disposable);

        _teachingPlanTimeline = appState.TeachingPlanChanged
            .Select(plan => (IReadOnlyList<RegistrationTimelineItem>)(plan?.RegistrationTimeline ?? new List<RegistrationTimelineItem>()))
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, nameof(TeachingPlanTimeline), scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(_disposable);
    }

    public void Dispose()
    {
        _disposable.Dispose();
        Log.Debug(nameof(HomeViewModel) + ": Disposed");
    }
}