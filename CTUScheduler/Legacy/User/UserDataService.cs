using System;
using System.Reactive.Disposables;
using System.Text.Json;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Helpers.Json;
using CTUScheduler.Core.Models.UserSaves;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Legacy.User
{
    public class UserDataService : IUserDataService , IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ILogger<UserDataService> _logger;
        public ScheduleSave ScheduleSaved { get; private set; }
        public UserDataService(ILogger<UserDataService> logger)
        {
            _logger = logger;
            ScheduleSaved = new ScheduleSave();
        }

        public void ClearScheduleSaved()
        {
            ScheduleSaved.Courses.Clear();
            ScheduleSaved.Courses.TrimExcess();
            ScheduleSaved.ScheduleTables.Clear();
            ScheduleSaved.ScheduleTables.TrimExcess();
            ScheduleSaved.LastSaved = DateTime.Now;
        }

        public async Task<bool> TryLoadUserDataAsync(string filePath, JsonSerializerOptions? options = null)
        {
            try
            { 
                var loadedData = await JsonHelper.DeserializeFromFileAsync<ScheduleSave>(filePath,options);
                ArgumentNullException.ThrowIfNull(loadedData);

                ScheduleSaved = loadedData;
                return true;
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }

        public async Task<bool> TrySaveUserDataAsync(string filePath, JsonSerializerOptions? options = null)
        {
            try
            {
                await JsonHelper.SerializeToFileAsync(filePath, ScheduleSaved,options);
                return true;
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
