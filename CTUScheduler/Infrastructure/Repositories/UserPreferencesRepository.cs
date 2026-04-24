using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Helpers.Json;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CTUScheduler.Infrastructure.Repositories;

public class UserPreferencesRepository : IUserPreferencesRepository
{
    private readonly ILogger<UserPreferencesRepository> _logger;
    private readonly UserPreferencesOptions _options;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        IgnoreReadOnlyFields = true,
    };

    public UserPreferencesRepository(IOptions<UserPreferencesOptions> options,
        ILogger<UserPreferencesRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Value.FilePath);
        _logger = logger;
        _options = options.Value;
        _logger.LogDebug("{this} initialized with options: {Options}", nameof(UserPreferencesRepository), _options);
    }

    public async Task<OperationResult> SaveAsync(UserPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await JsonHelper.SerializeToSafeFileAsync(
                    _options.FilePath,
                    preferences,
                    _jsonSerializerOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return MapException<UserPreferences>(ex);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<OperationResult<UserPreferences>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var filePath = _options.FilePath;

            if (!File.Exists(filePath))
            {
                return new UserPreferences();
            }

            var preferences = await JsonHelper
                .DeserializeFromFileAsync<UserPreferences>(filePath, token: cancellationToken)
                .ConfigureAwait(false);

            if (preferences is null)
            {
                _logger.LogWarning("Failed to load preferences from file {FilePath}.", filePath);
                return OperationResult<UserPreferences>.Failed(
                    "Dữ liệu cài đặt đã bị hỏng",
                    "Storage.FormatError",
                    OperationFailureReason.Validation);
            }

            return preferences;
        }
        catch (Exception ex)
        {
            return MapException<UserPreferences>(ex);
        }
        finally
        {
            _lock.Release();
        }
    }


    private static OperationResult<T> MapException<T>(Exception ex) => ex switch
    {
        JsonException => OperationResult<T>.FromException(ex, "Cấu trúc file bị lỗi định dạng.", "Storage.FormatError"),
        IOException => OperationResult<T>.FromException(ex, "File đang bị khóa.", "Storage.Locked"),
        UnauthorizedAccessException => OperationResult<T>.FromException(ex, "Không có quyền truy cập.",
            "Storage.Denied"),
        _ => OperationResult<T>.FromException(ex, "Lỗi hệ thống.", "Storage.Unknown")
    };
}