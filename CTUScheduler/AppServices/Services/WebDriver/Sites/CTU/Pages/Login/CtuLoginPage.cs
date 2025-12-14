using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.WebDriver.Core;
using CTUScheduler.AppServices.Services.WebDriver.Interfaces;
using CTUScheduler.AppServices.Services.WebDriver.Models;
using CTUScheduler.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.AppServices.Services.WebDriver.Sites.CTU.Pages.Login;

public class CtuLoginPage : BasePage<CtuLoginPage>, ICtuLoginPage
{
    private const string LoginUrl = "https://htql.ctu.edu.vn/";
    private const string CtuLoginHost = "accounts.ctu.edu.vn";
    private const string UsernameInputSelector = "#usernameUserInput";
    private const string PasswordInputSelector = "#password";
    private const string LoginButtonSelector = "#sign-in-button";
    private const string UsernameErrorSelector = "#usernameError";
    private const string PasswordErrorSelector = "#passwordError";
    private const string LoginFailSelector = "#error-msg";


    public CtuLoginPage(IWebDriverService webDriverService, ILogger<CtuLoginPage> logger) :
        base(webDriverService, logger)
    {
    }

    protected override string PageUrlPattern { get; } = "/authenticationendpoint/login.do";

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
                await _webDriverService.GoToPageAsync(LoginUrl);
                // Đợi input username xuất hiện
                await _webDriverService.CurrentPage!
                    .WaitForSelectorAsync(UsernameInputSelector, new() { Timeout = 10000 });
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                currentRetry++;

                if (maxRetries != -1 && currentRetry >= maxRetries)
                {
                    _logger.LogError($"Đã thất bại sau {currentRetry} lần thử. Lỗi cuối cùng: {ex.Message}");
                    throw;
                }

                string limitLog = maxRetries == -1 ? "Infinite" : maxRetries.ToString();
                _logger.LogWarning($"Fail to Navigate ({currentRetry}/{limitLog}): {ex.Message}. Retrying...");

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

            var userLocator = _webDriverService.GetLocator(UsernameInputSelector);
            var passwordLocator = _webDriverService.GetLocator(PasswordInputSelector);
            var loginButtonLocator = _webDriverService.GetLocator(LoginButtonSelector);

            await userLocator.FillAsync(username);
            await passwordLocator.FillAsync(password);
            await _webDriverService.ClickNavigateElementAsync(loginButtonLocator);

            var successTask = _webDriverService.CurrentPage!.WaitForFunctionAsync(
                $"() => location.hostname !== '{CtuLoginHost}' || !location.pathname.includes('{PageUrlPattern}')",
                null,
                new() { Timeout = 60000 }
            ).WaitAsync(internalCts.Token);

            var errorTask = Task.WhenAny(
                _webDriverService.CurrentPage.WaitForSelectorAsync(UsernameErrorSelector,
                    new() { State = WaitForSelectorState.Visible }).WaitAsync(internalCts.Token),
                _webDriverService.CurrentPage.WaitForSelectorAsync(PasswordErrorSelector,
                    new() { State = WaitForSelectorState.Visible }).WaitAsync(internalCts.Token),
                _webDriverService.CurrentPage.WaitForSelectorAsync(LoginFailSelector,
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
        await _webDriverService.CurrentPage!.EvaluateAsync(@"(selectors) => {
        selectors.forEach(sel => {
            const el = document.querySelector(sel);
            if (el) el.style.display = 'none';
        });
        }",
            new[] { UsernameErrorSelector, PasswordErrorSelector, LoginFailSelector });
    }

    protected override bool IsMatchUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        try
        {
            var uri = new Uri(url);
            return uri.Host == CtuLoginHost &&
                   uri.AbsolutePath.Contains(PageUrlPattern);
        }
        catch
        {
            return false;
        }
    }
}