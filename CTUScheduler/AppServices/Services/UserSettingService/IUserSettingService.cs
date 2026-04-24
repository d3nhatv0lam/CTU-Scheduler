using System;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Settings;

namespace CTUScheduler.AppServices.Services.UserSettingService;

public interface IUserSettingService
{
    IObservable<UserPreferences> SettingsChanged { get; }
    IObservable<AppearanceSettings> AppearanceSettingsChanged { get; }
    IObservable<AuthSettings> AuthSettingsChanged { get; }
    IObservable<ScheduleSettings> ScheduleSettingsChanged { get; }
    IObservable<BehaviorSettings> BehaviorSettingsChanged { get; }
    
    UserPreferences CurrentPreferences { get; }
    AppearanceSettings CurrentAppearanceSettings { get; }
    AuthSettings CurrentAuthSettings { get; }
    ScheduleSettings CurrentScheduleSettings { get; }
    BehaviorSettings CurrentBehaviorSettings { get; }

    Task InitializeAsync(CancellationToken token = default);
    void UpdateSettings(Func<UserPreferences, UserPreferences> updater);
}