using System;

namespace CTUScheduler.Presentation.Services.ApplicationLifetime;

public interface IAppLifecycleController
{
    void NotifyStarted();
    void NotifyStopping();
    void NotifyStopped();
    event Action? ShutdownRequested;
}