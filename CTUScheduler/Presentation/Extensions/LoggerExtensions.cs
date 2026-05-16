using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Extensions;

public static class LoggerExtensions
{
    private const string Ui = "UI";
    private const string Host = "Host";
    private const string Application = "Application";

    // ---------------- UI ----------------

    public static void UiInfo(
        this ILogger logger,
        string message,
        params object[] args)
    {
        logger.LogInformation(
            message + " {ShortTypeName}",
            args.Append(Ui).ToArray());
    }

    public static void UiError(
        this ILogger logger,
        Exception ex,
        string message,
        params object[] args)
    {
        logger.LogError(
            ex,
            message + " {ShortTypeName}",
            args.Append(Ui).ToArray());
    }

    // ---------------- Host ----------------

    public static void HostInfo(
        this ILogger logger,
        string message,
        params object[] args)
    {
        logger.LogInformation(
            message + " {ShortTypeName}",
            args.Append(Host).ToArray());
    }

    public static void HostError(
        this ILogger logger,
        Exception ex,
        string message,
        params object[] args)
    {
        logger.LogError(
            ex,
            message + " {ShortTypeName}",
            args.Append(Host).ToArray());
    }

    // ---------------- Application ----------------

    public static void AppInfo(
        this ILogger logger,
        string message,
        params object[] args)
    {
        logger.LogInformation(
            message + " {ShortTypeName}",
            args.Append(Application).ToArray());
    }

    public static void AppError(
        this ILogger logger,
        Exception ex,
        string message,
        params object[] args)
    {
        logger.LogError(
            ex,
            message + " {ShortTypeName}",
            args.Append(Application).ToArray());
    }
}