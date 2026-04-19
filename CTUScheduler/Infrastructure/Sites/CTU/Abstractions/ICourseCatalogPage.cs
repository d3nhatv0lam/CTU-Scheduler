using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.Sites.Base;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum.CourseData;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface ICourseCatalogPage: ISitePageRefactor
{
   IObservable<List<QuickSelectCourse>> AutoCompleteQueryResponse { get; }
   IObservable<RawCourse> CourseCatalogResponse { get; }
   
   Task FillQueryAsync(string query);
   Task SearchAsync();
}