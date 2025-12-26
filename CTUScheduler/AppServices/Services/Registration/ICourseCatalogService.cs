using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.AppServices.Services.Registration;

public interface ICourseCatalogService
{
    IObservable<List<QuickSelectCourse>> QuickSelectCourseChanges { get; }
    IObservable<Course> CourseChanges { get; }
    Task<OperationResult> NavigateToAsync();
    
    Task FillQueryAsync(string query);
    Task SearchAsync();
    Task<Course> FetchCourseAsync(string courseCode);
}