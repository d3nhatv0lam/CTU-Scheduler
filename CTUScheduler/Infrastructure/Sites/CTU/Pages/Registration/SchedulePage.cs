using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using CTUScheduler.Infrastructure.DriverCore.Extensions;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Extensions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;
using CTUScheduler.Infrastructure.Sites.CTU.Routes;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;

public class SchedulePage : BaseRegistrationPage, ISchedulePage
{
    private const string TuitionFeeTabSelector = ".ant-tabs-tab-btn:has-text('Thông tin học phí')";
    
    public SchedulePage(IWebTab tab, IConnectivityService connectivityService, ILoggerFactory loggerFactory) : base(tab,
        connectivityService, loggerFactory)
    {
        TuitionFeeResponse = tab.JsonResponse.Where(x => x.Url.Contains("/thongtinhocphi"))
            .FilterPacketJson(x => x["data"]?["chitiethocphi"] is not null)
            .ParseCtuResponse<RawThongTinHocPhiPayload>()
            .Where(x => x is { IsSuccess: true, Content: not null })
            .Select(x => x.Content!);
    }

    public override string PageUrl => CtuRoutes.DkmhSchedule;
    protected override string PageReadySelector => "section.calendar";
    
    public IObservable<RawThongTinHocPhiPayload> TuitionFeeResponse { get; }
    public async Task NavigateToTuitionFeeAsync()
    {
        await Tab.NativePage.ClickAsync(TuitionFeeTabSelector);
    }

    protected override async Task NavigateToFormSideBarAsync()
    {
        await this.SidebarComponent.NavigateToScheduleAsync();
    }
}