using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.DriverCore.Refactor;
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
            catch (OperationCanceledException)
            {
                /* ignore */
                _logger.LogDebug("suggestions stream cancelled by User");
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
    
    public async Task<Course> FetchCourseAsync(string courseCode, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        var finalTimeout = timeout ?? DefaultFetchTimeout;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(finalTimeout);
        
        await using var workerTab = await _webDriver.CreateTabAsync();
        
        try
        {
            var workerPage = _factory.GetPage<ICourseCatalogPage>(workerTab);
            
            // Lắng nghe tín hiệu kết quả trên tab phụ (chỉ bắt Data đầu tiên hợp lệ)
            var getCourseTask = workerPage.CourseCatalogResponse
                .Select(raw => raw.ToCourse())
                .Where(course => course is not null && 
                                 course.Code.Equals(courseCode, StringComparison.OrdinalIgnoreCase))
                .Select(x => x!)
                .FirstAsync()
                .ToTask(timeoutCts.Token);

            // Điều khiển tab phụ Navigate và bấm tìm kiếm
            await workerPage.NavigateToAsync();
            await workerPage.FillQueryAsync(courseCode);
            await workerPage.SearchAsync();

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
}