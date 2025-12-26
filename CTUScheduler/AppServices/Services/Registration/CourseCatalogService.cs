using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.Registration;

public class CourseCatalogService: ICourseCatalogService
{
   private readonly ILogger<CourseCatalogService> _logger;
   private readonly ICtuSitePageFactory _factory;
   private readonly ICourseCatalogPage _catalogPage;

   public IObservable<List<QuickSelectCourse>> QuickSelectCourseChanges { get; }
   public IObservable<Course> CourseChanges { get; }
   
   public CourseCatalogService(ILogger<CourseCatalogService> logger, ICtuSitePageFactory factory)
   {
      _logger = logger;
       _factory = factory;
       _catalogPage = factory.GetPage<ICourseCatalogPage>();
       
       QuickSelectCourseChanges = _catalogPage.AutoCompleteQueryResponse
           .Where(x => x is { IsSuccess: true, Content: not null })
           .Select(x => x.Content!);
       
       CourseChanges = _catalogPage.CourseCatalogResponse
           .Where(x => x is { IsSuccess: true, Content: not null })
           .Select(x => x.Content?.ToCourse())
           .Where(course => course is not null)
           .Select(x => x!);
   }

   public async Task<OperationResult> NavigateToAsync()
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
           _logger.LogWarning(ex,"fail to navigate to course catalog");
           return OperationResult.Failed("trang chưa sẵn sàng",OperationFailureReason.Network);
       }
   }

   public async Task FillQueryAsync(string query)
   {
       try
       {
           await _catalogPage.FillQueryAsync(query);
       }
       catch (Exception ex)
       {
           _logger.LogWarning(ex,"fail to fill query");
       }
   }

   public async Task SearchAsync()
   {
       try
       {
           await _catalogPage.SearchAsync();
       }
       catch (Exception ex)
       {
           _logger.LogWarning(ex,"fail to search");
       }
   }

   public async Task<Course> FetchCourseAsync(string courseCode)
   {
       if (!await _catalogPage.IsActive.FirstAsync())
           throw new InvalidOperationException("Course catalog page is not active");
       try
       {
           var task = CourseChanges
               .Where(c => string.Equals(c.Code, courseCode, StringComparison.OrdinalIgnoreCase))
               .Timeout(TimeSpan.FromSeconds(5))
               .FirstAsync()
               .ToTask()
               .ConfigureAwait(false);

           await FillQueryAsync(courseCode)
               .ConfigureAwait(false);
           await SearchAsync()
               .ConfigureAwait(false);
           
           var course = await task;
           return course;
       }
       catch (TimeoutException)
       {
           _logger.LogWarning("Timeout waiting for course {Code}", courseCode);
           throw new TimeoutException($"Search for course {courseCode} timed out.");
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Fail to fetch course {Code}", courseCode);
           throw;
       }
   }
}