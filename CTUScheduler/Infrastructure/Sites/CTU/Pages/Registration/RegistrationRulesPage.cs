using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using CTUScheduler.Infrastructure.DriverCore.Extensions;
using CTUScheduler.Infrastructure.Services.Network;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Extensions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;

internal class UserInfoResult
{
    public string k { get; set; } = string.Empty;
    public string u { get; set; } = string.Empty;
}

public class RegistrationRulesPage : DkmhSpaPage, IRegistrationRulesPage
{
    public override string PageUrl => "https://dkmhfe.ctu.edu.vn/dangkyhocphan/sinhvien/quydinhdangky";
    protected override string PageReadySelector => UserInfoButtonSelector;

    private const string UserInfoButtonSelector = "#root .anticon-user";
    private const string UserSettingButtonSelector = ".anticon-setting";


    public IObservable<DkmhQddkCrawlerPayload> RawRegistrationInformationResponse { get; }

    public RegistrationRulesPage(IWebTab tab, IConnectivityService connectivityService, ILoggerFactory logger) :
        base(tab, connectivityService, logger)
    {
        RawRegistrationInformationResponse = Tab.JsonResponse
            .Where(packet => packet.Url.Contains("/quydinhdangky"))
            .FilterPacketJson(node =>
                node["data"].HasFields<DkmhQddkCrawlerPayload>(x => x.HocKy, x => x.DanhSachQuyDinh, x => x.NamHoc,
                    x => x.DanhSachThoiGianDangKy))
            .ParseCtuResponse<DkmhQddkCrawlerPayload>()
            .Where(res => res is { IsSuccess: true, Content: not null })
            .Select(x => x.Content!);
    }

    public async Task<(string userKey, string userUnit)> TryGetUserKeyAndUnitAsync()
    {
        try
        {
            await Tab.NativePage.ClickAsync(UserInfoButtonSelector);
            await Tab.NativePage.ClickAsync(UserSettingButtonSelector);
            
            await Tab.NativePage.WaitForSelectorAsync(".ant-modal-close");

            string jsCode = @"(args) => {
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
            }";

            // Ép thẳng kết quả vào Record UserInfoResult cực an toàn
            var result = await Tab.NativePage.EvaluateAsync<UserInfoResult>(jsCode, new
            {
                keyLabel = "Khóa học",
                unitLabel = "Đơn vị",
                closeSel = ".ant-modal-close"
            });

            return (result.k, result.u);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Fail to get student key and unit");
            return default;
        }
    }

    protected override async Task NavigateToFormSideBarAsync()
    {
        await this.Sidebar.NavigateToRulesAsync();
    }
}