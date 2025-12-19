using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using CTUScheduler.Core.Models.WebResponse;
using CTUScheduler.Infrastructure.DriverCore;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;

public class CourseCatalogPage: RegistrationSpa, ICourseCatalogPage
{
    protected override string PageUrl => "https://dkmhfe.ctu.edu.vn/dangkyhocphan/sinhvien/danhmuchocphan";
    protected override string PathRegexPattern => "/danhmuchocphan";
    private const string AutoCompleteKey = "dkmh_tu_dien_hoc_phan_ma_auto_complete";
    private const string SearchBoxLabel = "Mã học phần";
    private const string SearchBoxSelector = $"//p[normalize-space()='{SearchBoxLabel}']/..//input";
    private const string SearchButtonSelector = "span[aria-label='search']";
    
    private IObservable<NetworkPacket> FilteredPacket => WebDriverService.JsonResponse
        .Where(packet => packet.Url.Contains(PathRegexPattern))
        .Publish()
        .RefCount();
    public IObservable<CtuApiBody<List<QuickSelectCourse>>> AutoCompleteQueryResponse => FilteredPacket
        .FilterPacketJson(node => node["data"]?[AutoCompleteKey] is not null)
        .ParseCtuResponse<List<QuickSelectCourse>>(node => node["data"]?[AutoCompleteKey])
        .Where(res => res.IsSuccess)
        .OfType<CtuApiBody<List<QuickSelectCourse>>>();
    public IObservable<CtuApiBody<RawCourse>> CourseCatalogResponse => FilteredPacket
        .FilterPacketJson(node => node["data"].HasFields<RawCourse>(
           x => x.hoc_phan_info,
           x => x.data,
           x => x.tuan_max))
        .ParseCtuResponse<RawCourse>()
        .Where(res => res.IsSuccess)
        .OfType<CtuApiBody<RawCourse>>();
    
    public CourseCatalogPage(IWebDriverService webDriverService,
        ILoggerFactory loggerFactory)
        :base(webDriverService, loggerFactory)
    {
    }
    

    protected override async Task NavigateToViaSidebarAsync(CancellationToken cancellationToken = default)
    {
        await Sidebar.NavigateToCatalogPageAsync(WebDriverService);
    }

    public async Task FillQueryAsync(string query)
    {
        if (string.IsNullOrEmpty(query) || !await IsActive.FirstAsync())
            return;
        var searchBox = WebDriverService.GetLocator(SearchBoxSelector);
        await searchBox.FillAsync(query);
    }

    public async Task SearchAsync()
    {
        if (!await IsActive.FirstAsync())
            return;
        var searchButton = WebDriverService.GetLocator(SearchButtonSelector);
        await searchButton.ClickAsync();
    }
}