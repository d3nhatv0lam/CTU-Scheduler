using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.Sites.Base;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface ICourseCatalogPage: ISitePage
{
   IObservable<List<QuickSelectDmhpCourse>> AutoCompleteQueryResponse { get; }
   IObservable<RawDmhpPayload> CourseCatalogResponse { get; }
   
   Task FillQueryAsync(string query);
   Task SearchAsync();
}