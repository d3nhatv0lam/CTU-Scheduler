using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.DriverCore.Extensions;
using CTUScheduler.Infrastructure.DriverCore.Refactor.Page.Components;
using CTUScheduler.Infrastructure.Sites.CTU.Extensions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum.CourseData;
using CTUScheduler.Infrastructure.Sites.CTU.Response;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.DriverCore.Refactor.Page;

public class CourseCatalogPage: AppPage, IRequireSession
{
   public override string PageUrl => "https://dkmhfe.ctu.edu.vn/dangkyhocphan/sinhvien/danhmuchocphan";
    protected override string PageReadySelector => SearchButtonSelector;

    private const string AutoCompleteQueryPattern = "/getdatafilter";
    private const string AutoCompleteKey = "dkmh_tu_dien_hoc_phan_ma_auto_complete";
    private const string SearchBoxLabel = "Mã học phần";
    private const string SearchBoxSelector = $"//p[normalize-space()='{SearchBoxLabel}']/..//input";
    private const string SearchButtonSelector = "span[aria-label='search']";
    
    public SidebarComponent Sidebar { get; }

    public IObservable<CtuApiBody<List<QuickSelectCourse>>> AutoCompleteQueryResponse { get; } 
    public IObservable<CtuApiBody<RawCourse>> CourseCatalogResponse { get; }
    
    public CourseCatalogPage(IWebTab tab, ILoggerFactory loggerFactory) : base(tab, loggerFactory)
    {
        Sidebar = new SidebarComponent(tab); // Khởi tạo mảnh ghép

        // Đã đổi WebDriverService -> Tab
        AutoCompleteQueryResponse = Tab.JsonResponse
            .Where(packet => packet.Url.Contains(AutoCompleteQueryPattern))
            .FilterPacketJson(node => node["data"]?[AutoCompleteKey] is not null)
            .ParseCtuResponse<List<QuickSelectCourse>>(node => node["data"]?[AutoCompleteKey])
            .Where(res => res.IsSuccess)
            .OfType<CtuApiBody<List<QuickSelectCourse>>>();
        
        CourseCatalogResponse = Tab.JsonResponse
            .Where(packet => packet.Url.Contains("/danhmuchocphan"))
            .FilterPacketJson(node => node["data"].HasFields<RawCourse>(x => x.hoc_phan_info, x => x.data, x => x.tuan_max))
            .ParseCtuResponse<RawCourse>()
            .Where(res => res.IsSuccess)
            .OfType<CtuApiBody<RawCourse>>();
    }

    public async Task FillQueryAsync(string query)
    {
        if (string.IsNullOrEmpty(query)) return;
        await Tab.FillAsync(SearchBoxSelector, query);
    }

    public async Task SearchAsync()
    {
        await Tab.ClickAsync(SearchButtonSelector);
    }
}