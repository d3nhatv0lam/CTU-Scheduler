using System.Collections.Generic;
using System.Threading.Tasks;
using CTUScheduler.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public class SessionManager : ISessionManager
{
    private readonly IEnumerable<ICleanup> _cleanupServices;
    private readonly IEnumerable<ICleanupAsync> _asyncCleanupServices;
    private readonly ILogger<SessionManager> _logger;

    public SessionManager(IEnumerable<ICleanup> cleanupServices,
        IEnumerable<ICleanupAsync> asyncCleanupServices,
        ILogger<SessionManager> logger)
    {
        _cleanupServices = cleanupServices;
        _asyncCleanupServices = asyncCleanupServices;
        _logger = logger;
    }

    public async Task LogoutAsync()
    {
        foreach (var cleanup in _cleanupServices)
        {
            cleanup.Cleanup();
        }

        foreach (var asyncCleanup in _asyncCleanupServices)
        {
            await asyncCleanup.CleanupAsync();
        }

        _logger.LogInformation("Logged out! Session data cleared.");
    }
}