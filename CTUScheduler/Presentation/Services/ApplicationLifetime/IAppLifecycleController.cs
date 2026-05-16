namespace CTUScheduler.Presentation.Services.ApplicationLifetime;

public interface IAppLifecycleController
{
    void NotifyStarted();
    void NotifyStopping();
    void NotifyStopped();
}