using System.Threading.Tasks;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface IMainPage: IStudentInfoPage
{
    Task NavigateToDkmhAsync();
}