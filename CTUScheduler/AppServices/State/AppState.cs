using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Settings;
using DynamicData;

namespace CTUScheduler.AppServices.State;

public class AppState : IAppState, IDisposable
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly BehaviorSubject<UserSettings> _userSettingsSubject;
    private readonly SourceCache<RuntimeCourse, string> _runtimeCoursesSource = new(x => x.Code);
    private readonly SourceCache<ScheduleProfile, Guid> _scheduleProfilesSource  = new(x => x.Id);
    
    internal SourceCache<RuntimeCourse, string> RuntimeCoursesSource => _runtimeCoursesSource;
    internal SourceCache<ScheduleProfile, Guid> ScheduleProfilesSource => _scheduleProfilesSource;
    
    public IObservable<UserSettings> UserSettingChanged { get; }
    
    public UserSettings CurrentSettings 
    {
        get => _userSettingsSubject.Value;
        set => _userSettingsSubject.OnNext(value);
    }
    public AppState()
    {
        _userSettingsSubject = new BehaviorSubject<UserSettings>(
            new UserSettings()
        );
        UserSettingChanged = _userSettingsSubject.AsObservable();
        
        _userSettingsSubject.DisposeWith(_disposables);
        _runtimeCoursesSource.DisposeWith(_disposables);
        _scheduleProfilesSource.DisposeWith(_disposables);
    }
    
    public void Dispose()
    {
       _disposables.Dispose();
    }
}