using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Text.Json;
using System.Threading.Tasks;
using CTUScheduler.Core.Helpers;
using CTUScheduler.Core.Models.UserSaves;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.User
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
