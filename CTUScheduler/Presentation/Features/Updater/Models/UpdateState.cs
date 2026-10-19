namespace CTUScheduler.Presentation.Features.Updater.Models;

public enum UpdateState
{
    Idle,
    Checking,
    Available,
    Downloading,
    UpToDate,
    Failed
}