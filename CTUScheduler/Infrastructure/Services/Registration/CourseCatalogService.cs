using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Extensions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.Registration;

public class CourseCatalogService : ICourseCatalogService
{
    private static readonly int SequentialThreshold = 15;
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

    // public async Task<OperationResult> EnsureReadyAsync() => OperationResult.Success();

    public async Task<OperationResult> EnsureReadyAsync()
    {
        try
        {
            await _catalogPage.NavigateToAsync();
            await _catalogPage.WaitForReadyAsync();
            return OperationResult.Success();
        }
        catch (NoInternetException)
        {
            return OperationResult.Failed("Không có kết nối mạng!", kind: OperationFailureReason.Network);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "fail to navigate to course catalog");
            return OperationResult.FromException(ex, "Trang chưa sẵn sàng", kind: OperationFailureReason.System);
        }
    }

    public IObservable<List<QuickSelectDmhpCourse>> RequestSuggestionsStream(string query, TimeSpan? timeout = null)
    {
        var finalTimeout = timeout ?? DefaultStreamTimeout;
        return Observable.Create<List<QuickSelectDmhpCourse>>(async (observer, ct) =>
        {
            var subscription = _catalogPage.AutoCompleteQueryResponse
                .Take(1)
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
                .Where(course => course is null ||
                                 course.Code.Equals(query, StringComparison.OrdinalIgnoreCase))
                .Take(1)
                .Timeout(finalTimeout)
                .Subscribe(course =>
                {
                    if (course is not null)
                        observer.OnNext(course);
                    else
                        observer.OnError(new Exception($"Không tìm thấy môn học có mã: {query}"));
                }, observer.OnError, observer.OnCompleted);

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
        return await ExecuteCourseFetchCoreAsync(_catalogPage, courseCode, cancellationToken, timeout);
    }

    public async IAsyncEnumerable<Course> FetchCoursesBatchAsync(
        IEnumerable<string> courseCodes,
        int maxWorkers = 2,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        TimeSpan? timeoutPerItem = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxWorkers, 1);

        var codesList = courseCodes.Distinct().ToList();

        if (codesList.Count == 0)
            yield break;

        if (maxWorkers == 1 || codesList.Count <= SequentialThreshold)
        {
            _logger.LogDebug("Has {courses} courses. Fetching courses in sequential mode", codesList.Count);
            await foreach (var course in FetchCoursesSequentiallyAsyncEnumerable(codesList, cancellationToken,
                               timeoutPerItem))
            {
                yield return course;
            }

            yield break;
        }

        // chạy đa tab
        var queue = new ConcurrentQueue<string>(codesList);
        var channel = Channel.CreateUnbounded<Course>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var actualWorkers = Math.Min(codesList.Count, maxWorkers);

        _logger.LogInformation("Bắt đầu cào {Count} môn học với {Workers} worker tabs...", codesList.Count,
            actualWorkers);

        var workerTasks = new List<Task>(actualWorkers);

        for (int i = 0; i < actualWorkers; i++)
        {
            var workerId = i + 1;
            var existingPage = i == 0 ? _catalogPage : null;

            workerTasks.Add(ProcessQueueWithWorkerAsync(
                workerId,
                queue,
                channel.Writer,
                cancellationToken,
                timeoutPerItem,
                existingPage: existingPage));
        }

        _ = Task.WhenAll(workerTasks).ContinueWith(t =>
        {
            Exception? ex = null;
            if (t.IsFaulted)
                ex = t.Exception!.Flatten().InnerException ?? t.Exception;
            else if (t.IsCanceled)
                ex = new OperationCanceledException("Fetch courses cancelled");

            channel.Writer.TryComplete(ex);
        }, TaskContinuationOptions.ExecuteSynchronously);

        // Đẩy từng Course ra ngay khi có
        await foreach (var course in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return course;
        }
    }

    private async IAsyncEnumerable<Course> FetchCoursesSequentiallyAsyncEnumerable(
        List<string> courseCodes,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        TimeSpan? timeout)
    {
        foreach (var courseCode in courseCodes)
        {
            if (cancellationToken.IsCancellationRequested) yield break;

            Course? course = null;

            try
            {
                course = await FetchCourseAsync(courseCode, cancellationToken, timeout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Thất bại khi lấy mã {Code} (tuần tự). Sẽ bỏ qua mã này.", courseCode);
            }

            if (course is not null)
            {
                _logger.LogDebug("Cào thành công mã {Code}", course.Code);
                yield return course;
            }
        }
    }

    private async Task ProcessQueueWithWorkerAsync(
        int workerId,
        ConcurrentQueue<string> queue,
        ChannelWriter<Course> writer,
        CancellationToken cancellationToken,
        TimeSpan? timeoutPerItem,
        ICourseCatalogPage? existingPage = null)
    {
        IAsyncDisposable? tabToDispose = null;
        ICourseCatalogPage workerPage;

        if (existingPage is not null)
        {
            workerPage = existingPage;
            _logger.LogDebug("[Worker {Id}] Đang sử dụng Main Tab tái sử dụng.", workerId);
        }
        else
        {
            var workerTab = await _webDriver.CreateTabAsync();
            tabToDispose = workerTab;
            workerPage = _factory.GetPage<ICourseCatalogPage>(workerTab);
            _logger.LogDebug("[Worker {Id}] Đã tạo Tab mới thành công.", workerId);
        }

        try
        {
            while (queue.TryDequeue(out var courseCode))
            {
                if (cancellationToken.IsCancellationRequested) break;
                try
                {
                    var course = await ExecuteCourseFetchCoreAsync(
                        workerPage,
                        courseCode,
                        cancellationToken,
                        timeoutPerItem);

                    await writer.WriteAsync(course, cancellationToken);

                    _logger.LogDebug("[Worker {Id}] Cào thành công mã {Code}", workerId, courseCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Worker {Id}] Thất bại khi lấy mã {Code}. Sẽ bỏ qua mã này.", workerId,
                        courseCode);
                }
            }
        }
        finally
        {
            if (tabToDispose is not null)
            {
                await tabToDispose.DisposeAsync();
                _logger.LogDebug("[Worker {Id}] Đã đóng tab và giải phóng tài nguyên.", workerId);
            }
            else
            {
                _logger.LogDebug("[Worker {Id}] Trả lại Main Tab.", workerId);
            }
        }
    }

    private async Task<Course> ExecuteCourseFetchCoreAsync(
        ICourseCatalogPage page,
        string courseCode,
        CancellationToken cancellationToken,
        TimeSpan? timeout)
    {
        var finalTimeout = timeout ?? DefaultFetchTimeout;

        try
        {
            await page.NavigateToAsync();
            await page.WaitForReadyAsync();
            await page.CheckSessionAndThrowAsync();

            var getCourseTask = page.CourseCatalogResponse
                .Select(raw => raw.ToCourse())
                .Where(course => course is not null &&
                                 course.Code.Equals(courseCode, StringComparison.OrdinalIgnoreCase))
                .Select(x => x!)
                .FirstAsync()
                .Timeout(finalTimeout)
                .ToTask(cancellationToken);

            await page.FillQueryAsync(courseCode);
            await page.SearchAsync();

            return await getCourseTask;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Search for course {Code} timed out or cancelled on page/tab.", courseCode);
            throw new TimeoutException($"Quá thời gian đáp ứng khi tìm môn học {courseCode}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to fetch course {Code}", courseCode);
            throw;
        }
    }
}