using System.Diagnostics;
using System.Text;

namespace RxAmethyst.PlaywrightWrapper.Helpers;

internal static class ShellExecutor
{
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
        foreach (var arg in arguments)
            startInfo.ArgumentList.Add(arg);
        if (envVars != null)
        {
            foreach (var kvp in envVars)
                startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
        }
        using var process = new Process();
        process.StartInfo = startInfo;
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
                if (!process.HasExited)
                {
                    process.Kill(true); // Kill toàn bộ Process Tree
                }
            }
            catch
            {
                // ignore
            }
            throw;
        }
        return process.ExitCode;
    }
    public static (string FileName, IEnumerable<string> Args) PrepareShellCommand(string scriptPath, string[] scriptArgs)
    {
        string fullPath = Path.GetFullPath(scriptPath);
        if (OperatingSystem.IsWindows())
        {
            var args = new List<string>
            {
                "-NoProfile",
                "-ExecutionPolicy", "Bypass",
                "-File", fullPath
            };
            args.AddRange(scriptArgs);
            return ("powershell.exe", args);
        }
        else
        {
            string scriptPathSh = Path.ChangeExtension(fullPath, ".sh");
            if (!File.Exists(scriptPathSh))
            {
                if (File.Exists(fullPath)) scriptPathSh = fullPath;
                else throw new FileNotFoundException($"Không tìm thấy script tại: {scriptPathSh} hoặc {fullPath}");
            }
            var args = new List<string> { scriptPathSh };
            args.AddRange(scriptArgs);
            return ("/bin/sh", args);
        }
    }
}