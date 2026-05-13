using System;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CTUScheduler.Core.Models.Contributors;
using CTUScheduler.Core.Models.Settings;
using Material.Icons;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Contact.ViewModels;

public record SocialLinkItem(SocialPlatform Platform, string Url)
{
    public string Title => Platform.ToString();
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
        _ => "#F44336"
    };
}

public class ContributorViewModel : ReactiveObject
{
    private static readonly HttpClient HttpClient = new HttpClient();

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
        SocialLinks = profile.SocialLinks.Select(kvp => new SocialLinkItem(kvp.Key, kvp.Value)).ToList();
        
        FallbackLetter = !string.IsNullOrWhiteSpace(Name) ? Name.Trim().Last().ToString().ToUpper() : "?";

        if (!string.IsNullOrWhiteSpace(profile.AvatarUrl))
        {
            Task.Run(async () =>
            {
                try
                {
                    var response = await HttpClient.GetAsync(profile.AvatarUrl);
                    response.EnsureSuccessStatusCode();
                    await using var stream = await response.Content.ReadAsStreamAsync();
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    var bitmap = new Bitmap(ms);
                    
                    // Update on UI Thread might not be strictly necessary for Avalonia if using RaiseAndSetIfChanged?
                    // Better to be safe. Avalonia allows property changed from any thread in 11.0, but to be sure:
                    AvatarImage = bitmap;
                }
                catch { }
            });
        }
    }
}

public class ContactViewModel : ViewModelBase, IRoutableViewModel, IViewModel
{
    public string UrlPathSegment => nameof(ContactViewModel);
    public IScreen HostScreen { get; }
    
    public IReadOnlyList<ContributorViewModel> Contributors { get; }
    public ReactiveCommand<string, Unit> OpenUrlCommand { get; }

    public ContactViewModel(IScreen hostScreen)
    {
        HostScreen = hostScreen;
        Contributors = AppConstants.AppCredits.AllContributors.Select(c => new ContributorViewModel(c)).ToList();
        
        OpenUrlCommand = ReactiveCommand.Create<string>(url =>
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        });
    }
}
