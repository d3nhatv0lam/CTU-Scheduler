using System;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Core.Models.Settings;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public interface IUserSessionService
{
    /// <summary>
    /// Context in Saved
    /// </summary>
    IObservable<RegistrationContext?> LocalContextChanged { get; }

    /// <summary>
    /// If has saved, return saved context, else return server context if has, otherwise return null
    /// </summary>
    RegistrationContext? CurrentContext { get; }

    IObservable<RegistrationInformation?> RegistrationInfoChanged { get; }
    RegistrationInformation? CurrentRegistrationInfo { get; }
    IObservable<bool> IsReadonly { get; }
    IObservable<DateTimeOffset?> LastSaved { get; }
    void SetLocalContext(RegistrationContext context);
    void UpdateServerInfo(RegistrationInformation info);
    void NotifyModified();
    void SetLastModified(DateTimeOffset lastSaved);
}