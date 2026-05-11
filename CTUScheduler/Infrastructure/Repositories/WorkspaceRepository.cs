using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Helpers.Json;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Core.Utils.IO;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Repositories;

public class WorkspaceRepository : IWorkspaceRepository
{
    private readonly ILogger<WorkspaceRepository> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new SafeGuidConverter() }
    };

    public WorkspaceRepository(ILogger<WorkspaceRepository> logger)
    {
        _logger = logger;
    }

    public async Task<OperationResult> SaveAsync(WorkspaceSnapshot snapshot, string filePath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        
        if (!PathUtils.IsValidFilePath(filePath, out var errorMessage))
        {
            return OperationResult.Failed(errorMessage, "Path.Invalid", OperationFailureReason.Validation);
        }

        await _lock.WaitAsync(ct);
        try
        {
            await JsonHelper.SerializeToSafeFileAsync(filePath, snapshot, _jsonSerializerOptions, ct)
                .ConfigureAwait(false);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving workspace to {FilePath}", filePath);
            return MapException<WorkspaceSnapshot>(ex);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<OperationResult<WorkspaceSnapshot>> LoadAsync(string filePath, CancellationToken ct = default)
    {
        if (!PathUtils.IsValidFilePath(filePath, out var errorMessage))
        {
            return OperationResult<WorkspaceSnapshot>.Failed(errorMessage, "Path.Invalid", OperationFailureReason.Validation);
        }

        await _lock.WaitAsync(ct);
        try
        {
            var workspace = await JsonHelper.DeserializeFromFileAsync<WorkspaceSnapshot>(filePath, _jsonSerializerOptions, ct)
                .ConfigureAwait(false);

            if (workspace is null)
            {
                return OperationResult<WorkspaceSnapshot>.Failed(
                    "Không thể đọc dữ liệu workspace", 
                    "Storage.FormatError", 
                    OperationFailureReason.Validation);
            }

            return workspace;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading workspace from {FilePath}", filePath);
            return MapException<WorkspaceSnapshot>(ex);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static OperationResult<T> MapException<T>(Exception ex) => ex switch
    {
        JsonException => OperationResult<T>.FromException(ex, "Cấu trúc file bị lỗi định dạng.", "Storage.FormatError"),
        IOException => OperationResult<T>.FromException(ex, "File đang bị khóa hoặc lỗi I/O.", "Storage.IOError"),
        UnauthorizedAccessException => OperationResult<T>.FromException(ex, "Không có quyền truy cập file.", "Storage.Denied"),
        _ => OperationResult<T>.FromException(ex, "Lỗi hệ thống không xác định.", "Storage.Unknown")
    };
}
