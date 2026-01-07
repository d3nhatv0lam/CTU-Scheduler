using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Models;
using CTUScheduler.AppServices.Services.Registration;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Utils.Comparers;
using DynamicData;
using DynamicData.Aggregation;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.ScheduleService;

public class ScheduleManager: IScheduleManager, IDisposable
{
    private readonly CompositeDisposable  _disposables = new CompositeDisposable();
    private readonly SourceCache<RuntimeCourse, string> _coursesSource;
    private readonly SourceCache<ScheduleProfile, Guid> _profileSource;
    private readonly Subject<Unit> _refreshedSubject = new();
    private readonly ICourseCatalogService _catalogService;
    private readonly ILogger<ScheduleManager> _logger;
    
    public ScheduleManager(AppState appState, ICourseCatalogService catalog, ILogger<ScheduleManager> logger)
    {
        _coursesSource = appState.RuntimeCoursesSource;
        _profileSource = appState.ScheduleProfilesSource;
        _catalogService = catalog;
        _logger = logger;

        var currentProfileCountStream = _profileSource.Connect()
            .Count()
            .StartWith(_profileSource.Count)
            .DistinctUntilChanged();

        var maxProfileLimitStream = appState.UserSettingChanged
            .Select(x => x.MaxScheduleProfiles)
            .DistinctUntilChanged();

        ProfileUsageState = currentProfileCountStream.CombineLatest(
                maxProfileLimitStream,
                (count, limit) => new ProfileUsageState(count, limit)
            )
            .Replay(1)
            .RefCount();
 
        
        CoursesRefreshed = _refreshedSubject.AsObservable();
        _disposables.Add(_refreshedSubject);
    }

    public IObservable<IChangeSet<RuntimeCourse, string>> ConnectCourses() => _coursesSource.Connect();
    public IObservable<IChangeSet<ScheduleProfile, Guid>> ConnectProfiles() => _profileSource.Connect();
    public IObservable<ProfileUsageState> ProfileUsageState { get; }
    public IEnumerable<Course> GetCourseSnapshot() => _coursesSource
        .Items
        .Select(x => x.ToCourse())
        .ToList();
    public IEnumerable<ScheduleProfile> GetProfileSnapshot() => _profileSource.Items.ToList();
    public IObservable<Unit> CoursesRefreshed { get; }
    
    public void ImportSchedule(IEnumerable<Course> courses, IEnumerable<ScheduleProfile> profiles)
    {
        if (courses is null || profiles is null) return;
        ClearAll();
        _coursesSource.Edit(innerCache =>
        {
            ApplySnapshotToCourses(innerCache, courses, profiles);
        });
        _profileSource.AddOrUpdate(profiles);
    }

    public bool RegisterBlueprint(ScheduleBlueprint blueprint)
    {
        if (blueprint is null || !blueprint.IsConsistent)
            return false;
        _logger.LogInformation("Registering blueprint: {Id} with {Count} courses", blueprint.Metadata.Id, blueprint.Courses.Count);
        _coursesSource.Edit(innerCourses => 
        {
            ApplyBlueprintToCourses(innerCourses, blueprint);
        });
        _profileSource.AddOrUpdate(blueprint.Metadata);
        return true;
    }

    public bool RegisterBlueprint(IEnumerable<ScheduleBlueprint> blueprints)
    {
        var validList = blueprints?
            .Where(x => x.IsConsistent)
            .ToList();
        if (validList is null || validList.Count == 0) return false;
        
        _coursesSource.Edit(innerCourses =>
        {
            foreach (var blueprint in validList)
            {
                ApplyBlueprintToCourses(innerCourses, blueprint);
            }
        });
        _profileSource.AddOrUpdate(validList.Select(x => x.Metadata));
        return true;
    }

