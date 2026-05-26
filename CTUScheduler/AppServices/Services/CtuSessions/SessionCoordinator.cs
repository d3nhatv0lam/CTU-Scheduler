using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.CtuSessions;

public class SessionCoordinator : ISessionCoordinator
{
    private readonly IAuthClient _authClient;
    private readonly ICtuSessionStore _sessionStore;
    private readonly ISessionHeartbeatService _heartbeatService;
    private readonly IEnumerable<ICleanup> _cleanupServices;
    private readonly IEnumerable<ICleanupAsync> _asyncCleanupServices;
    private readonly ILogger<SessionCoordinator> _logger;

    public SessionCoordinator(
        IAuthClient authClient,
        ICtuSessionStore sessionStore,
        ISessionHeartbeatService heartbeatService,
        IEnumerable<ICleanup> cleanupServices,
        IEnumerable<ICleanupAsync> asyncCleanupServices,
        ILogger<SessionCoordinator> logger)
    {
        _authClient = authClient;
        _sessionStore = sessionStore;
        _heartbeatService = heartbeatService;
        _cleanupServices = cleanupServices;
        _asyncCleanupServices = asyncCleanupServices;
        _logger = logger;
    }

    public async Task<OperationResult> LoginAsync()
    {
        try
        {
            throw new System.NotImplementedException();
        }
        catch (Exception ex)
        {
            return OperationResult.FromException(ex);
        }
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