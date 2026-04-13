using System;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.DriverCore.Response;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.DriverCore.Refactor;

public interface IWebTab : IAsyncDisposable
{
    IPage NativePage { get; }
    
    string CurrentUrl { get; }
    IObservable<NetworkPacket> JsonResponse { get; }
    IObservable<string> UrlChanges { get; }
    IObservable<DialogInfo> AlertReceived { get; }
    IObservable<DialogInfo> ConfirmReceived { get; }
    IObservable<DialogInfo> PromptReceived { get; }
}