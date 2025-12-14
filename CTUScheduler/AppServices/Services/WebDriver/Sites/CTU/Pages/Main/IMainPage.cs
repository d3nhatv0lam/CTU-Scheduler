using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.WebDriver.Interfaces;

namespace CTUScheduler.AppServices.Services.WebDriver.Sites.CTU.Pages.Main;

public interface IMainPage: ISitePage
{
    Task<string> GetUserInfoAsync();
    Task NavigateToDkmhAsync();
}