using CTUScheduler.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CTUScheduler.Services.Interfaces
{
    public interface IUserDataService
    {
        public UserData _UserData { get; }

        public void LoadUserData();

        public void SaveUserData();
    }
}
