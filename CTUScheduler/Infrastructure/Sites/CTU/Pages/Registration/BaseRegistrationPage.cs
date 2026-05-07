using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using CTUScheduler.Infrastructure.Sites.Base;
using CTUScheduler.Infrastructure.Sites.CTU.Routes;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;

public abstract class BaseRegistrationPage : AppPage, IRequireSession, IStudentInfoPage
{
    private const string InvalidSessionSelector =
        ".ant-modal-confirm-content:has-text('Bạn hiện không có quyền truy cập vào hệ thống')";
    
    // >>: Đổi cách tìm element theo quy tắc ở phía sau
    private const string SpaUserInfoSelector = "header span[aria-label='user'] >> xpath=../../div[1]";

    protected SidebarComponent SidebarComponent { get; }

    protected BaseRegistrationPage(IWebTab tab, IConnectivityService connectivityService, ILoggerFactory loggerFactory) : base(
        tab, connectivityService, loggerFactory)
    {
        SidebarComponent = new SidebarComponent(tab);
    }


    public override async Task NavigateToAsync(PageGotoOptions? options = null)
    {
        if (IsInsideSpaHost())
        {
            await NavigateToFormSideBarAsync();
            return;
        }

        await base.NavigateToAsync(options);
    }

    public override async Task WaitForReadyAsync(int timeoutMs = 30000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);

        var opt = new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 0 };
        var urlOpt = new PageWaitForURLOptions { Timeout = 0 };

        var pReadyTask = Tab.NativePage.Locator(PageReadySelector).WaitForAsync(opt);
        var pUrlDeadTask = Tab.NativePage.WaitForURLAsync(CtuRoutes.AuthRedirectRegex, urlOpt);
        var pPopupDeadTask = string.IsNullOrEmpty(InvalidSessionSelector)
            ? Task.Delay(Timeout.Infinite)
            : Tab.NativePage.Locator(InvalidSessionSelector).WaitForAsync(opt);

        pReadyTask.FireAndForgetSafe();
        pUrlDeadTask.FireAndForgetSafe();
        pPopupDeadTask.FireAndForgetSafe();

        var readyTask = pReadyTask.WaitAsync(cts.Token);
        var urlDeadTask = pUrlDeadTask.WaitAsync(cts.Token);
        var popupDeadTask = pPopupDeadTask.WaitAsync(cts.Token);

        try
        {
            var winner = await Task.WhenAny(readyTask, urlDeadTask, popupDeadTask);

            await winner;

            if (winner == urlDeadTask)
            {
                throw new SessionExpiredException($"Mất session tại {GetType().Name}");
            }

            if (winner == popupDeadTask)
            {
                throw new SessionExpiredException($"Mất session tại {GetType().Name}");
            }
            
            // winner == readyTask, (Thành công!)
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Quá {timeoutMs}ms không thể tải được trang {GetType().Name}.");
        }
        finally
        {
            await cts.CancelAsync();

            readyTask.FireAndForgetSafe();
            urlDeadTask.FireAndForgetSafe();
            popupDeadTask.FireAndForgetSafe();
        }
    }
    
    public async Task<string> GetUserInfoAsync(CancellationToken ct = default)
    {
        var locator = Tab.NativePage.Locator(SpaUserInfoSelector);
        try 
        {
            await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
            return await locator.InnerTextAsync();
        }
        catch 
        {
            return string.Empty;
        }
    }

    protected override async Task<bool> IsSessionExpiredAsync()
    {
        if (await base.IsSessionExpiredAsync()) return true;

        return await Tab.NativePage
            .Locator(InvalidSessionSelector)
            .IsVisibleAsync();
    }

    protected virtual bool IsInsideSpaHost()
    {
        return Tab.CurrentUrl.StartsWith(CtuRoutes.DkmhRoot, StringComparison.OrdinalIgnoreCase);
    }

    protected abstract Task NavigateToFormSideBarAsync();

    public override async Task<bool> IsActiveAsync()
    {
        if (!IsInsideSpaHost()) return false;
        return await Tab.NativePage.Locator(PageReadySelector).IsVisibleAsync();
    }

    public async Task<StudentProfile?> GetStudentProfileAsync(CancellationToken cancellationToken = default)
    {
        var locator = Tab.NativePage.Locator(SpaUserInfoSelector);
        try
        {
            await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
            var userText = await locator.InnerTextAsync();

            if (string.IsNullOrWhiteSpace(userText)) return null;

            var lines = userText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length >= 2)
            {
                var name = lines[0].Trim();
                var mssv = lines[1].Trim('(', ')', ' ');
                return new StudentProfile(mssv, name);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Fail to get student profile from SPA header");
        }

        return null;
    }
}