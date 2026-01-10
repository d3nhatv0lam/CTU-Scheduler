using System;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.AppServices.Services.Registration;

public interface IRegistrationRulesService
{
    IObservable<RegistrationInformation> RegistrationInfoChanges { get; }
    Task<OperationResult> NavigateToAsync();
}