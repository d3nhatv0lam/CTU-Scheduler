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

    IObservable<RegistrationInformation?> RegistrationInfo { get; }
    
    IObservable<bool> IsReadonly { get; }
    
    IObservable<DateTimeOffset?> LastSaved { get; }

    void SetContext(RegistrationContext context);
    void UpdateLiveInfo(RegistrationInformation info);
    void NotifySaved();
    void SetLastSaved(DateTimeOffset lastSaved);
}