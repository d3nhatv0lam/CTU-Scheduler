using System;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace CTUScheduler.AppServices.Services.UserSettingService;

public class UserSettingService : IUserSettingService, IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly IDisposable _autoSaveSubscription;
    private readonly BehaviorSubject<UserPreferences> _settingsSubject;
    private readonly IUserPreferencesRepository _userPreferencesRepository;
    private readonly ILogger<UserSettingService> _logger;

    private bool _isDisposed;
    private bool _isInitialized;

    public UserSettingService(AppState appState, IUserPreferencesRepository userPreferencesRepository,
        ILogger<UserSettingService> logger)
    {
        _settingsSubject = appState.UserSettingsSubject;
        _userPreferencesRepository = userPreferencesRepository;
        _logger = logger;

        SettingsChanged = _settingsSubject.AsObservable();
        AppearanceSettingsChanged = SettingsChanged.Select(settings => settings.Appearance).DistinctUntilChanged();
        AuthSettingsChanged = SettingsChanged.Select(settings => settings.Auth).DistinctUntilChanged();
        ScheduleSettingsChanged = SettingsChanged.Select(settings => settings.Schedule).DistinctUntilChanged();
        BehaviorSettingsChanged = SettingsChanged.Select(settings => settings.Behavior).DistinctUntilChanged();

        _autoSaveSubscription = _settingsSubject
            .SubscribeOn(RxSchedulers.TaskpoolScheduler)
            .Where(_ => _isInitialized)
            .Throttle(TimeSpan.FromMilliseconds(800))
            .DistinctUntilChanged()
            .Select(settings =>
                Observable.FromAsync(ct => _userPreferencesRepository.SaveAsync(settings, ct)))
            .Switch()
            .Subscribe(result =>
                {
                    result.Match(
                        onSuccess: () => _logger.LogDebug("User settings saved successfully."),
                        onFailure: (errors, _) =>
                            _logger.LogWarning("Failed to save user settings. Reason: {Errors}", errors),
                        onException: ex =>
                            _logger.LogError(ex, "Exception caught inside OperationResult when saving settings.")
                    );
                },
                criticalEx => _logger.LogCritical(criticalEx, "CRASHED when saving settings"));
    }

    public IObservable<UserPreferences> SettingsChanged { get; }
    public IObservable<AppearanceSettings> AppearanceSettingsChanged { get; }
    public IObservable<AuthSettings> AuthSettingsChanged { get; }
    public IObservable<ScheduleSettings> ScheduleSettingsChanged { get; }
    public IObservable<BehaviorSettings> BehaviorSettingsChanged { get; }
    public UserPreferences CurrentPreferences => _settingsSubject.Value;
    public AppearanceSettings CurrentAppearanceSettings => CurrentPreferences.Appearance;
    public AuthSettings CurrentAuthSettings => CurrentPreferences.Auth;
    public ScheduleSettings CurrentScheduleSettings => CurrentPreferences.Schedule;
    public BehaviorSettings CurrentBehaviorSettings => CurrentPreferences.Behavior;

    public async Task InitializeAsync(CancellationToken token = default)
    {
        if (_isInitialized) return;
        await _lock.WaitAsync(token);
        if (_isInitialized) return;
        try
        {
            _logger.LogDebug("Initializing {service}", nameof(UserSettingService));
            var preferences = await _userPreferencesRepository.LoadAsync(token);

            preferences.Match(
                userPrefs =>
                {
                    _logger.LogDebug("User preferences loaded.");
                    _settingsSubject.OnNext(userPrefs);
                },
                (errors, _) =>
                {
                    _logger.LogDebug(string.Join('\n', errors) + "\nUsed default settings.");
                    _settingsSubject.OnNext(new UserPreferences());
                },
                ex =>
                {
                    _logger.LogError(ex, "Exception when load user preferences.");
                    _settingsSubject.OnNext(new UserPreferences());
                }
            );
            _isInitialized = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void UpdateSettings(Func<UserPreferences, UserPreferences> updater)
    {
        ArgumentNullException.ThrowIfNull(updater);
        _lock.Wait();
        try
        {
            _settingsSubject.OnNext(updater(_settingsSubject.Value));
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _autoSaveSubscription.Dispose();
    }
}