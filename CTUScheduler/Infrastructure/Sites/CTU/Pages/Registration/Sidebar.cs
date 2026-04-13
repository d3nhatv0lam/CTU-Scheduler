using System.Threading.Tasks;
using CTUScheduler.Infrastructure.DriverCore.Refactor;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;

public class Sidebar(IWebTab tab)
{
    private const string RulesButtonSelector = "li[data-menu-id*='/dangkyhocphan/sinhvien/quydinhdangky']";
    private const string CatalogButtonSelector = "li[data-menu-id*='/dangkyhocphan/sinhvien/danhmuchocphan']";

    public async Task NavigateToRulesAsync() => await tab.NativePage.ClickAsync(RulesButtonSelector);
    public async Task NavigateToCatalogAsync() => await tab.NativePage.ClickAsync(CatalogButtonSelector);
}