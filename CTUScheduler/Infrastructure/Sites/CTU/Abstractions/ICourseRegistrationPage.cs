using System;
using System.Collections.Generic;
using CTUScheduler.Infrastructure.Sites.Base;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface ICourseRegistrationPage: ISitePage
{
    IObservable<List<RawDkhpPayload>> CourseRegistrationResponse { get; }
}