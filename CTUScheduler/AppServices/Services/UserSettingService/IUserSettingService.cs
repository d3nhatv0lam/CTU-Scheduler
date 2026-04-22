using System;
using CTUScheduler.Core.Models.Settings;

namespace CTUScheduler.AppServices.Services.UserSettingService;

public interface IUserSettingService
{
    IObservable<UserPreferences> SettingsChanged { get; }
    IObservable<AppearanceSettings> AppearanceSettingsChanged { get; }
    IObservable<AuthSettings> AuthSettingsChanged { get; }
    IObservable<ScheduleSettings> GeneralSettingsChanged { get; }
    
    UserPreferences CurrentPreferences { get; }
    AppearanceSettings CurrentAppearanceSettings { get; }
    AuthSettings CurrentAuthSettings { get; }
    
    void UpdateSettings(Func<UserPreferences, UserPreferences> updater);
}