using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.DriverCore;

public abstract class BaseUiContext
{
    protected readonly IWebDriverService WebDriverService;
    protected readonly ILogger Logger;
    
    protected BaseUiContext(IWebDriverService webDriverService, ILoggerFactory loggerFactory)
    {
        WebDriverService = webDriverService;
        Logger = loggerFactory.CreateLogger(this.GetType());
    }
}