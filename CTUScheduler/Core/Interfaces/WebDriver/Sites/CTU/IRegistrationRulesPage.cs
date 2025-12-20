using System;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Raw;
using CTUScheduler.Core.Models.WebResponse;

namespace CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;

public interface IRegistrationRulesPage: ISitePage
{
    IObservable<CtuApiBody<RawRegistrationInformation>> RawRegistrationInformationResponse { get; }

    Task<(string userKey, string userUnit)> TryGetUserKeyAndUnitAsync();
}