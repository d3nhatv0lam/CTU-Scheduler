using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Raw;
using CTUScheduler.Core.Models.WebResponse;
using CTUScheduler.Infrastructure.DriverCore;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;

public class RegistrationRulesPage: RegistrationSpa, IRegistrationRulesPage
{
    private const string UserInfoButtonLable= "user";
    private const string UserSettingButtonLable= "user";
    private const string CtuDkmhInfoKeySelector = "li:has-text('Khóa học') p:nth-of-type(2)";
    private const string CtuDkmhInfoUnitSelector = "li:has-text('Đơn vị') p:nth-of-type(2)";
    protected override string PathRegexPattern => "/quydinhdangky";

    public IObservable<CtuApiBody<RawRegistrationInformation>> RawRegistrationInformationResponse =>
        WebDriverService.JsonResponse
            .Where(packet => packet.Url.Contains(PathRegexPattern))
            .FilterPacketJson(node => node["data"].HasFields<RawRegistrationInformation>(
                x => x.hocky,
                x => x.quyDinh,
                x => x.namhoc,
                x => x.thoiGianDangKy))
            .ParseResponse<RawRegistrationInformation>()
            .Where(res => res.IsSuccess)
            .OfType<CtuApiBody<RawRegistrationInformation>>();
        

    
    public RegistrationRulesPage(IWebDriverService webDriverService, ILoggerFactory logger) : base(webDriverService, logger)
    {
    }
    
    protected override async Task NavigateToViaSidebarAsync(CancellationToken cancellationToken = default)
    {
        await Sidebar.NavigateToRulesPageAsync(WebDriverService);
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