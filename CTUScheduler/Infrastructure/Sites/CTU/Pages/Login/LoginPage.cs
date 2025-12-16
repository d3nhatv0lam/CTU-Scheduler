using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
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
    protected override string PageUrl => "https://htql.ctu.edu.vn/";
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

    public async Task<LoginResult> TryLoginAsync(string username, string password,
        CancellationToken cancelLoginToken = default)
    {
        using var internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancelLoginToken);
        try
        {
            // clean page trước khi thực hiện lại
            await CleanUpPage();

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

            var errorTask = Task.WhenAny(
                WebDriverService.CurrentPage.WaitForSelectorAsync(UsernameErrorSelector,
                    new() { State = WaitForSelectorState.Visible }).WaitAsync(internalCts.Token),
                WebDriverService.CurrentPage.WaitForSelectorAsync(PasswordErrorSelector,
                    new() { State = WaitForSelectorState.Visible }).WaitAsync(internalCts.Token),
                WebDriverService.CurrentPage.WaitForSelectorAsync(LoginFailSelector,
                    new() { State = WaitForSelectorState.Visible }).WaitAsync(internalCts.Token)
            );

            var completed = await Task.WhenAny(successTask, errorTask);

            if (completed == successTask)
            {
                await successTask;
                return LoginResult.Success();
            }

            var doneTask = await errorTask;
            var element = await doneTask;
            var errorMessage = element != null ? await element.InnerTextAsync() : "Lỗi không xác định";

            return LoginResult.Failed(errorMessage);
        }
        catch (OperationCanceledException)
        {
            return LoginResult.Failed("Hủy đăng nhập");
        }
        catch (TimeoutException)
        {
            return LoginResult.Failed("Quá thời gian phản hồi từ hệ thống!");
        }
        catch (NoInternetException)
        {
            return LoginResult.Failed("Không có kết nối mạng!");
        }
        catch (Exception)
        {
            return LoginResult.Failed("Vấn đề chưa xác định, Bạn hãy liên hệ với nhà phát triển để tìm cách khắc phục");
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