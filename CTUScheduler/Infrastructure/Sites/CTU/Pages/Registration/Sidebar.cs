using System.Threading.Tasks;
using CTUScheduler.Infrastructure.DriverCore;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;

public class Sidebar
{
    private const string RulesButtonSelector = "li[data-menu-id*='/dangkyhocphan/sinhvien/quydinhdangky']";
    private const string CatalogButtonSelector = "li[data-menu-id*='/dangkyhocphan/sinhvien/danhmuchocphan']";
    public async Task NavigateToRulesPageAsync(IWebDriverService webDriverService)
    {
        await webDriverService.GetLocator(RulesButtonSelector).ClickAsync();
    }
    public async Task NavigateToCatalogPageAsync(IWebDriverService webDriverService)
    {
        await webDriverService.GetLocator(CatalogButtonSelector).ClickAsync();
    }
}