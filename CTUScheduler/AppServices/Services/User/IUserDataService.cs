using System.Threading.Tasks;
using CTUScheduler.Core.Models.UserSaves;

namespace CTUScheduler.AppServices.Services.User
{
    public interface IUserDataService
    {
        public ScheduleSave ScheduleSaved { get; }

        Task<bool> TryLoadUserDataAsync(string filePath);

        Task<bool> TrySaveUserDataAsync(string filePath);
    }
}
