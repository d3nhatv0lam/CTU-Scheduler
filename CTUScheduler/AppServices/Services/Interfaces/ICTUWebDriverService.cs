using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Interfaces
{
    public interface ICTUWebDriverService
    {

        Task GoToSignInPageAsync();
        Task<bool> TrySignInAsync(string userName, string password, string captcha);
        Task<Bitmap?> TryGetCaptchaImageAsync();

        Task GoToRegistrationRulesPage();
    }
}
