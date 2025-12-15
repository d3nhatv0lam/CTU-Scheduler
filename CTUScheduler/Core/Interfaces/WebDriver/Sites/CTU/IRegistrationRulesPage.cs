using System;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;

namespace CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;

public interface IRegistrationRulesPage: ISitePage
{
    IObservable<RegistrationInformation> RegistrationInformationResponse { get; }
}