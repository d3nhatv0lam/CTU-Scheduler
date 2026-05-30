using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CTUScheduler.Core.Models.Contributors;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Presentation.Base;
using Material.Icons;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Contact.ViewModels;

public class ContactViewModel : ViewModelBase, IRoutableViewModel, IDisposable
{
    private readonly ILogger<ContactViewModel> _logger;
    public string UrlPathSegment => nameof(ContactViewModel);
    public IScreen HostScreen { get; }

    public IReadOnlyList<ContributorViewModel> Contributors { get; }
    public ReactiveCommand<string, Unit> OpenUrlCommand { get; }

    public ContactViewModel(IScreen hostScreen, ILogger<ContactViewModel> logger)
    {
        HostScreen = hostScreen;
        _logger = logger;

        Contributors = AppConstants.AppCredits.AllContributors.Select(c => new ContributorViewModel(c)).ToList();

        OpenUrlCommand = ReactiveCommand.Create<string>(url =>
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // ignored
            }
        });
    }

    public void Dispose()
    {
        foreach (var contributor in Contributors)
        {
            contributor.Dispose();
        }

        OpenUrlCommand.Dispose();

        _logger.LogDebug("Disposed");
    }
}

public class ContributorViewModel : ReactiveObject, IDisposable
{
    private static readonly HttpClient HttpClient = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _isDisposed;

    public string Name { get; }
    public string? Nickname { get; }
    public string Bio { get; }
    public string FallbackLetter { get; }
    public List<SocialLinkItem> SocialLinks { get; }

    private Bitmap? _avatarImage;

    public Bitmap? AvatarImage
    {
        get => _avatarImage;
        private set => this.RaiseAndSetIfChanged(ref _avatarImage, value);
    }

    public ContributorViewModel(ContributorProfile profile)
    {
        Name = profile.Name;
        Nickname = profile.Nickname;
        Bio = profile.Bio ?? string.Empty;
        SocialLinks = profile.SocialLinks
            .OrderBy(kvp => kvp.Key.ToString())
            .Select(kvp => new SocialLinkItem(kvp.Key, kvp.Value)).ToList();

        FallbackLetter = !string.IsNullOrWhiteSpace(Name) ? Name.Trim().Last().ToString().ToUpper() : "?";

        if (!string.IsNullOrWhiteSpace(profile.AvatarUrl))
        {
            var token = _cts.Token;
            Task.Run(async () =>
            {
                try
                {
                    using var response = await HttpClient.GetAsync(profile.AvatarUrl, token);
                    response.EnsureSuccessStatusCode();
                    await using var stream = await response.Content.ReadAsStreamAsync(token);
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms, token);
                    ms.Seek(0, SeekOrigin.Begin);

                    if (token.IsCancellationRequested) return;

                    var bitmap = new Bitmap(ms);

                    if (token.IsCancellationRequested)
                    {
                        bitmap.Dispose();
                        return;
                    }

                    AvatarImage = bitmap;
                }
                catch
                {
                    // ignored
                }
            }, token);
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            _cts.Cancel();
        }
        catch
        {
            // ignored
        }

        _cts.Dispose();

        _avatarImage?.Dispose();
        _avatarImage = null;
    }
}

public record SocialLinkItem(SocialPlatform Platform, string Url)
{
    public string Title => Platform.ToString();

    // Dữ liệu SVG của các icon tùy chỉnh không có sẵn trong bộ Material.Icons
    public string? CustomIconData => Platform switch
    {
        SocialPlatform.TikTok =>
            "M12.53.02C13.84 0 15.14.01 16.44 0c.08 1.53.63 3.09 1.75 4.17 1.12 1.11 2.7 1.62 4.24 1.79v4.03c-1.44-.05-2.89-.35-4.2-.97-.57-.26-1.1-.59-1.62-.93-.01 2.92.01 5.84-.02 8.75-.08 1.4-.54 2.79-1.35 3.94-1.31 1.92-3.58 3.17-5.91 3.17-3.29 0-5.96-2.67-5.96-5.96 0-3.29 2.67-5.96 5.96-5.96.67 0 1.32.12 1.92.34v4.54c-.6-.2-1.25-.32-1.92-.32-1.63 0-2.95 1.32-2.95 2.95 0 1.63 1.32 2.95 2.95 2.95 1.63 0 2.95-1.32 2.95-2.95V0h3.01z",
        _ => null
    };

    public bool HasCustomIcon => !string.IsNullOrEmpty(CustomIconData);
    public bool HasNormalIcon => !HasCustomIcon;

    public MaterialIconKind Icon => Platform switch
    {
        SocialPlatform.Facebook => MaterialIconKind.Facebook,
        SocialPlatform.YouTube => MaterialIconKind.Youtube,
        SocialPlatform.GitHub => MaterialIconKind.Github,
        _ => MaterialIconKind.Link
    };

    public string BackgroundColor => Platform switch
    {
        SocialPlatform.Facebook => "#1877F2",
        SocialPlatform.YouTube => "#FF0000",
        SocialPlatform.GitHub => "#24292F",
        SocialPlatform.TikTok => "#000000",
        _ => "#F44336"
    };
}