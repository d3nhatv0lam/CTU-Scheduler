namespace RxAmethyst.PlaywrightWrapper.Abstractions;

public interface IWebDriverService
{
    IWebTab MainTab { get; }
    Task InitBrowserAsync(CancellationToken cancellationToken = default);
    Task ResetBrowserAsync();
    Task<IWebTab> CreateTabAsync();
}