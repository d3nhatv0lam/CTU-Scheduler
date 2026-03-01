using System;
using System.Collections.Generic;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Infrastructure.Sites.Base;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum.CourseData;
using CTUScheduler.Infrastructure.Sites.CTU.Response;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface ICourseCatalogPage: ISitePage, ISearchable
{
   IObservable<CtuApiBody<List<QuickSelectCourse>>> AutoCompleteQueryResponse { get; }
   IObservable<CtuApiBody<RawCourse>> CourseCatalogResponse { get; }
}