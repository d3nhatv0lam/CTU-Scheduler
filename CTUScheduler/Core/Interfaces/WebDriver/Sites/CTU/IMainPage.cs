using System.Threading.Tasks;

namespace CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;

public interface IMainPage: ISitePage
{
    Task<string> GetUserInfoAsync();
    Task NavigateToDkmhAsync();
}