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

public class RegistrationRulesPage : RegistrationSpa, IRegistrationRulesPage
{
    private const string UserInfoButtonLable = ".anticon-user";
    private const string UserSettingButtonLable = ".anticon-setting";
    private const string CtuDkmhInfoKeySelector = "li:has-text('Khóa học') p:nth-of-type(2)";
    private const string CtuDkmhInfoUnitSelector = "li:has-text('Đơn vị') p:nth-of-type(2)";
    protected override string PathRegexPattern => "/quydinhdangky";
    public IObservable<CtuApiBody<RawRegistrationInformation>> RawRegistrationInformationResponse { get; }
    public RegistrationRulesPage(IWebDriverService webDriverService, ILoggerFactory logger) : base(webDriverService,
        logger)
    {
        RawRegistrationInformationResponse = WebDriverService.JsonResponse
            .Where(packet => packet.Url.Contains(PathRegexPattern))
            .FilterPacketJson(node => node["data"].HasFields<RawRegistrationInformation>(
                x => x.hocky,
                x => x.quyDinh,
                x => x.namhoc,
                x => x.thoiGianDangKy))
            .ParseCtuResponse<RawRegistrationInformation>()
            .Where(res => res.IsSuccess)
            .OfType<CtuApiBody<RawRegistrationInformation>>();
    }

    protected override async Task NavigateToViaSidebarAsync(CancellationToken cancellationToken = default)
    {
        await Sidebar.NavigateToRulesPageAsync(WebDriverService);
    }

    public async Task<(string userKey, string userUnit)> TryGetUserKeyAndUnitAsync()
    {
        try
        {
            await WebDriverService
                .GetLocator(UserInfoButtonLable)
                .ClickAsync()
                .ConfigureAwait(false);
            
            await WebDriverService
                .GetLocator(UserSettingButtonLable)
                .ClickAsync()
                .ConfigureAwait(false);
            
            await WebDriverService.GetLocator(CtuDkmhInfoKeySelector)
                .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached })
                .ConfigureAwait(false);
            
            var result = await WebDriverService.CurrentPage!.EvaluateAsync<dynamic>(@"(args) => {
                const getVal = (label) => {
                    const li = Array.from(document.querySelectorAll('li')).find(el => el.textContent.includes(label));
                    return li ? li.querySelector('p:nth-of-type(2)')?.textContent.trim() : '';
                };

                const k = getVal(args.keyLabel);
                const u = getVal(args.unitLabel);

                const closeBtn = document.querySelector(args.closeSel);
                if (closeBtn) {
                    closeBtn.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, view: window }));
                }

                return { k, u };
            }", new { 
                keyLabel = "Khóa học", 
                unitLabel = "Đơn vị", 
                closeSel = ".ant-modal-close" 
            }).ConfigureAwait(false);

            return (result.k.ToString(), result.u.ToString());
            
            // var result = await Task.WhenAll(
            //     WebDriverService.GetLocator(CtuDkmhInfoKeySelector).InnerTextAsync()
            //     , WebDriverService.GetLocator(CtuDkmhInfoUnitSelector).InnerTextAsync()
            // ).ConfigureAwait(false);
            //
            // string userKey = result[0];
            // string userUnit = result[1];
            //
            // await WebDriverService.GetLocator(".ant-modal-close")
            //     .ClickAsync()
            //     .ConfigureAwait(false);
            //
            // return (userKey, userUnit);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Fail to get student key and unit");
            return default;
        }
    }
}