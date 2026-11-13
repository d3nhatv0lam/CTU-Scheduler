using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Update;
using Microsoft.Extensions.Logging;
using Velopack;

namespace CTUScheduler.Infrastructure.Services.Update;

public class VelopackUpdateService : IUpdateService
{
    private readonly UpdateManager _updateManager;
    private UpdateInfo? _lastUpdateInfo;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger<VelopackUpdateService> _logger;

    public VelopackUpdateService(UpdateManager updateManager, ILogger<VelopackUpdateService> logger)
    {
        _updateManager = updateManager;
        _logger = logger;
    }

    public bool IsInstalled => _updateManager.IsInstalled;

    public string CurrentVersion
    {
        get
        {
            if (_updateManager is { IsInstalled: true, CurrentVersion: not null })
            {
                return _updateManager.CurrentVersion.ToNormalizedString();
            }

            var asm = Assembly.GetEntryAssembly() ?? typeof(VelopackUpdateService).Assembly;
            var infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(infoVer))
            {
                return infoVer.Split('+')[0];
            }

            return asm.GetName().Version?.ToString(3) ?? "1.0.0-beta";
        }
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var currentVer = _updateManager.CurrentVersion?.ToString();
            if (!_updateManager.IsInstalled)
            {
                return UpdateCheckResult.NoUpdate(currentVer);
            }

            _lastUpdateInfo = await _updateManager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (_lastUpdateInfo is null)
            {
                return UpdateCheckResult.NoUpdate(currentVer);
            }

            var packageSize = _lastUpdateInfo.DeltasToTarget.Length > 0
                ? _lastUpdateInfo.DeltasToTarget.Sum(data => data.Size)
                : _lastUpdateInfo.TargetFullRelease.Size;

            return new UpdateCheckResult(
                IsUpdateAvailable: true,
                CurrentVersion: currentVer,
                TargetVersion: _lastUpdateInfo.TargetFullRelease.Version.ToString(),
                PackageSizeBytes: packageSize,
                IsDowngrade: _lastUpdateInfo.IsDowngrade
            );
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DownloadUpdatesAsync(Action<int>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_lastUpdateInfo is null)
                throw new InvalidOperationException("No update info available");

            await _updateManager.DownloadUpdatesAsync(_lastUpdateInfo, onProgress, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void ApplyUpdatesAndRestart()
    {
        if (_lastUpdateInfo is null)
            throw new InvalidOperationException("No update info available");

        _updateManager.ApplyUpdatesAndRestart(_lastUpdateInfo);
    }
}