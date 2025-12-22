using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using CTUScheduler.Core.Models.WebResponse;

namespace CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;

public interface ICourseCatalogPage: ISitePage, ISearchable
{
   IObservable<CtuApiBody<List<QuickSelectCourse>>> AutoCompleteQueryResponse { get; }
   IObservable<CtuApiBody<RawCourse>> CourseCatalogResponse { get; }
}