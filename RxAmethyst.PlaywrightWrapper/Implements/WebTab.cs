using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Playwright;
using RxAmethyst.PlaywrightWrapper.Abstractions;
using RxAmethyst.PlaywrightWrapper.Models;

namespace RxAmethyst.PlaywrightWrapper.Implements;

public class WebTab : IWebTab
{
    private readonly CompositeDisposable _disposables = new();
    private readonly Subject<NetworkPacket> _jsonResponseSubject = new();
    private readonly BehaviorSubject<string> _urlSubject;
    private readonly IPage _page;
    private readonly Subject<DialogInfo> _alertSubject = new();
    private readonly Subject<DialogInfo> _confirmSubject = new();
    private readonly Subject<DialogInfo> _promptSubject = new();

    private WebTab(IPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        _page = page;
        _urlSubject = new BehaviorSubject<string>(page.Url);

        JsonResponse = _jsonResponseSubject.AsObservable();
        UrlChanges = _urlSubject.AsObservable();
        AlertReceived = _alertSubject.AsObservable();
        ConfirmReceived = _confirmSubject.AsObservable();
        PromptReceived = _promptSubject.AsObservable();

        _jsonResponseSubject.DisposeWith(_disposables);
        _urlSubject.DisposeWith(_disposables);
        _alertSubject.DisposeWith(_disposables);
        _confirmSubject.DisposeWith(_disposables);
        _promptSubject.DisposeWith(_disposables);

        SetupReactiveEvents();
        SetupDialogEvents();
    }

    public IPage NativePage => _page;
    public string CurrentUrl => _page.Url;
    public IObservable<NetworkPacket> JsonResponse { get; }
    public IObservable<string> UrlChanges { get; }
    public IObservable<DialogInfo> AlertReceived { get; }
    public IObservable<DialogInfo> ConfirmReceived { get; }
    public IObservable<DialogInfo> PromptReceived { get; }

    public static async Task<WebTab> CreateAsync(IPage page)
    {
        var tab = new WebTab(page);

        await tab.OptimizePageLoadAsync();

        return tab;
    }

    private async Task OptimizePageLoadAsync()
    {
        var routeTask = _page.RouteAsync("**/*", async route =>
        {
            if (route.Request.ResourceType is "font" or "stylesheet")
                await route.AbortAsync();
            else
                await route.ContinueAsync();
        });

        var mediaTask = NativePage.EmulateMediaAsync(new PageEmulateMediaOptions
        {
            ReducedMotion = ReducedMotion.Reduce
        });

        await Task.WhenAll(routeTask, mediaTask);
    }

    private void SetupReactiveEvents()
    {
        Observable.FromEventPattern<EventHandler<IFrame>, IFrame>(
                h => _page.FrameNavigated += h,
                h => _page.FrameNavigated -= h)
            .Select(e => e.EventArgs)
            .Where(f => f == _page.MainFrame)
            .Select(f => f.Url)
            .Subscribe(url => _urlSubject.OnNext(url))
            .DisposeWith(_disposables);

        Observable.FromEventPattern<EventHandler<IResponse>, IResponse>(
                h => _page.Response += h,
                h => _page.Response -= h)
            .Select(e => e.EventArgs)
            .Where(r => r.Request.ResourceType is "fetch" or "xhr")
            .SelectMany(async r =>
            {
                try
                {
                    if (r.Headers.TryGetValue("content-type", out var ct) &&
                        ct.Contains("json", StringComparison.OrdinalIgnoreCase))
                    {
                        var text = await r.TextAsync();
                        return string.IsNullOrWhiteSpace(text)
                            ? null
                            : new NetworkPacket { Url = r.Request.Url, Method = r.Request.Method, RawBody = text };
                    }

                    return null;
                }
                catch
                {
                    return null;
                }
            })
            .Where(p => p is not null)
            .Subscribe(p => _jsonResponseSubject.OnNext(p!))
            .DisposeWith(_disposables);
    }

    private void SetupDialogEvents()
    {
        Observable.FromEventPattern<EventHandler<IDialog>, IDialog>(
                h => _page.Dialog += h,
                h => _page.Dialog -= h)
            .Select(e => e.EventArgs)
            .SelectMany(async dialog =>
            {
                var info = new DialogInfo(dialog.Message, dialog.DefaultValue);

                // Quan trọng: Phải Accept hoặc Dismiss nếu không Playwright sẽ bị treo (freeze)
                switch (dialog.Type)
                {
                    case DialogType.Alert:
                        _alertSubject.OnNext(info);
                        await dialog.DismissAsync();
                        break;

                    case DialogType.Confirm:
                        _confirmSubject.OnNext(info);
                        // Tùy logic, mặc định Auto-Accept để crawl tiếp
                        await dialog.AcceptAsync();
                        break;

                    case DialogType.Prompt:
                        _promptSubject.OnNext(info);
                        // Truyền giá trị tự động gõ vào prompt
                        await dialog.AcceptAsync("Default Prompt Value");
                        break;

                    default:
                        await dialog.DismissAsync();
                        break;
                }

                return System.Reactive.Unit.Default;
            })
            .Subscribe()
            .DisposeWith(_disposables);
    }


    public async ValueTask DisposeAsync()
    {
        _jsonResponseSubject.OnCompleted();
        _urlSubject.OnCompleted();
        _alertSubject.OnCompleted();
        _confirmSubject.OnCompleted();
        _promptSubject.OnCompleted();

        _disposables.Dispose();

        if (!_page.IsClosed)
        {
            await _page.UnrouteAsync("**/*");
            await _page.CloseAsync();
        }
    }
}