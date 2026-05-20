using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Helpers;

public static class ProcessHelper
{
    /// <summary>
    /// Open url with default browser in Windows, Linux, Mac
    /// </summary>
    /// <param name="url">Web url</param>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static void OpenUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        // Nếu không phải là đường dẫn tệp tin hoặc thư mục tồn tại cục bộ, kiểm tra định dạng Uri tuyệt đối
        if (!File.Exists(url) && !Directory.Exists(url))
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                throw new ArgumentException("Invalid url or local path", nameof(url));
        }

        if (TryOpenUrl(url)) return;

        throw new NotSupportedException("Unsupported OS");
    }

    private static bool TryOpenUrl(string url)
    {
        return OperatingSystem.IsWindows() ? OpenUrlInWindows(url) :
            OperatingSystem.IsLinux() ? TryOpenUrlWithLinux(url) :
            OperatingSystem.IsMacOS() ? TryOpenUrlWithMac(url) : false;
    }

    private static bool OpenUrlInWindows(string url)
    {
        if (!OperatingSystem.IsWindows()) return false;
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            }
        };
        process.Start();
        return true;
    }

    private static bool TryOpenUrlWithLinux(string url)
    {
        if (!OperatingSystem.IsLinux()) return false;
        try
        {
            Process.Start("xdg-open", url);
            return true;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            try
            {
                // Nếu `xdg-open` không tồn tại, thử `x-www-browser`
                Process.Start("x-www-browser", url);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private static bool TryOpenUrlWithMac(string url)
    {
        if (!OperatingSystem.IsMacOS()) return false;
        Process.Start("open", url);
        return true;
    }

    /// <summary>
    /// Chạy script đa nền tảng (Wrapper cho Process)
    /// </summary>
    public static async Task<int> RunScriptAsync(
        string fileName,
        IEnumerable<string> arguments,
        string workingDir,
        Dictionary<string, string>? envVars,
        Action<string> onOutput,
        Action<string> onError,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDir,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        // Tự động add từng tham số vào ArgumentList
        foreach (var arg in arguments)
            startInfo.ArgumentList.Add(arg);

        // Inject Environment Variables
        if (envVars != null)
        {
            foreach (var kvp in envVars)
                startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
        }

        using var process = new Process();
        process.StartInfo = startInfo;

        // Đăng ký event log
        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null) onOutput(e.Data);
        };
        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null) onError(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try
            {
                // Kiểm tra nếu process chưa thoát thì mới giết
                if (!process.HasExited)
                {
                    // Kill(true) = Giết cả dòng họ (Process Tree)
                    process.Kill(true);
                }
            }
            catch
            {
                // Có thể process vừa tự tắt xong, hoặc không đủ quyền truy cập.
                // Việc Kill thất bại lúc này không quan trọng, vì mục tiêu là dừng task.
                // Có thể log warning nhẹ nếu muốn: Debug.WriteLine($"Kill failed: {ex.Message}");
                // ignore
            }

            throw;
        }

        return process.ExitCode;
    }

    public static (string FileName, IEnumerable<string> Args) PrepareShellCommand(string scriptPath,
        string[] scriptArgs)
    {
        // 1. Chuẩn hóa đường dẫn tuyệt đối (Xử lý luôn vụ \ hay /)
        string fullPath = Path.GetFullPath(scriptPath);

        if (OperatingSystem.IsWindows())
        {
            // Windows: Gọi PowerShell
            // Cú pháp: powershell.exe -NoProfile -ExecutionPolicy Bypass -File "path" arg1 arg2
            var args = new List<string>
            {
                "-NoProfile",
                "-ExecutionPolicy", "Bypass",
                "-File", fullPath
            };
            args.AddRange(scriptArgs); // Nối thêm tham số của script (ví dụ: install chromium)

            return ("powershell.exe", args);
        }
        else
        {
            //  đổi sang .sh
            string scriptPathSh = Path.ChangeExtension(fullPath, ".sh");

            if (!File.Exists(scriptPathSh))
            {
                // Fallback: Nếu không tìm thấy file .sh tự sinh, kiểm tra xem file gốc có tồn tại không
                if (File.Exists(fullPath)) scriptPathSh = fullPath;
                else throw new FileNotFoundException($"Không tìm thấy script tại: {scriptPathSh} hoặc {fullPath}");
            }

            // Linux: Gọi sh path/to/script.sh arg1 arg2
            var args = new List<string> { scriptPathSh };
            args.AddRange(scriptArgs);

            return ("/bin/sh", args);
        }
    }
}