using Microsoft.Playwright;
using RxAmethyst.PlaywrightWrapper.Models;

namespace RxAmethyst.PlaywrightWrapper.Abstractions;

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