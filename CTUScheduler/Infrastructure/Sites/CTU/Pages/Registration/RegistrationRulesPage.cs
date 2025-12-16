using System;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using CTUScheduler.AppServices.Helpers.Json;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Raw;
using CTUScheduler.Infrastructure.DriverCore;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Serilog;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;

public class RegistrationRulesPage: RegistrationSpa, IRegistrationRulesPage
{
    private const string UserInfoButtonLable= "user";
    private const string UserSettingButtonLable= "user";
    private const string CtuDkmhInfoKeySelector = "li:has-text('Khóa học') p:nth-of-type(2)";
    private const string CtuDkmhInfoUnitSelector = "li:has-text('Đơn vị') p:nth-of-type(2)";

    public IObservable<RegistrationInformation> RegistrationInformationResponse => WebDriverService.JsonResponse
        .Where(IsValidResponse)
        .Select(rawJson => JsonHelper.Deserialize<RawRegistrationInformation>(rawJson))
        .Where(x => x is not null)
        .SelectMany(async x =>
        {
            // get userKey(ID) & userUnit
            try
            {
                var user = await TryGetUserKeyAndUnitAsync();
                var result = x!.ToRegistrationInformation(user.userKey, user.userUnit);
                return Observable.Return(result);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Fail to Convert RegistrationInformation");
                return Observable.Empty<RegistrationInformation>();
            }
        })
        .Merge();

    
    public RegistrationRulesPage(IWebDriverService webDriverService, ILoggerFactory logger) : base(webDriverService, logger)
    {
    }
    
    protected override async Task NavigateToViaSidebarAsync(CancellationToken cancellationToken = default)
    {
        await Sidebar.NavigateToRulesPageAsync(WebDriverService);
    }
    
    private bool IsValidResponse(JsonElement jsonElement)
    {
        try
        {
            var jsonData = JsonHelper.ChangeRoot(jsonElement, "data");
            return HasRequiredFields(jsonData);
            //check valid
            bool HasRequiredFields(JsonElement element)
            {
                return element.TryGetProperty("quyDinh", out _)
                       && element.TryGetProperty("namhoc", out _)
                       && element.TryGetProperty("hocky", out _)
                       && element.TryGetProperty("thoiGianDangKy", out _);
            }
        }
        catch
        {
            return false;
        }
    }
    
    private async Task<(string userKey, string userUnit)> TryGetUserKeyAndUnitAsync()
    {
        try
        {
            await WebDriverService.CurrentPage!.GetByRole(AriaRole.Img,new () {Name = UserInfoButtonLable}).ClickAsync();
            await WebDriverService.CurrentPage!.GetByRole(AriaRole.Img,new () {Name = UserSettingButtonLable}).ClickAsync();

            var result = await Task.WhenAll(
                WebDriverService.GetLocator(CtuDkmhInfoKeySelector).InnerTextAsync()
                , WebDriverService.GetLocator(CtuDkmhInfoUnitSelector).InnerTextAsync());
            
            string userKey = result[0];
            string userUnit = result[1];
            
            await WebDriverService.GetLocator(".ant-modal-close").ClickAsync();
            
            return (userKey, userUnit);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex,"Fail to get student key and unit");
            return default;
        }
    }
}