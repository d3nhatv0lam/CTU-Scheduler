using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Helpers.Json;

public static class JsonHelper
{
    public static JsonSerializerOptions ScheduleLoadOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
    };

    // Buffer 32KB cho luồng file để tối ưu I/O với file lớn
    private const int DefaultBufferSize = 32768; // 1 << 15

    #region Serialize (Ghi File)

    public static async Task SerializeToFileAsync<T>(
        string filePath,
        T obj,
        JsonSerializerOptions? options = null,
        CancellationToken token = default)
    {
        var fileOptions = new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous,
            BufferSize = DefaultBufferSize
        };

        await using var stream = new FileStream(filePath, fileOptions);
        await JsonSerializer.SerializeAsync(stream, obj, options, token).ConfigureAwait(false);
    }

    public static async Task SerializeToSafeFileAsync<T>(
        string filePath,
        T obj,
        JsonSerializerOptions? options = null,
        CancellationToken token = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempFilePath = filePath + ".tmp";
        var fileOptions = new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous,
            BufferSize = DefaultBufferSize
        };

        bool serializationSuccess = false;

        try
        {
            await using (var stream = new FileStream(tempFilePath, fileOptions))
            {
                await JsonSerializer.SerializeAsync(stream, obj, options, token).ConfigureAwait(false);
            }

            serializationSuccess = true;
        }
        finally
        {
            if (serializationSuccess)
            {
                File.Move(tempFilePath, filePath, overwrite: true);
            }
            else
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
    }

    #endregion
    
    #region Deserialize (Đọc File)
    
    public static T? DeserializeFromFile<T>(string filePath, JsonSerializerOptions? options = null)
    {
        if (!File.Exists(filePath)) return default;

        // Đọc đồng bộ (Sync) chỉ nên dùng cho file rất nhỏ hoặc lúc khởi động app.
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.SequentialScan);
        return JsonSerializer.Deserialize<T>(stream, options);
    }
    
    public static async Task<T?> DeserializeFromFileAsync<T>(
        string filePath, 
        JsonSerializerOptions? options = null,
        CancellationToken token = default)
    {
        if (!File.Exists(filePath)) return default;

        var fileOptions = new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan, // Gợi ý cho OS biết file sẽ đọc tuần tự
            BufferSize = DefaultBufferSize
        };

        await using var stream = new FileStream(filePath, fileOptions);
        return await JsonSerializer.DeserializeAsync<T>(stream, options, token).ConfigureAwait(false);
    }
    #endregion
}