using Avalonia.Media.Imaging;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Interfaces
{
    public interface ICTUWebDriverService
    {
        IObservable<RegistrationInformation> RegistrationInformationResponse { get; }
        Task GoToSignInPageAsync();
        Task<bool> TrySignInAsync(string userName, string password, string captcha);
        Task<Bitmap?> TryGetCaptchaImageAsync();

        Task<(string userName, string userMSSV)> TryGetUserInfomation();

        Task GoToRegistrationRulesPage();
    }
}
