using Microsoft.Playwright;

namespace RxAmethyst.Playwright.Extensions;

public static class PlaywrightExtensions
{
    /// <summary>
    /// Bấm và chờ cho đến khi URL thay đổi thành URL mong muốn (Tuyệt đối an toàn)
    /// Hỗ trợ Glob pattern (VD: "**/*hindex.php*") hoặc Regex
    /// </summary>
    public static async Task ClickAndWaitForUrlAsync(
        this ILocator locator,
        string urlGlobString, 
        LocatorClickOptions? options = null)
    {
        var page = locator.Page;

        await Task.WhenAll(
            page.WaitForURLAsync(urlGlobString),
            locator.ClickAsync(options)
        );
    }
    
    /// <summary>
    /// Bấm và chờ trạng thái mạng tĩnh lặng (Hàm này vẫn an toàn nếu dùng với WhenAll)
    /// </summary>
    public static async Task ClickAndWaitForLoadStateAsync(
        this ILocator locator, 
        LoadState state = LoadState.NetworkIdle, 
        LocatorClickOptions? options = null)
    {
        var page = locator.Page;
        await Task.WhenAll(
            page.WaitForLoadStateAsync(state),
            locator.ClickAsync(options)
        );
    }
}