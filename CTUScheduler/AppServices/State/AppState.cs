using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Settings;
using DynamicData;

namespace CTUScheduler.AppServices.State;

public class AppState : IAppState, IDisposable
{
    // System Config
    internal readonly SystemConfig SystemConfig = new();
    private readonly BehaviorSubject<UserSettings> _userSettingsSubject = new(new UserSettings());
    private readonly BehaviorSubject<RegistrationInformation?> _registrationInfo = new(null);
    private readonly SourceCache<RuntimeCourse, string> _runtimeCoursesSource = new(x => x.Code);
    private readonly SourceList<ScheduleProfile> _scheduleProfilesSource  = new();
    internal SourceCache<RuntimeCourse, string> RuntimeCoursesSource => _runtimeCoursesSource;
    internal SourceList<ScheduleProfile> ScheduleProfilesSource => _scheduleProfilesSource;
    
    public IObservable<UserSettings> UserSettingChanged { get; }
    public UserSettings CurrentSettings 
    {
        get => _userSettingsSubject.Value;
        set => _userSettingsSubject.OnNext(value);
    }
    
    /// <summary>
    /// Session Registration Information, live update from CTU Web
    /// </summary>
    public IObservable<RegistrationInformation?> RegistrationInfo { get; }
    public IObservableCache<RuntimeCourse, string> RuntimeCourses { get; } 
    public IObservableList<ScheduleProfile> ScheduleProfiles { get; }

    public AppState()
    {
        UserSettingChanged = _userSettingsSubject.AsObservable();
        RegistrationInfo =_registrationInfo.AsObservable();
        RuntimeCourses = _runtimeCoursesSource.AsObservableCache();
        ScheduleProfiles = _scheduleProfilesSource.AsObservableList();
    }

    public void UpdateRegistrationInfo(RegistrationInformation? info)
    {
        _registrationInfo.OnNext(info);
    }

    public void Dispose()
    {
        _userSettingsSubject.Dispose();
        _registrationInfo.Dispose();
        _runtimeCoursesSource.Dispose();
        _scheduleProfilesSource.Dispose();
    }
}