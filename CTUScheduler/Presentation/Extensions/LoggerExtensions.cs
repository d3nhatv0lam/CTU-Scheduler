using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Extensions;

public static class LoggerExtensions
{
    private const string ShortTypeName = nameof(ShortTypeName);
    
    private static readonly IReadOnlyDictionary<string, object> UiScope =
        new Dictionary<string, object>
        {
            [ShortTypeName] = "UI"
        };

    private static readonly IReadOnlyDictionary<string, object> HostScope =
        new Dictionary<string, object>
        {
            [ShortTypeName] = "Host"
        };

    private static readonly IReadOnlyDictionary<string, object> AppScope =
        new Dictionary<string, object>
        {
            [ShortTypeName] = "Application"
        };


    private static IDisposable? BeginTaggedScope(
        this ILogger logger,
        IReadOnlyDictionary<string, object> scope)
    {
        return logger.BeginScope(scope);
    }
    
    // ---------------- UI ----------------

    public static void UiInfo(
        this ILogger logger,
        string message,
        params object[] args)
    {
        using var _ = logger.BeginTaggedScope(UiScope);
        logger.LogInformation(message, args);
    }

    public static void UiError(
        this ILogger logger,
        Exception ex,
        string message,
        params object[] args)
    {
        using var _ = logger.BeginTaggedScope(UiScope);
        logger.LogError(ex, message, args);
    }

    // ---------------- Host ----------------

    public static void HostInfo(
        this ILogger logger,
        string message,
        params object[] args)
    {
        using var _ = logger.BeginTaggedScope(HostScope);
        logger.LogInformation(message, args);
    }

    public static void HostError(
        this ILogger logger,
        Exception ex,
        string message,
        params object[] args)
    {
        using var _ = logger.BeginTaggedScope(HostScope);
        logger.LogError(ex, message, args);
    }

    // ---------------- App ----------------

    public static void AppInfo(
        this ILogger logger,
        string message,
        params object[] args)
    {
        using var _ = logger.BeginTaggedScope(AppScope);
        logger.LogInformation(message, args);
    }

    public static void AppError(
        this ILogger logger,
        Exception ex,
        string message,
        params object[] args)
    {
        using var _ = logger.BeginTaggedScope(AppScope);
        logger.LogError(ex, message, args);
    }
}