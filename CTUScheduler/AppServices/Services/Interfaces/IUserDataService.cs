using CTUScheduler.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Interfaces
{
    public interface IUserDataService
    {
        public UserData _UserData { get; }

        public void LoadUserData(string fileName);

        public void SaveUserData(string fileName);
    }
}
