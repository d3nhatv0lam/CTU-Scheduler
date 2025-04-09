using CTUScheduler.Models;
using CTUScheduler.Services.Interfaces;
using CTUScheduler.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CTUScheduler.Services
{
    public class UserDataService : IUserDataService , IDisposable
    {
        private CompositeDisposable _disposables = new CompositeDisposable();
        public UserData _UserData { get; private set; }
        public bool DataChanged { get; set; } = false;

        public UserDataService()
        {
            _UserData = new UserData();
            _disposables.Add(_UserData);
        }

        public void LoadUserData(string fileName)
        {
            try
            {
                var loadedData = JsonHelper.Deserialize<UserData>(fileName);
                if (loadedData == null) throw new Exception("Loaded Fail!");

                _UserData = loadedData;
            }
            catch (Exception ex) 
            {
                Debug.WriteLine(ex);
            }
            
        }

        public void SaveUserData(string fileName)
        {
            try
            {
                string json = JsonHelper.Serialize<UserData>(_UserData);
                Debug.WriteLine(json);
            }
            catch
            {

            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
