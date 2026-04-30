using System.Threading.Tasks;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;

public class SidebarComponent(IWebTab tab)
{
    private const string RulesButtonSelector = "li[data-menu-id*='/dangkyhocphan/sinhvien/quydinhdangky']";
    private const string CatalogButtonSelector = "li[data-menu-id*='/dangkyhocphan/sinhvien/danhmuchocphan']";
    private const string RegistrationButtonSelector = "li[data-menu-id*='/dangkyhocphan/sinhvien/dangkyhocphan']";

    public async Task NavigateToRulesAsync() => await tab.NativePage.ClickAsync(RulesButtonSelector);
    public async Task NavigateToCatalogAsync() => await tab.NativePage.ClickAsync(CatalogButtonSelector);
    public async Task NavigateToRegistrationAsync() => await tab.NativePage.ClickAsync(RegistrationButtonSelector);
}