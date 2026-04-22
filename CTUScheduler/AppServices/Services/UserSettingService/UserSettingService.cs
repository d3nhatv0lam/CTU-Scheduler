using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Models.Settings;

namespace CTUScheduler.AppServices.Services.UserSettingService;

public class UserSettingService: IUserSettingService, IDisposable
{
    private readonly IDisposable _saveSubscription;
    private readonly BehaviorSubject<UserPreferences> _settingsSubject;
    
    public UserSettingService(AppState appState)
    {
        _settingsSubject = appState.UserSettingsSubject;

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


    public void UpdateSettings(Func<UserPreferences, UserPreferences> updater)
    {
        ArgumentNullException.ThrowIfNull(updater);
        _settingsSubject.OnNext(updater(_settingsSubject.Value));
    }

    public void Dispose()
    {
        _settingsSubject.Dispose();
        // _saveSubscription.Dispose();
    }
}