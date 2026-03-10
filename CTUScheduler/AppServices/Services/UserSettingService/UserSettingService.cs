using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Models.Settings;

namespace CTUScheduler.AppServices.Services.UserSettingService;

public class UserSettingService: IUserSettingService
{
    private readonly BehaviorSubject<UserSettings> _settingsSubject;
    
    public UserSettingService(AppState appState)
    {
        _settingsSubject = appState.UserSettingsSubject;

        SettingsChanged = _settingsSubject.AsObservable();
        AppearanceSettingsChanged = SettingsChanged.Select(settings => settings.Appearance).DistinctUntilChanged();
        AuthSettingsChanged = SettingsChanged.Select(settings => settings.Auth).DistinctUntilChanged();
        GeneralSettingsChanged = SettingsChanged.Select(settings => settings.General).DistinctUntilChanged();
    }
    
    public IObservable<UserSettings> SettingsChanged { get; }
    public IObservable<AppearanceSettings> AppearanceSettingsChanged { get; }
    public IObservable<AuthSettings> AuthSettingsChanged { get; }
    public IObservable<GeneralSettings> GeneralSettingsChanged { get; }
    
    public UserSettings CurrentSettings => _settingsSubject.Value;
    public AppearanceSettings CurrentAppearanceSettings => CurrentSettings.Appearance;
    public AuthSettings CurrentAuthSettings => CurrentSettings.Auth;


    public void UpdateSettings(Func<UserSettings, UserSettings> updater)
    {
        ArgumentNullException.ThrowIfNull(updater);
        _settingsSubject.OnNext(updater(_settingsSubject.Value));
    }
}