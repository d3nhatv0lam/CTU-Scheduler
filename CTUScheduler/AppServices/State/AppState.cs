using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.TeachingPlan;
using DynamicData;

namespace CTUScheduler.AppServices.State;

public class AppState : IAppState, IDisposable
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private readonly BehaviorSubject<UserPreferences> _userSettingsSubject = new (
        new UserPreferences()
    );

    private readonly BehaviorSubject<TeachingPlanData?> _teachingPlanSubject = new(null);

    private readonly SourceCache<RuntimeCourse, string> _runtimeCoursesSource = new(x => x.Code);
    private readonly SourceCache<ScheduleProfile, Guid> _scheduleProfilesSource = new(x => x.Id);

    internal SourceCache<RuntimeCourse, string> RuntimeCoursesSource => _runtimeCoursesSource;
    internal SourceCache<ScheduleProfile, Guid> ScheduleProfilesSource => _scheduleProfilesSource;
    internal BehaviorSubject<UserPreferences> UserSettingsSubject => _userSettingsSubject;
    internal BehaviorSubject<TeachingPlanData?> TeachingPlanSubject => _teachingPlanSubject;

    public IObservable<TeachingPlanData?> TeachingPlanChanged => _teachingPlanSubject.AsObservable();
    public TeachingPlanData? TeachingPlan => _teachingPlanSubject.Value;

    public void SetTeachingPlan(TeachingPlanData? data)
    {
        _teachingPlanSubject.OnNext(data);
    }

    public AppState()
    {
        _runtimeCoursesSource.DisposeWith(_disposables);
        _scheduleProfilesSource.DisposeWith(_disposables);
        _userSettingsSubject.DisposeWith(_disposables);
        _teachingPlanSubject.DisposeWith(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}