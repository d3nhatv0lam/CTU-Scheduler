using System;
using System.Collections.Generic;
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

public class CourseRegistrationPage : BaseRegistrationPage, ICourseRegistrationPage
{
    private const string UrlPattern = "/hocphandadangky";
    public CourseRegistrationPage(IWebTab tab,
        IConnectivityService connectivityService,
        ILoggerFactory loggerFactory) :
        base(tab,
            connectivityService,
            loggerFactory)
    {
        CourseRegistrationResponse = tab.JsonResponse
            .Where(packet => packet.Url.Contains(UrlPattern))
            .FilterPacketJson(node => node["data"]?["data"] is not null)
            .ParseCtuResponse<List<RawDkhpPayload>>(node => node["data"]?["data"])
            .Where(x => x is { IsSuccess: true, Content: not null })
            .Select(x => x.Content!);
    }

    public override string PageUrl => CtuRoutes.DkmhRegistration;
    protected override string PageReadySelector => "div.table__layout .ant-table-tbody";
    
    public IObservable<List<RawDkhpPayload>> CourseRegistrationResponse { get; }

    protected override async Task NavigateToFormSideBarAsync()
    {
        await this.SidebarComponent.NavigateToRegistrationAsync();
    }

    
}