using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.UserSettingService;

public class UserSettingService : IUserSettingService, IDisposable
{
    private readonly IDisposable _saveSubscription;
    private readonly BehaviorSubject<UserPreferences> _settingsSubject;
    private readonly IUserPreferencesRepository _userPreferencesRepository;
    private readonly ILogger<UserSettingService> _logger;

    public UserSettingService(AppState appState, IUserPreferencesRepository userPreferencesRepository,
        ILogger<UserSettingService> logger)
    {
        _settingsSubject = appState.UserSettingsSubject;
        _userPreferencesRepository = userPreferencesRepository;
        _logger = logger;

        SettingsChanged = _settingsSubject.AsObservable();
        AppearanceSettingsChanged = SettingsChanged.Select(settings => settings.Appearance).DistinctUntilChanged();
        AuthSettingsChanged = SettingsChanged.Select(settings => settings.Auth).DistinctUntilChanged();
        GeneralSettingsChanged = SettingsChanged.Select(settings => settings.Schedule).DistinctUntilChanged();
    }

    public IObservable<UserPreferences> SettingsChanged { get; }
    public IObservable<AppearanceSettings> AppearanceSettingsChanged { get; }
    public IObservable<AuthSettings> AuthSettingsChanged { get; }
    public IObservable<ScheduleSettings> GeneralSettingsChanged { get; }

    public UserPreferences CurrentPreferences => _settingsSubject.Value;
    public AppearanceSettings CurrentAppearanceSettings => CurrentPreferences.Appearance;
    public AuthSettings CurrentAuthSettings => CurrentPreferences.Auth;

    public async Task InitializeAsync()
    {
        _logger.LogDebug("Initializing user settings service...");
        var preferences = await _userPreferencesRepository.LoadAsync();

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
                _logger.LogError(preferences.Exception, "Exception when load user preferences.");
                _settingsSubject.OnNext(new UserPreferences());
            }
        );
    }

    public void UpdateSettings(Func<UserPreferences, UserPreferences> updater)
    {
        ArgumentNullException.ThrowIfNull(updater);
        _settingsSubject.OnNext(updater(_settingsSubject.Value));
    }

    public void Dispose()
    {
        // _saveSubscription.Dispose();
    }
}