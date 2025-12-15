using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.DriverCore;

public abstract class BaseUiContext<T> where T: class
{
    protected readonly IWebDriverService WebDriverService;
    protected readonly ILogger<T> Logger;
    
    protected BaseUiContext(IWebDriverService webDriverService, ILogger<T> logger)
    {
        WebDriverService = webDriverService;
        Logger = logger;
    }
}