    public void UnregisterProfile(ScheduleProfile profile)
    {
        if (profile is null) return;
        var profileIdString = profile.Id.ToString();
        _coursesSource.Edit(innerCourse =>
        {
            var keysToRemove = new List<string>();
            foreach (var (code, group) in profile.SavedCourseGroupKeys)
            {
                var lookup = innerCourse.Lookup(code);
                if (!lookup.HasValue) continue;
                
                var runtimeCourse = lookup.Value;

                bool isEmpty = runtimeCourse.UnregisterSection(group,profileIdString);
                if (isEmpty)
                {
                    runtimeCourse.Dispose();
                    keysToRemove.Add(code);
                }
            }

            if (keysToRemove.Count > 0)
                innerCourse.RemoveKeys(keysToRemove);
        });
        _profileSource.Remove(profile.Id);
        
    }
    
    public async Task RefreshCoursesAsync(CancellationToken token = default)
    {
        var allCourses = _coursesSource.Items.ToList();
        _logger.LogInformation("Starting refresh for {Count} courses...", allCourses.Count);
        foreach (var runtimeCourse in allCourses)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var serverCourse = await _catalogService
                    .FetchCourseAsync(runtimeCourse.Code, token)
                    .ConfigureAwait(false);
                runtimeCourse.Merge(serverCourse);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Refresh operation cancelled.");
                throw; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh course: {Code}", runtimeCourse.Code);
            }
        }
        _logger.LogInformation("Finished refreshing courses.");
    }

    public void ClearAll()
    {
        foreach (var course in _coursesSource.Items) course.Dispose();
        _coursesSource.Clear();
        _profileSource.Clear();
    }
    
    private static void ApplyBlueprintToCourses(
        ISourceUpdater<RuntimeCourse, string> innerCache, 
        ScheduleBlueprint blueprint)
    {
        Debug.Assert(innerCache is not null);
        Debug.Assert(blueprint is not null);
        
        var catalogDict = blueprint.Courses.ToDictionary(c => c.Code);

        foreach (var (courseCode, groupCode) in blueprint.Metadata.SavedCourseGroupKeys)
        {
            if (!catalogDict.TryGetValue(courseCode, out var courseDto)) continue;
            
            var lookup = innerCache.Lookup(courseCode);
            RuntimeCourse runtimeCourse;

            if (lookup.HasValue)
            {
                runtimeCourse = lookup.Value;
            }
            else
            {
                runtimeCourse = new RuntimeCourse(courseDto);
                innerCache.AddOrUpdate(runtimeCourse);
            }
            
            var sectionToRegister = courseDto.Sections.FirstOrDefault(s => s.Group == groupCode);
            if (sectionToRegister is not null)
            {
                runtimeCourse.RegisterSection(sectionToRegister, blueprint.Metadata.Id.ToString());
            }
        }
    }
    
    private static void ApplySnapshotToCourses(
        ISourceUpdater<RuntimeCourse, string> innerCache, 
        IEnumerable<Course> courses,
        IEnumerable<ScheduleProfile> profiles)
    {
        Debug.Assert(innerCache is not null);
        Debug.Assert(courses is not null);
        Debug.Assert(profiles is not null);
        
        var catalogDict = courses.ToDictionary(c => c.Code);
        foreach (var profile in profiles)
        {
            foreach (var (courseCode, groupCode) in profile.SavedCourseGroupKeys)
            {
                if (!catalogDict.TryGetValue(courseCode, out var courseDto)) continue;
            
                var lookup = innerCache.Lookup(courseCode);
                RuntimeCourse runtimeCourse;

                if (lookup.HasValue)
                {
                    runtimeCourse = lookup.Value;
                }
                else
                {
                    runtimeCourse = new RuntimeCourse(courseDto);
                    innerCache.AddOrUpdate(runtimeCourse);
                }
            
                var sectionToRegister = courseDto.Sections.FirstOrDefault(s => s.Group == groupCode);
                if (sectionToRegister is not null)
                {
                    runtimeCourse.RegisterSection(sectionToRegister, profile.Id.ToString());
                }
            }
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
        _logger.LogInformation(nameof(ScheduleManager) + " has been disposed.");
    }
}