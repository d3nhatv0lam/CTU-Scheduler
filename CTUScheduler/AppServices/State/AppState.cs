using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Settings;
using DynamicData;

namespace CTUScheduler.AppServices.State;

public class AppState : IDisposable
{
    // System Config
    internal readonly SystemConfig SystemConfig = new();
    // User Settings
    private readonly BehaviorSubject<UserSettings> _userSettingsSubject = new(new UserSettings());
    public IObservable<UserSettings> UserSettingChanged => _userSettingsSubject.AsObservable();
    public UserSettings CurrentSettings 
    {
        get => _userSettingsSubject.Value;
        set => _userSettingsSubject.OnNext(value);
    }
    
    /// <summary>
    /// Session Registration Information, live update from CTU Web
    /// </summary>
    private readonly BehaviorSubject<RegistrationInformation?> _registrationInfo = new(null);
    public IObservable<RegistrationInformation?> RegistrationInfo => _registrationInfo
        .AsObservable()
        .Publish()
        .RefCount();
    
    // Schedule Data
    private readonly SourceCache<RuntimeCourse, string> _runtimeCoursesSource = new(x => x.Code);
    public IObservableCache<RuntimeCourse, string> RuntimeCourses => _runtimeCoursesSource.AsObservableCache();
    internal SourceCache<RuntimeCourse, string> RuntimeCoursesSource => _runtimeCoursesSource;

    public SourceList<ScheduleProfile> Tables { get; } = new();

    public AppState()
    {

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
        Tables.Dispose();
    }
}