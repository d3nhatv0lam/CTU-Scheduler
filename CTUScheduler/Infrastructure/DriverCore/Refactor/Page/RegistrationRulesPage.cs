using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.DriverCore.Extensions;
using CTUScheduler.Infrastructure.DriverCore.Refactor.Page.Components;
using CTUScheduler.Infrastructure.Sites.CTU.Extensions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum.Registration;
using CTUScheduler.Infrastructure.Sites.CTU.Response;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.DriverCore.Refactor.Page;

internal record UserInfoResult(string k, string u);

public class RegistrationRulesPage : AppPage
{
   public override string PageUrl => "https://dkmhfe.ctu.edu.vn/dangkyhocphan/sinhvien/quydinhdangky";
    protected override string PageReadySelector => UserInfoButtonSelector;

    private const string UserInfoButtonSelector = ".anticon-user";
    private const string UserSettingButtonSelector = ".anticon-setting";
    
    // COMPOSITION: Gắn Sidebar vào
    public SidebarComponent Sidebar { get; }
    
    public IObservable<CtuApiBody<RawRegistrationInformation>> RawRegistrationInformationResponse { get; }

    public RegistrationRulesPage(IWebTab tab, ILoggerFactory logger) : base(tab, logger)
    {
        Sidebar = new SidebarComponent(tab);

        RawRegistrationInformationResponse = Tab.JsonResponse
            .Where(packet => packet.Url.Contains("/quydinhdangky"))
            .FilterPacketJson(node => node["data"].HasFields<RawRegistrationInformation>(x => x.hocky, x => x.quyDinh, x => x.namhoc, x => x.thoiGianDangKy))
            .ParseCtuResponse<RawRegistrationInformation>()
            .Where(res => res.IsSuccess)
            .OfType<CtuApiBody<RawRegistrationInformation>>();
    }

    public async Task<(string userKey, string userUnit)> TryGetUserKeyAndUnitAsync()
    {
        try
        {
            await Tab.ClickAsync(UserInfoButtonSelector);
            await Tab.ClickAsync(UserSettingButtonSelector);
            
            // Script cào dữ liệu qua cổng EvaluateAsync chuẩn xác
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
            var result = await Tab.EvaluateAsync<UserInfoResult>(jsCode, new 
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
}