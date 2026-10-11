using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Models.Update;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Updater.Models;
using Irihi.Avalonia.Shared.Contracts;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Updater.ViewModels;

public partial class UpdateViewModel : ViewModelBase, IDialogContext, IDisposable
{
    private readonly IUpdateService _updateService;
    private readonly ILogger<UpdateViewModel> _logger;
    private CancellationTokenSource? _downloadCts;

    [Reactive] private UpdateState _state = UpdateState.Available;
    [Reactive] private string _currentVersion = string.Empty;
    [Reactive] private string _targetVersion = string.Empty;
    [Reactive] private string _formattedSize = string.Empty;
    [Reactive] private bool _isDowngrade;
    [Reactive] private int _downloadProgress;
    [Reactive] private string? _errorMessage;

    public ReactiveCommand<Unit, Unit> DownloadAndInstallCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelDownloadCommand { get; }
    public ReactiveCommand<Unit, Unit> IgnoreVersionCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }

    public event EventHandler<object?>? RequestClose;

    public UpdateViewModel(IUpdateService updateService, ILogger<UpdateViewModel> logger)
    {
        _updateService = updateService;
        _logger = logger;

        DownloadAndInstallCommand = ReactiveCommand.CreateFromTask(DownloadAndInstallAsync);
        CancelDownloadCommand = ReactiveCommand.Create(CancelDownload);
        IgnoreVersionCommand = ReactiveCommand.Create(() => CloseWith(UpdateOutcome.Ignored));
        CloseCommand = ReactiveCommand.Create(() => CloseWith(UpdateOutcome.Closed));
    }

    public void Initialize(UpdateCheckResult checkResult)
    {
        // CurrentVersion = checkResult.CurrentVersion ?? "Chưa xác định";
        // TargetVersion = checkResult.TargetVersion ?? string.Empty;
        // FormattedSize = checkResult.FormattedSize;
        // IsDowngrade = checkResult.IsDowngrade;
        CurrentVersion = checkResult.CurrentVersion ?? "1.0.1";
        TargetVersion = checkResult.TargetVersion ?? "1.1.0";
        FormattedSize = "1000MB";
        IsDowngrade = checkResult.IsDowngrade;
        State = UpdateState.Available;
    }

    private async Task DownloadAndInstallAsync()
    {
        State = UpdateState.Downloading;
        DownloadProgress = 0;
        ErrorMessage = null;

        _downloadCts = new CancellationTokenSource();
        try
        {
            await _updateService.DownloadUpdatesAsync(
                onProgress: progress => DownloadProgress = progress,
                cancellationToken: _downloadCts.Token);

            RequestClose?.Invoke(this, UpdateOutcome.Updated);
            _updateService.ApplyUpdatesAndRestart();
        }
        // Người dùng chủ động hủy tải -> Quay lại trạng thái Available
        catch (OperationCanceledException)
        {
            DownloadProgress = 0;
            State = UpdateState.Available;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download updates");
            ErrorMessage = ex.Message;
            State = UpdateState.Failed;
        }
    }

    private void CancelDownload()
    {
        _downloadCts?.Cancel();
        CloseWith(UpdateOutcome.Cancelled);
    }

    public void Close()
    {
        CloseWith(UpdateOutcome.Closed);
    }

    private void CloseWith(UpdateOutcome outcome)
    {
        _downloadCts?.Cancel();
        RequestClose?.Invoke(this, outcome);
    }

    public void Dispose()
    {
        _downloadCts?.Cancel();
        _downloadCts?.Dispose();
        _downloadCts = null;
    }
}