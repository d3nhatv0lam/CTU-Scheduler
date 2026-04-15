using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.DriverCore.Extensions;
using CTUScheduler.Infrastructure.DriverCore.Refactor;
using CTUScheduler.Infrastructure.Services.Network;
using CTUScheduler.Infrastructure.Sites.CTU.Extensions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum.CourseData;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;

public class CourseCatalogPageRefactor : DkmhSpaPage
{
    public override string PageUrl => "https://dkmhfe.ctu.edu.vn/dangkyhocphan/sinhvien/danhmuchocphan";
    protected override string PageReadySelector => SearchButtonSelector;

    private const string AutoCompleteQueryPattern = "/getdatafilter";
    private const string AutoCompleteKey = "dkmh_tu_dien_hoc_phan_ma_auto_complete";
    private const string SearchBoxLabel = "Mã học phần";
    private const string SearchBoxSelector = $"//p[normalize-space()='{SearchBoxLabel}']/..//input";
    private const string SearchButtonSelector = "span[aria-label='search']";

    public IObservable<List<QuickSelectCourse>> AutoCompleteQueryResponse { get; }
    public IObservable<RawCourse> CourseCatalogResponse { get; }

    public CourseCatalogPageRefactor(IWebTab tab, ConnectivityService connectivityService, ILoggerFactory loggerFactory)
        : base(tab, connectivityService, loggerFactory)
    {
        AutoCompleteQueryResponse = Tab.JsonResponse
            .Where(packet => packet.Url.Contains(AutoCompleteQueryPattern))
            .FilterPacketJson(node => node["data"]?[AutoCompleteKey] is not null)
            .ParseCtuResponse<List<QuickSelectCourse>>(node => node["data"]?[AutoCompleteKey])
            .Where(res => res is { IsSuccess: true, Content: not null })
            .Select(x => x.Content!);


        CourseCatalogResponse = Tab.JsonResponse
            .Where(packet => packet.Url.Contains("/danhmuchocphan"))
            .FilterPacketJson(node =>
                node["data"].HasFields<RawCourse>(x => x.hoc_phan_info, x => x.data, x => x.tuan_max))
            .ParseCtuResponse<RawCourse>()
            .Where(res => res is { IsSuccess: true, Content: not null })
            .Select(x => x.Content!);
    }

    public async Task FillQueryAsync(string query)
    {
        if (string.IsNullOrEmpty(query)) return;
        await Tab.NativePage.FillAsync(SearchBoxSelector, query);
    }

    public async Task SearchAsync()
    {
        await Tab.NativePage.ClickAsync(SearchButtonSelector);
    }

    protected override async Task NavigateToFormSideBarAsync()
    {
        await this.Sidebar.NavigateToCatalogAsync();
    }
}