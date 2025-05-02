using Avalonia.Media;
using Avalonia.Media.Imaging;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Core.Exceptions;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Implementations
{
    public class CTUWebDriverService: ICTUWebDriverService, IDisposable
    {
        private readonly IWebDriverService _webDriverService;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();


        public CTUWebDriverService(IWebDriverService webDriverService)
        {
            _webDriverService = webDriverService;
        }

        #region SignIn
        public async Task GoToSignInPageAsync()
        {
            string signInPageUrl = AppConstants.CTU_SIGN_IN_URL;
            await _webDriverService.GoToPageAsync(signInPageUrl);
        }

        public async Task<bool> TrySignInAsync(string userName, string password, string captcha)
        {
           try
           {
                ILocator userNameInput = _webDriverService.LocatorElement(AppConstants.CTU_SIGN_IN_USERNAME);
                ILocator passwordInput = _webDriverService.LocatorElement(AppConstants.CTU_SIGN_IN_PASSWORD);
                ILocator capchaInput = _webDriverService.LocatorElement(AppConstants.CTU_SIGN_IN_CAPCHA);
                ILocator loginButton = _webDriverService.LocatorElement(AppConstants.CTU_SIGN_IN_BUTTON);

                await _webDriverService.FillElementAsync(userNameInput, userName);
                await _webDriverService.FillElementAsync(passwordInput, password);
                await _webDriverService.FillElementAsync(capchaInput, captcha);
                await _webDriverService.ClickNavigateElementAsync(loginButton);

                ILocator userLogged = _webDriverService.LocatorElement("//*[@id=\"user-login\"]");

                if (await userLogged.IsVisibleAsync())
                    return true;
                else
                    return false;
           }
           catch
           {
                return false;
           }
        }

        public async Task<Bitmap?> TryGetCaptchaImageAsync()
        {
            try
            { 
                ILocator captchaImage = _webDriverService.LocatorElement(AppConstants.CTU_SIGN_IN_CAPCHA_IMAGE);
                await captchaImage.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible , Timeout = 5000});
                byte[]  imageBytes = await _webDriverService.GetImageToByteArrayAsync(captchaImage);
                if (imageBytes.Length == 0) return null;

                using var stream = new MemoryStream(imageBytes, writable: false);
                return new Bitmap(stream);
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region DKMH_Quydinhdangky

        public async Task GoToRegistrationRulesPage() 
        {
            string currentUrl = _webDriverService.GetPageUrl();
            try
            {
                // first Navigate to DKMH page
                if (currentUrl.Contains(AppConstants.CTU_HOME_URL))
                {
                    ILocator navigateElement = _webDriverService.LocatorElement(AppConstants.CTU_HOME_DKMH_BUTTON);
                    await _webDriverService.ClickNavigateElementAsync(navigateElement);
                }
                else
                 // Current url is DKMH page
                if (currentUrl.Contains(AppConstants.CTU_DKMH_URL_KEY))
                {
                    ILocator DKMHnavigateElement = _webDriverService.LocatorElement(AppConstants.CTU_DKMH_QUYDINHDANGKY_BUTTON);
                    await _webDriverService.ClickNavigateElementAsync(DKMHnavigateElement);
                }
                else
                {
                    Debug.WriteLine($"Url: {currentUrl} khong thuoc quan ly cua service");
                }
            }
            catch
            {
                Debug.WriteLine("Exception when GoToRegistrationRulesPage");
            } 
        }

        #endregion

        public void Dispose()
        {
            
        }

        
    }
}
