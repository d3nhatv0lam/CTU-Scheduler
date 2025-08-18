using CTUScheduler.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.UserSaves;

namespace CTUScheduler.AppServices.Services.Interfaces
{
    public interface IUserDataService
    {
        public ScheduleSave ScheduleSaved { get; }

        Task<bool> TryLoadUserDataAsync(string filePath);

        Task<bool> TrySaveUserDataAsync(string filePath);
    }
}
