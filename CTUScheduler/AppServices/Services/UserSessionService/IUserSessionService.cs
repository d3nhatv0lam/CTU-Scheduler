using System;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.UserSaves;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public interface IUserSessionService
{
    /// <summary>
    /// Context in Saved
    /// </summary>
    IObservable<RegistrationContext?> LocalContext { get; }
    /// <summary>
    /// If has saved, return saved context, else return server context if has, otherwise return null
    /// </summary>
    RegistrationContext? CurrentContext { get; }
    IObservable<RegistrationInformation?> RegistrationInfo { get; }
    RegistrationInformation? CurrentRegistrationInfo { get; }
    IObservable<bool> IsReadonly { get; }
    IObservable<DateTimeOffset?> LastSaved { get; }
    void SetContext(RegistrationContext context);
    void UpdateServerInfo(RegistrationInformation info);
    void NotifyModified();
    void SetLastModified(DateTimeOffset lastSaved);
}