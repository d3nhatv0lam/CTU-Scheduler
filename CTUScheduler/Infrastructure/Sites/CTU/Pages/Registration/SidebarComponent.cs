using System.Threading.Tasks;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;

public class SidebarComponent(IWebTab tab)
{
    private const string RulesButtonSelector = "li[data-menu-id*='/dangkyhocphan/sinhvien/quydinhdangky']";
    private const string CatalogButtonSelector = "li[data-menu-id*='/dangkyhocphan/sinhvien/danhmuchocphan']";
    private const string RegistrationButtonSelector = "li[data-menu-id*='/dangkyhocphan/sinhvien/dangkyhocphan']";
    private const string ScheduleButtonSelector = "li[data-menu-id*='/dangkyhocphan/sinhvien/thoikhoabieu']";

    public async Task NavigateToRulesAsync() => await tab.NativePage.ClickAsync(RulesButtonSelector);
    public async Task NavigateToCatalogAsync() => await tab.NativePage.ClickAsync(CatalogButtonSelector);
    public async Task NavigateToRegistrationAsync() => await tab.NativePage.ClickAsync(RegistrationButtonSelector);
    public async Task NavigateToScheduleAsync() => await tab.NativePage.ClickAsync(ScheduleButtonSelector);
}