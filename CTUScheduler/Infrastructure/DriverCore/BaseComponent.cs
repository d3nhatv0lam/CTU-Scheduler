using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static Microsoft.Playwright.Assertions;

namespace CTUScheduler.Infrastructure.DriverCore;

/// <summary>
/// WebElement witout PageUrl
/// </summary>
/// <typeparam name="T">Class</typeparam>
public abstract class BaseComponent<T>: BaseUiContext<T> where T: class
{
    protected abstract string ComponentSelector { get; }
    
    public BaseComponent(IWebDriverService webDriverService, ILogger<T> logger) : base(webDriverService, logger)
    {
    }
    public virtual async Task IsComponentVisibleAsync()
    {
         await Expect(WebDriverService.GetLocator(ComponentSelector)).ToBeVisibleAsync();
    } 
}