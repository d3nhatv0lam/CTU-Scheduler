using System;
using System.Threading.Tasks;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.DriverCore.Refactor;
using CTUScheduler.Infrastructure.DriverCore.Refactor.Page;
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

    public LoginPage(IWebTab tab, ILoggerFactory logger) : base(tab, logger)
    {
    }

    public override string PageUrl => "https://htql.ctu.edu.vn/";
    protected override string PageReadySelector => UsernameInputSelector;

    public async Task FillCredentialsAsync(string username, string password)
    {
        await CleanUpPageAsync();

        await Tab.FillAsync(UsernameInputSelector, username);
        await Tab.FillAsync(PasswordInputSelector, password);
    }

    public async Task SubmitAsync()
    {
        await Tab.ClickNavigateElementAsync(LoginButtonSelector, loadState: LoadState.NetworkIdle);
    }

    public async Task<bool> HasErrorVisibleAsync()
    {
        string jsCheck = @"(selectors) => {
            return selectors.some(s => {
                const el = document.querySelector(s);
                return el && el.offsetWidth > 0 && el.offsetHeight > 0;
            });
         }";

        return await Tab.EvaluateAsync<bool>(jsCheck,
            new[] { UsernameErrorSelector, PasswordErrorSelector, LoginFailSelector });
    }

    public async Task<string> GetErrorMessageAsync()
    {
        if (await Tab.IsVisibleAsync(UsernameErrorSelector)) return await Tab.GetTextAsync(UsernameErrorSelector);
        if (await Tab.IsVisibleAsync(PasswordErrorSelector)) return await Tab.GetTextAsync(PasswordErrorSelector);
        if (await Tab.IsVisibleAsync(LoginFailSelector)) return await Tab.GetTextAsync(LoginFailSelector);

        return "Lỗi đăng nhập không xác định.";
    }

    public bool IsLoginSuccess()
    {
        return !CurrentUrl.Contains("authenticationendpoint/login.do", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<OperationResult> PerformLoginActionAsync(string username, string password)
    {
        await FillCredentialsAsync(username, password);
        await SubmitAsync();
        
        //TODO
        
        if (IsLoginSuccess()) return OperationResult.Success();
    
        return OperationResult.Failed("Hệ thống không phản hồi.");
    }
    

    private async Task CleanUpPageAsync()
    {
        string jsCode = $@"(selectors) => {{
            selectors.forEach(sel => {{
                const el = document.querySelector(sel);
                if (el) el.style.display = 'none';
            }});
        }}";

        // Gọi JS qua cổng EvaluateActionAsync an toàn của IWebTab
        await Tab.EvaluateActionAsync(jsCode,
            new[] { UsernameErrorSelector, PasswordErrorSelector, LoginFailSelector });
    }
}