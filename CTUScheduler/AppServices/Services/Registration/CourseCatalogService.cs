using System;
using System.Collections.Generic;
using System.Reactive.Linq;
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
   
   public IObservable<List<QuickSelectCourse>> QuickSelectCourseChanges =>
        _catalogPage.AutoCompleteQueryResponse
           .Where(x => x.IsSuccess)
           .Select(x => x.Content)
           .Where(content => content is not null)!;

   public IObservable<Course> CourseChanges =>
       _catalogPage.CourseCatalogResponse
           .Where(x => x.IsSuccess)
           .Select(x => x.Content)
           .Where(content => content is not null)
           .Select(raw => raw!.ToCourse())!;
   
   public CourseCatalogService(ILogger<CourseCatalogService> logger, ICtuSitePageFactory factory)
   {
      _logger = logger;
       _factory = factory;
       
       _catalogPage = factory.GetPage<ICourseCatalogPage>();
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
}