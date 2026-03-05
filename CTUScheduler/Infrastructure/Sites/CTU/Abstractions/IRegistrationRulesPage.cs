using System;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.Sites.Base;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum.Registration;
using CTUScheduler.Infrastructure.Sites.CTU.Response;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface IRegistrationRulesPage: ISitePage
{
    IObservable<CtuApiBody<RawRegistrationInformation>> RawRegistrationInformationResponse { get; }

    Task<(string userKey, string userUnit)> TryGetUserKeyAndUnitAsync();
}