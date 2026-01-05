using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Path = System.IO.Path;

namespace CTUScheduler.Core.Utils.IO;

public static class PathUtils
{
    public static bool IsValidFilePath(string? path,[NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            errorMessage = "File path cannot be empty or whitespace.";
            return false;
        }
        try
        {
            var fullPath = Path.GetFullPath(path);
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                errorMessage = "Path contains invalid characters.";
                return false;
            }
            var root = Path.GetPathRoot(fullPath);
            if (!string.IsNullOrEmpty(root) && !Directory.Exists(root))
            {
                errorMessage = $"System drive or root path '{root}' does not exist.";
                return false;
            }
            return true;
        }
        catch (ArgumentException) { errorMessage = "Path contains invalid characters or format."; }
        catch (PathTooLongException) { errorMessage = "File path is too long (exceeds OS limit)."; }
        catch (NotSupportedException) { errorMessage = "Path format is not supported (e.g. contains colon in wrong place)."; }
        catch (Exception ex) { errorMessage = $"Invalid path: {ex.Message}"; }
        return false;
    }

    public static bool TryCreateDirectoryForFile(string filePath,[NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            var fullPath = Path.GetFullPath(filePath);
            var directory = Path.GetDirectoryName(fullPath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return true;
        }
        catch (UnauthorizedAccessException) { errorMessage = "Permission denied. Cannot create directory."; }
        catch (IOException) { errorMessage = "Cannot access the path (Directory might be a file or logic error)."; }
        catch (Exception ex) { errorMessage = $"Failed to create directory: {ex.Message}"; }
        return false;
    }
}