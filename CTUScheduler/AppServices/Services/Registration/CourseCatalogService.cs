using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.Registration;

public class CourseCatalogService : ICourseCatalogService
{
    private static readonly TimeSpan DefaultFetchTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan DefaultStreamTimeout = TimeSpan.FromSeconds(10);
    
    private readonly ILogger<CourseCatalogService> _logger;
    private readonly ICtuSitePageFactory _factory;
    private readonly ICourseCatalogPage _catalogPage;
    
    public CourseCatalogService(
        ILogger<CourseCatalogService> logger, 
        ICtuSitePageFactory factory)
    {
        _logger = logger;
        _factory = factory;
        _catalogPage = factory.GetPage<ICourseCatalogPage>();
    }

    public async Task<OperationResult> EnsureReadyAsync()
    {
        try
        {
            if (await _catalogPage.IsActive.FirstAsync())
                return OperationResult.Success();

            await _catalogPage.NavigateToAsync();
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "fail to navigate to course catalog");
            return OperationResult.Failed("trang chưa sẵn sàng", OperationFailureReason.Network);
        }
    }

    public IObservable<List<QuickSelectCourse>> GetSuggestionsStream(string query, TimeSpan? timeout = null)
    {
        var finalTimeout = timeout ?? DefaultStreamTimeout;
        return Observable.Create<List<QuickSelectCourse>>(async (observer, ct) =>
        {
            var subscription = _catalogPage.AutoCompleteQueryResponse
                .Where(x => x is { IsSuccess: true, Content: not null })
                .Select(x => x.Content!)
                .Timeout(finalTimeout)
                .Subscribe(observer);

            try
            {
                await _catalogPage.FillQueryAsync(query, ct);
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

    public IObservable<Course> GetCourseStream(string query, TimeSpan? timeout = null)
    {
        var finalTimeout = timeout ?? DefaultStreamTimeout;
        return Observable.Create<Course>(async (observer, ct) =>
        {
            var subscription = _catalogPage.CourseCatalogResponse
                .Where(x => x is { IsSuccess: true, Content: not null })
                .Select(x => x.Content?.ToCourse())
                .Where(course => course is not null && 
                                 course.Code.Equals(query, StringComparison.OrdinalIgnoreCase))
                .Select(x => x!)
                .Take(1)
                .Timeout(finalTimeout)
                .Subscribe(observer);

            try
            {
                await _catalogPage.FillQueryAsync(query, ct);
                await _catalogPage.SearchAsync(ct);
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
        if (!await _catalogPage.IsActive.FirstAsync())
            throw new InvalidOperationException("Course catalog page is not active");
        
        var finalTimeout = timeout ?? DefaultFetchTimeout;
        
        try
        {
            return await GetCourseStream(courseCode,finalTimeout)
                .FirstAsync() 
                .ToTask(cancellationToken);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Search for course {Code} timed out.", courseCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to fetch course {Code}", courseCode);
            throw;
        }
    }
}