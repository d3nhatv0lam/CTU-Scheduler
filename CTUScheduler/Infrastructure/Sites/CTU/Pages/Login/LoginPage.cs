using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.WebResponse;
using CTUScheduler.Infrastructure.DriverCore;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Login;

public class LoginPage : CtuBasePage, ILoginPage
{
    private const string UsernameInputSelector = "#usernameUserInput";
    private const string PasswordInputSelector = "#password";
    private const string LoginButtonSelector = "#sign-in-button";
    private const string UsernameErrorSelector = "#usernameError";
    private const string PasswordErrorSelector = "#passwordError";
    private const string LoginFailSelector = "#error-msg";
    protected override string PageUrl => LOGIN_PAGE_URL;
    protected override string UriHost => "accounts.ctu.edu.vn";
    protected override string PathRegexPattern => LOGIN_URL_PATTERN;
    
    public LoginPage(IWebDriverService webDriverService, ILoggerFactory logger) :
        base(webDriverService, logger)
    {
    }
    
    public override async Task NavigateToAsync(int maxRetries = 3, CancellationToken cancellationToken = default)
    {
        if (await IsActive.FirstAsync())
            return;

        var currentRetry = 1;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await WebDriverService.GoToPageAsync(PageUrl);
                // Đợi input username xuất hiện
                await WebDriverService.CurrentPage!
                    .WaitForSelectorAsync(UsernameInputSelector, new() { Timeout = 10000 });
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                currentRetry++;

                if (maxRetries != -1 && currentRetry >= maxRetries)
                {
                    Logger.LogError(ex,$"Đã thất bại sau {currentRetry} lần thử.");
                    throw;
                }

                string limitLog = maxRetries == -1 ? "Infinite" : maxRetries.ToString();
                Logger.LogWarning($"Fail to Navigate ({currentRetry}/{limitLog}): {ex.Message}. Retrying...");

                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    public async Task LoginAsync(string username, string password,
        CancellationToken cancelLoginToken = default)
    {
        using var internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancelLoginToken);
        try
        {
            if (!await IsActive.FirstAsync())
                throw new InvalidOperationException("Page is not active");

            await CleanUpPage().ConfigureAwait(false);

            var userLocator = WebDriverService.GetLocator(UsernameInputSelector);
            var passwordLocator = WebDriverService.GetLocator(PasswordInputSelector);
            var loginButtonLocator = WebDriverService.GetLocator(LoginButtonSelector);
            
            await userLocator.FillAsync(username);
            await passwordLocator.FillAsync(password);
            await WebDriverService.ClickNavigateElementAsync(loginButtonLocator);

            var successTask = WebDriverService.CurrentPage!.WaitForFunctionAsync(
                $"() => location.hostname !== '{UriHost}' || !location.pathname.includes('{PathRegexPattern}')",
                null,
                new() { Timeout = 60000 }
            ).WaitAsync(internalCts.Token);

            var errorOption = new PageWaitForSelectorOptions()
            {
                Timeout = 0,
                State = WaitForSelectorState.Visible,
            };

            var errorTask = Task.WhenAny(
                WebDriverService.CurrentPage.WaitForSelectorAsync(UsernameErrorSelector, errorOption),
                WebDriverService.CurrentPage.WaitForSelectorAsync(PasswordErrorSelector, errorOption),
                WebDriverService.CurrentPage.WaitForSelectorAsync(LoginFailSelector, errorOption)
            ).WaitAsync(internalCts.Token);

            var winnerTask = await Task.WhenAny(successTask, errorTask).ConfigureAwait(false);

            if (winnerTask == successTask)
            {
                errorTask.FireAndForgetSafe();
                
                await successTask;
                return;
            }

            successTask.FireAndForgetSafe();
            
            var completedErrorTask = await errorTask.ConfigureAwait(false);
            var element = await completedErrorTask.ConfigureAwait(false);
            var errorMessage = element != null 
                ? await element.InnerTextAsync().ConfigureAwait(false) 
                : throw new Exception("element.InnerTextAsync() fail");

            throw new InvalidCredentialsException(errorMessage);
        }
        finally
        {
            await internalCts.CancelAsync();
        }
    }

    /// <summary>
    /// ẩn các thông báo lỗi giúp trạng thái trang về như ban đầu
    /// </summary>
    private async Task CleanUpPage()
    {
        await WebDriverService.CurrentPage!.EvaluateAsync(@"(selectors) => {
        selectors.forEach(sel => {
            const el = document.querySelector(sel);
            if (el) el.style.display = 'none';
        });
        }",
            new[] { UsernameErrorSelector, PasswordErrorSelector, LoginFailSelector });
    }
}