using System;
using CTUScheduler.Core.Models.Settings;

namespace CTUScheduler.AppServices.Services.UserSettingService;

public interface IUserSettingService
{
    IObservable<UserSettings> SettingsChanged { get; }
    IObservable<AppearanceSettings> AppearanceSettingsChanged { get; }
    IObservable<AuthSettings> AuthSettingsChanged { get; }
    IObservable<GeneralSettings> GeneralSettingsChanged { get; }
    
    UserSettings CurrentSettings { get; }
    AppearanceSettings CurrentAppearanceSettings { get; }
    AuthSettings CurrentAuthSettings { get; }
    
    void UpdateSettings(Func<UserSettings, UserSettings> updater);
}