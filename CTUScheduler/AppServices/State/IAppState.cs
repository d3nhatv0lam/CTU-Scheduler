using System;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Settings;
using DynamicData;

namespace CTUScheduler.AppServices.State;

public interface IAppState
{
    IObservable<UserSettings> UserSettingChanged { get; }
    UserSettings CurrentSettings { get; set; }
    
}