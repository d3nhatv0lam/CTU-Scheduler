using System.Threading.Tasks;

namespace CTUScheduler.Infrastructure.DriverCore.Refactor.Page.Components;

public class SidebarComponent
{
    private readonly IWebTab _tab;
    private const string RulesButtonSelector = "li[data-menu-id*='/dangkyhocphan/sinhvien/quydinhdangky']";
    private const string CatalogButtonSelector = "li[data-menu-id*='/dangkyhocphan/sinhvien/danhmuchocphan']";

    public SidebarComponent(IWebTab tab) => _tab = tab;

    public async Task NavigateToRulesAsync() => await _tab.ClickAsync(RulesButtonSelector);
    public async Task NavigateToCatalogAsync() => await _tab.GetLocator(CatalogButtonSelector).ClickAsync();
}