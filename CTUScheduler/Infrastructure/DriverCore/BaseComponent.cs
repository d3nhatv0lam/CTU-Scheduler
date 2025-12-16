using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static Microsoft.Playwright.Assertions;

namespace CTUScheduler.Infrastructure.DriverCore;

/// <summary>
/// WebElement witout PageUrl
/// </summary>
public abstract class BaseComponent: BaseUiContext
{
    protected abstract string ComponentSelector { get; }
    
    public BaseComponent(IWebDriverService webDriverService, ILoggerFactory loggerFactory) : base(webDriverService, loggerFactory)
    {
    }
    protected virtual async Task IsComponentVisibleAsync()
    {
         await Expect(WebDriverService.GetLocator(ComponentSelector)).ToBeVisibleAsync();
    } 
}