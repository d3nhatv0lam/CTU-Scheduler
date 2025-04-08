using CTUScheduler.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CTUScheduler.Services
{
    public class UserDataService
    {
        public UserData _UserData { get; }
        public bool DataChanged { get; set; } = false;

        public UserDataService()
        {
            _UserData = new UserData();
        }

        public void LoadUserData()
        {

        }

        public void SaveUserData()
        {
            string json = JsonSerializer.Serialize(_UserData);
            Debug.WriteLine(json);
        }
    }
}
