using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Extensions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum.CourseData;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.Registration;

public class CourseCatalogService : ICourseCatalogService
{
    private static readonly TimeSpan DefaultFetchTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan DefaultStreamTimeout = TimeSpan.FromSeconds(10);

    private readonly ILogger<CourseCatalogService> _logger;
    private readonly ICtuPageFactory _factory;
    private readonly IWebDriverService _webDriver;
    private readonly ICourseCatalogPage _catalogPage;

    public CourseCatalogService(
        ILogger<CourseCatalogService> logger,
        ICtuPageFactory factory,
        IWebDriverService webDriver)
    {
        _logger = logger;
        _factory = factory;
        _webDriver = webDriver;
        _catalogPage = _factory.GetPage<ICourseCatalogPage>(webDriver.MainTab);
    }

    public async Task<OperationResult> EnsureReadyAsync()
    {
        try
        {
            await _catalogPage.NavigateToAsync();
            await _catalogPage.WaitForReadyAsync();
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "fail to navigate to course catalog");
            return OperationResult.Failed("Trang chưa sẵn sàng", kind: OperationFailureReason.Network);
        }
    }

    public IObservable<List<QuickSelectCourse>> RequestSuggestionsStream(string query, TimeSpan? timeout = null)
    {
        var finalTimeout = timeout ?? DefaultStreamTimeout;
        return Observable.Create<List<QuickSelectCourse>>(async (observer, ct) =>
        {
            var subscription = _catalogPage.AutoCompleteQueryResponse
                .Timeout(finalTimeout)
                .Subscribe(observer);
            try
            {
                await _catalogPage.FillQueryAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "fail to get suggestions stream");
                observer.OnError(ex);
            }

            return subscription;
        });
    }

    public IObservable<Course> RequestCourseStream(string query, TimeSpan? timeout = null)
    {
        var finalTimeout = timeout ?? DefaultStreamTimeout;
        return Observable.Create<Course>(async (observer, ct) =>
        {
            var subscription = _catalogPage.CourseCatalogResponse
                .Select(course => course.ToCourse())
                .Where(course => course is not null &&
                                 course.Code.Equals(query, StringComparison.OrdinalIgnoreCase))
                .Select(x => x!)
                .Take(1)
                .Timeout(finalTimeout)
                .Subscribe(observer);

            try
            {
                await _catalogPage.FillQueryAsync(query);
                await _catalogPage.SearchAsync();
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("suggestions stream cancelled by User");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "fail to get course stream");
                observer.OnError(ex);
            }

            return subscription;
        });
    }

    public async Task<Course> FetchCourseAsync(string courseCode, CancellationToken cancellationToken = default,
        TimeSpan? timeout = null)
    {
        var finalTimeout = timeout ?? DefaultFetchTimeout;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(finalTimeout);
        
        try
        {
            var getCourseTask = _catalogPage.CourseCatalogResponse
                .Select(raw => raw.ToCourse())
                .Where(course => course is not null &&
                                 course.Code.Equals(courseCode, StringComparison.OrdinalIgnoreCase))
                .Select(x => x!)
                .FirstAsync()
                .ToTask(timeoutCts.Token);

            await _catalogPage.NavigateToAsync();
            await _catalogPage.WaitForReadyAsync();
            await _catalogPage.FillQueryAsync(courseCode);
            await _catalogPage.SearchAsync();

            return await getCourseTask;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Search for course {Code} timed out or cancelled.", courseCode);
            throw new TimeoutException($"Quá thời gian đáp ứng khi tìm môn học {courseCode}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to fetch course {Code}", courseCode);
            throw;
        }
    }

    public async Task<List<Course>> FetchCoursesBatchAsync(
        IEnumerable<string> courseCodes,
        int maxWorkers = 3,
        CancellationToken cancellationToken = default,
        TimeSpan? timeoutPerItem = null)
    {
        var results = new ConcurrentBag<Course>();
        var codesList = courseCodes.Distinct().ToList();

        if (!codesList.Any()) return new List<Course>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxWorkers,
            CancellationToken = cancellationToken
        };

        _logger.LogInformation("Bắt đầu cào {Count} môn học với {Workers} worker tabs...", codesList.Count, maxWorkers);

        await Parallel.ForEachAsync(codesList, parallelOptions, async (courseCode, ct) =>
        {
            try
            {
                var course = await FetchSingleCourseWithWorkerAsync(courseCode, ct, timeoutPerItem);
                results.Add(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker thất bại khi lấy mã {Code}. Sẽ bỏ qua mã này.", courseCode);
            }
        });

        return results.ToList();
    }


    private async Task<Course> FetchSingleCourseWithWorkerAsync(string courseCode, CancellationToken ct,
        TimeSpan? timeout)
    {
        var finalTimeout = timeout ?? DefaultFetchTimeout;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(finalTimeout);

        await using var workerTab = await _webDriver.CreateTabAsync();

        var workerPage = _factory.GetPage<ICourseCatalogPage>(workerTab);

        var getCourseTask = workerPage.CourseCatalogResponse
            .Select(raw => raw.ToCourse())
            .Where(course => course is not null &&
                             course.Code.Equals(courseCode, StringComparison.OrdinalIgnoreCase))
            .Select(x => x!)
            .FirstAsync()
            .ToTask(timeoutCts.Token);

        await workerPage.NavigateToAsync();
        await workerPage.WaitForReadyAsync();
        await workerPage.FillQueryAsync(courseCode);
        await workerPage.SearchAsync();

        return await getCourseTask;
    }
}