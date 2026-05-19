using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using CTUScheduler.Presentation.Services.UserInteractionService.Implementations.Ursa.Notifications.Proxies;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;
using CTUScheduler.Presentation.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using CTUMessageCloseReason = CTUScheduler.Presentation.Services.UserInteractionService.Models.MessageCloseReason;
using MessageCloseReason = Ursa.Controls.MessageCloseReason;

namespace CTUScheduler.Presentation.Services.UserInteractionService.Implementations.Ursa.Notifications.Base;

public abstract class UrsaInteractionManagerBase<TManager> : INotificationTypeAccessor, IUiDisposable
    where TManager : class
{
    protected readonly ILogger Logger;
    protected TManager? Manager;
    private readonly IDisposable _viewContextSubscription;
    private bool _isDisposed;

    public INotificationTypeAccessor Light { get; }


    protected UrsaInteractionManagerBase(IViewContextService viewContextService, ILogger logger)
    {
        Logger = logger;
        Light = new NotificationThemeProxy(this, NotificationTheme.Light);

        _viewContextSubscription = viewContextService.WhenTopLevelChanged
            .DistinctUntilChanged()
            .Subscribe(InitializeInternal);
    }

    // --- Nhóm hàm Success ---
    public void Success(string title, object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Success, title, in options);

    public void Success(object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Success, in options);

    // --- Nhóm hàm Error ---
    public void Error(string title, object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Error, title, in options);

    public void Error(object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Error, in options);

    // --- Nhóm hàm Warning ---
    public void Warning(string title, object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Warning, title, in options);

    public void Warning(object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Warning, in options);

    // --- Nhóm hàm Info ---
    public void Info(string title, object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Information, title, in options);

    public void Info(object content, in NotificationOptions options = default) =>
        Show(content, NotificationType.Information, in options);


    public void Show(object content, NotificationType type, in NotificationOptions options = default) =>
        Show(content, type, null, in options);

    public void Show(
        object content,
        NotificationType type,
        string? title,
        in NotificationOptions options = default)
    {
        var manager = Manager;
        if (manager is null) return;

        // tạo custom content theo ursa
        object finalContent = CreateFinalContent(content, title);

        // Xử lý đóng gói Classes và Theme
        var finalOpt = ProcessThemeAndClasses(in options);
        InvokeShow(manager, type, finalContent, in finalOpt);
    }

    private NotificationOptions ProcessThemeAndClasses(in NotificationOptions opt)
    {
        if (opt.Theme != NotificationTheme.Light) return opt;

        // Zero GC: Dùng cache nếu không có class đi kèm
        if (opt.Classes is null || opt.Classes.Length == 0)
            return opt with { Classes = NotificationCache.LightThemeClass };

        // Chỉ Allocation khi bắt buộc (Light + Class riêng)
        var mergedClasses = new string[opt.Classes.Length + 1];
        mergedClasses[0] = nameof(NotificationTheme.Light);
        opt.Classes.CopyTo(mergedClasses, 1);
        return opt with { Classes = mergedClasses };
    }

    protected abstract TManager CreateManager(TopLevel context);
    protected abstract void UninstallManager(TManager manager);

    protected abstract void InvokeShow(TManager manager, NotificationType type, object content,
        in NotificationOptions opt);

    protected virtual object CreateFinalContent(object content, string? title) => content;

    protected void CleanupManager()
    {
        if (Manager is not null)
        {
            UninstallManager(Manager);
            Manager = null;
        }
    }

    private void InitializeInternal(TopLevel? context)
    {
        CleanupManager();
        if (context is not null)
            Manager = CreateManager(context);
    }

    public virtual void Dispose()
    {
        if (_isDisposed) return;
        _viewContextSubscription.Dispose();
        CleanupManager();
        Logger.LogDebug("{Service} disposed", GetType().Name);
        _isDisposed = true;
    }
    
    protected static CTUMessageCloseReason MapReason(MessageCloseReason reason) => (CTUMessageCloseReason)reason;

}