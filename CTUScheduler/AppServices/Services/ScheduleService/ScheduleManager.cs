using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Models;
using CTUScheduler.AppServices.Services.UserSettingService;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using DynamicData;
using DynamicData.Aggregation;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.ScheduleService;

public class ScheduleManager : IScheduleManager, IDisposable
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly SourceCache<RuntimeCourse, string> _coursesSource;
    private readonly SourceCache<ScheduleProfile, Guid> _profileSource;
    private readonly BehaviorSubject<bool> _isRefreshingSubject;
    private readonly ICourseCatalogService _catalogService;
    private readonly ILogger<ScheduleManager> _logger;
    private bool _isDisposed;

    public ScheduleManager(AppState appState, IUserSettingService settingService, ICourseCatalogService catalog,
        ILogger<ScheduleManager> logger)
    {
        _coursesSource = appState.RuntimeCoursesSource;
        _profileSource = appState.ScheduleProfilesSource;
        _catalogService = catalog;
        _logger = logger;

        _isRefreshingSubject = new BehaviorSubject<bool>(false)
            .DisposeWith(_disposables);

        var currentProfileCountStream = _profileSource.Connect()
            .Count()
            .StartWith(_profileSource.Count)
            .DistinctUntilChanged();

        var maxProfileLimitStream = settingService.SettingsChanged
            .Select(x => x.General.MaxScheduleProfiles)
            .DistinctUntilChanged();

        ProfileUsageState = currentProfileCountStream.CombineLatest(
                maxProfileLimitStream,
                (count, limit) => new ProfileUsageState(count, limit)
            )
            .Replay(1)
            .RefCount();


        IsRefreshing = _isRefreshingSubject.AsObservable();
    }

    public IObservable<IChangeSet<RuntimeCourse, string>> ConnectCourses() => _coursesSource.Connect();
    public IObservable<IChangeSet<ScheduleProfile, Guid>> ConnectProfiles() => _profileSource.Connect();
    public IObservable<ProfileUsageState> ProfileUsageState { get; }

    public IEnumerable<Course> GetCoursesSnapshot() => _coursesSource
        .Items
        .Select(x => x.ToCourse())
        .ToList();

    public Course? GetCourseSnapshot(string courseCode)
    {
        var lookup = _coursesSource.Lookup(courseCode);
        return lookup.HasValue ? lookup.Value.ToCourse() : null;
    }

    public IEnumerable<ScheduleProfile> GetProfileSnapshot() => _profileSource.Items.ToList();
    public IObservable<bool> IsRefreshing { get; }

    public void ImportSchedule(IEnumerable<Course> courses, IEnumerable<ScheduleProfile> profiles)
    {
        if (courses is null || profiles is null) return;

        _profileSource.Edit(innerCache =>
        {
            innerCache.Clear();
            innerCache.AddOrUpdate(profiles);
        });

        var oldCourses = _coursesSource.Items.ToList();
        _coursesSource.Edit(innerCache =>
        {
            innerCache.Clear();
            ApplySnapshotToCourses(innerCache, courses, profiles);
        });

        foreach (var course in oldCourses)
        {
            course.Dispose();
        }
    }

    public bool RegisterBlueprint(ScheduleBlueprint blueprint)
    {
        if (blueprint is null || !blueprint.IsConsistent)
            return false;
        _logger.LogInformation("Registering blueprint: {Id} with {Count} courses", blueprint.Metadata.Id,
            blueprint.Courses.Count);
        _coursesSource.Edit(innerCourses => { ApplyBlueprintToCourses(innerCourses, blueprint); });
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

                bool isEmpty = runtimeCourse.UnregisterSection(group, profileIdString);
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
        if (_isRefreshingSubject.Value) return;
        _isRefreshingSubject.OnNext(true);
        
        try
        {
            var coursesDict = _coursesSource.Items.ToDictionary(x => x.Code);
            _logger.LogInformation("Starting refresh for {Count} courses...", coursesDict.Count);

            try
            {
                var serverCourses = _catalogService.FetchCoursesBatchAsync(
                    coursesDict.Keys,
                    maxWorkers: 2,
                    cancellationToken: token);

                await foreach (var course in serverCourses.ConfigureAwait(false))
                {
                    if (coursesDict.TryGetValue(course.Code, out var runtimeCourse))
                    {
                        runtimeCourse.Merge(course);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Refresh operation cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh courses batch.");
            }
            
            // cách cũ chạy từng course
            // var allCourses = _coursesSource.Items.ToList();
            // foreach (var runtimeCourse in allCourses)
            // {
            //     token.ThrowIfCancellationRequested();
            //     try
            //     {
            //         var serverCourse = await _catalogService
            //             .FetchCourseAsync(runtimeCourse.Code, token)
            //             .ConfigureAwait(false);
            //         runtimeCourse.Merge(serverCourse);
            //     }
            //     catch (OperationCanceledException)
            //     {
            //         _logger.LogDebug("Refresh operation cancelled.");
            //         throw;
            //     }
            //     catch (Exception ex)
            //     {
            //         _logger.LogError(ex, "Failed to refresh course: {Code}", runtimeCourse.Code);
            //     }
            // }
        }
        finally
        {
            _logger.LogInformation("Finished refreshing courses.");
            _isRefreshingSubject.OnNext(false);
        }
    }

    public void ClearAll()
    {
        var coursesSnapshot = _coursesSource.Items.ToList();
        _profileSource.Clear();
        _coursesSource.Clear();
        foreach (var course in coursesSnapshot) course.Dispose();
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
        if (_isDisposed) return;
        _isDisposed = true;

        _disposables.Dispose();
        _logger.LogInformation(nameof(ScheduleManager) + " has been disposed.");
    }
}