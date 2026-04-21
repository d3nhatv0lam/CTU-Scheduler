using System;
using System.Collections.Generic;
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

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;

public abstract class DkmhSpaPage : AppPage, IRequireSession
{
    private const string InvalidSessionSelector = "text='Bạn hiện không có quyền truy cập vào hệ thống'";
    protected Sidebar Sidebar { get; }

    protected DkmhSpaPage(IWebTab tab, IConnectivityService connectivityService, ILoggerFactory loggerFactory) : base(
        tab, connectivityService, loggerFactory)
    {
        Sidebar = new Sidebar(tab);
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

    public override async Task WaitForReadyAsync(int timeoutMs = 10000)
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
                throw new SessionExpiredException($"Mất session tại {GetType().Name} (UrlRedirect)");
            }
        
            if (winner == popupDeadTask)
            {
                throw new SessionExpiredException($"Mất session tại {GetType().Name} (BPopup)");
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
}