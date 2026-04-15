using System;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.DriverCore.Extensions;
using CTUScheduler.Infrastructure.DriverCore.Refactor;
using CTUScheduler.Infrastructure.Sites.Base;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Login;

public class LoginPage : AppPage, ILoginPage
{
    private const string UsernameInputSelector = "#usernameUserInput";
    private const string PasswordInputSelector = "#password";
    private const string LoginButtonSelector = "#sign-in-button";
    private const string UsernameErrorSelector = "#usernameError";
    private const string PasswordErrorSelector = "#passwordError";
    private const string LoginFailSelector = "#error-msg";

    public LoginPage(IWebTab tab, IConnectivityService connectivityService, ILoggerFactory logger) : base(tab,
        connectivityService, logger)
    {
    }

    public override string PageUrl => "https://htql.ctu.edu.vn/";
    protected override string PageReadySelector => UsernameInputSelector;

    public async Task FillCredentialsAsync(string username, string password)
    {
        await Tab.NativePage.FillAsync(UsernameInputSelector, username);
        await Tab.NativePage.FillAsync(PasswordInputSelector, password);
    }

    public async Task SubmitAsync()
    {
        await Tab.NativePage
            .Locator(LoginButtonSelector)
            .ClickAndWaitForLoadStateAsync(LoadState.Load);
    }

    public async Task<bool> HasErrorVisibleAsync()
    {
        string jsCheck = @"(selectors) => {
            return selectors.some(s => {
                const el = document.querySelector(s);
                return el && el.offsetWidth > 0 && el.offsetHeight > 0;
            });
         }";

        return await Tab.NativePage.EvaluateAsync<bool>(jsCheck,
            new[] { UsernameErrorSelector, PasswordErrorSelector, LoginFailSelector });
    }

    public async Task<string> GetErrorMessageAsync()
    {
        var page = Tab.NativePage;
        var usernameErrorLocator = page.Locator(UsernameErrorSelector);
        var passwordErrorLocator = page.Locator(PasswordErrorSelector);
        var loginFailLocator = page.Locator(LoginFailSelector);

        if (await usernameErrorLocator.IsVisibleAsync()) return await usernameErrorLocator.InnerTextAsync();
        if (await passwordErrorLocator.IsVisibleAsync()) return await passwordErrorLocator.InnerTextAsync();
        if (await loginFailLocator.IsVisibleAsync()) return await loginFailLocator.InnerTextAsync();

        return "Lỗi đăng nhập không xác định.";
    }

    public bool IsLoginSuccess()
    {
        return !CurrentUrl.Contains("authenticationendpoint/login.do", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<OperationResult> PerformLoginActionAsync(string username, string password,
        CancellationToken cancellationToken = default)
    {
        using var internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await CleanUpPageAsync();
        await FillCredentialsAsync(username, password);

        var (successTask, errorTask, timeoutTask) = CreateLoginRaceTasks(internalCts.Token);

        try
        {
            await SecureClickAsync(LoginButtonSelector);
            return await WaitForLoginResultAsync(successTask, errorTask, timeoutTask);
        }
        catch (NoInternetException ex)
        {
            return OperationResult.Failed(ex.Message, "Internet", OperationFailureReason.Network);
        }
        finally
        {
            await internalCts.CancelAsync();
        }
    }

    private (Task waitForSuccessTask, Task waitForErrorTask, Task timeoutTask) CreateLoginRaceTasks(
        CancellationToken cancellationToken)
    {
        string successJs = @"() => {
            const currentHost = window.location.hostname;
            const currentPath = window.location.pathname;
            return currentHost !== 'accounts.ctu.edu.vn' || !currentPath.includes('login.do');
        }";

        var waitForSuccessTask = Tab.NativePage.WaitForFunctionAsync(
            successJs, null, new PageWaitForFunctionOptions { Timeout = 0 }
        ).WaitAsync(cancellationToken);

        var errorOption = new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 0 };
        var waitForErrorTask = Task.WhenAny(
            Tab.NativePage.WaitForSelectorAsync(UsernameErrorSelector, errorOption),
            Tab.NativePage.WaitForSelectorAsync(PasswordErrorSelector, errorOption),
            Tab.NativePage.WaitForSelectorAsync(LoginFailSelector, errorOption)
        ).WaitAsync(cancellationToken);

        var timeoutTask = Task.Delay(15000, cancellationToken);

        return (waitForSuccessTask, waitForErrorTask, timeoutTask);
    }

    private async Task<OperationResult> WaitForLoginResultAsync(
        Task successTask, Task errorTask, Task timeoutTask)
    {
        var completedTask = await Task.WhenAny(successTask, errorTask, timeoutTask);

        if (completedTask == successTask)
        {
            return OperationResult.Success();
        }

        if (completedTask == errorTask)
        {
            await errorTask;
            var msg = await GetErrorMessageAsync();
            return OperationResult.Failed(msg, "Auth.Failed", OperationFailureReason.Validation);
        }

        return OperationResult.Failed(
            "Quá thời gian chờ (15s) phản hồi từ hệ thống CTU.",
            "Auth.Timeout",
            OperationFailureReason.Network);
    }


    private async Task CleanUpPageAsync()
    {
        string jsCode = $@"(selectors) => {{
            selectors.forEach(sel => {{
                const el = document.querySelector(sel);
                if (el) el.style.display = 'none';
            }});
        }}";

        await Tab.NativePage.EvaluateAsync(jsCode,
            new[] { UsernameErrorSelector, PasswordErrorSelector, LoginFailSelector });
    }
}