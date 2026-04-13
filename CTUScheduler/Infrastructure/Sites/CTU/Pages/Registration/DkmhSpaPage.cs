using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Infrastructure.DriverCore.Refactor;
using CTUScheduler.Infrastructure.Sites.Base;
using CTUScheduler.Infrastructure.Sites.CTU.Routes;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;

public abstract class DkmhSpaPage : AppPage, IRequireSession
{
    private const string InvalidSessionSelector = "text='Bạn hiện không có quyền truy cập vào hệ thống'";
    protected Sidebar Sidebar { get; }

    protected DkmhSpaPage(IWebTab tab, ILoggerFactory loggerFactory) : base(tab, loggerFactory)
    {
        Sidebar = new Sidebar(tab);
    }


    public override async Task NavigateToAsync(PageGotoOptions? options = null)
    {
        if (await IsActiveAsync())
            return;

        if (IsInsideSpaHost())
        {
            await NavigateToFormSideBarAsync();
            return;
        }

        await base.NavigateToAsync(options);
    }

    public override async Task WaitForReadyAsync(int timeoutMs = 10000)
    {
        //  Trang đích xuất hiện DOM thành công
        async Task<string> WaitReady()
        {
            try
            {
                await Tab.NativePage.Locator(PageReadySelector)
                    .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
                return "READY";
            }
            catch
            {
                return null!;
            }
        }

        // Bị Server đá về URL đăng nhập
        async Task<string> WaitUrlDead()
        {
            try
            {
                await Tab.NativePage.WaitForURLAsync(CtuRoutes.AuthRedirectRegex, new() { Timeout = timeoutMs });
                return "URL_DEAD";
            }
            catch
            {
                return null!;
            }
        }

        // Luồng 3: Trang SPA nhảy bềnh Popup mất Session
        async Task<string> WaitPopupDead()
        {
            if (string.IsNullOrEmpty(InvalidSessionSelector))
            {
                await Task.Delay(timeoutMs);
                return null!;
            }

            try
            {
                await Tab.NativePage.Locator(InvalidSessionSelector)
                    .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
                return "POPUP_DEAD";
            }
            catch
            {
                return null!;
            }
        }

        // Ném cả 3 luồng vào chạy song song
        var tasks = new List<Task<string>> { WaitReady(), WaitUrlDead(), WaitPopupDead() };
        while (tasks.Count > 0)
        {
            // Bắt đền thằng nào chạy xong đầu tiên
            var finishedTask = await Task.WhenAny(tasks);
            tasks.Remove(finishedTask);
            var result = await finishedTask;
            if (result == "URL_DEAD" || result == "POPUP_DEAD")
            {
                throw new SessionExpiredException(
                    $"Mất session tại {GetType().Name} ({(result == "POPUP_DEAD" ? "BPopup" : "UrlRedirect")})");
            }

            if (result == "READY")
            {
                return;
            }
        }

        throw new TimeoutException($"Quá {timeoutMs}ms không thể tải được trang {GetType().Name}.");
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