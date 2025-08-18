using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.AppServices.Helpers;
using CTUScheduler.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.UserSaves;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.Implementations
{
    public class UserDataService : IUserDataService , IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ILogger<UserDataService> _logger;
        public ScheduleSave ScheduleSaved { get; private set; }
        public bool DataChanged { get; set; } = false;

        public UserDataService(ILogger<UserDataService> logger)
        {
            _logger = logger;
            ScheduleSaved = new ScheduleSave();
        }

        public async Task<bool> TryLoadUserDataAsync(string filePath)
        {
            try
            {
                var loadedData = await JsonHelper.DeserializeFromFileAsync<ScheduleSave>(filePath);
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

        public async Task<bool> TrySaveUserDataAsync(string filePath)
        {
            try
            {
                await JsonHelper.SerializeToFileAsync(filePath, ScheduleSaved);
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
