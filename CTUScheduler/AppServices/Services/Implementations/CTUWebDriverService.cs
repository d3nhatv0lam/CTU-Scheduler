using Avalonia.Media;
using Avalonia.Media.Imaging;
using CTUScheduler.AppServices.Helpers;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Raw;
using Microsoft.Playwright;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Implementations
{
    public class CTUWebDriverService: ICTUWebDriverService, IDisposable
    {
        private readonly IWebDriverService _webDriverService;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IObservable<RegistrationInformation> _registrationInformationResponse;

        public IObservable<RegistrationInformation> RegistrationInformationResponse => _registrationInformationResponse;

        public CTUWebDriverService(IWebDriverService webDriverService)
        {
            _webDriverService = webDriverService;

            // Registration Rules Page Response
            _registrationInformationResponse =
                _webDriverService.JsonResponse
                .Select(rawJsonData => ToRegistrationRulesPageJsonData(rawJsonData))
                .WhereNotNull()
                // convert to class
                .Select(jsonData => JsonHelper.Deserialize<RawRegistrationInformation>((JsonElement)jsonData!))
                .WhereNotNull()
                .Select(x => );



        }

        #region SignIn
        /// <summary>
        /// If navigate fail, throw exception
        /// </summary>
        /// <exception cref="Exception"></exception>
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

                ILocator userLogged = _webDriverService.LocatorElement(AppConstants.CTU_HOME_USER_INFO);

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

        #region MainHome

        public async Task<(string userName, string userMSSV)> TryGetUserInfomation()
        {
            try 
            {
                string userName = string.Empty;
                string MSSV = string.Empty;
                ILocator userInformationElement = _webDriverService.LocatorElement(AppConstants.CTU_HOME_USER_INFO);
                string[] userInfoArray = (await userInformationElement.InnerTextAsync()).Split(" ");
                userName = string.Join(' ', userInfoArray[0..^1]);
                MSSV = userInfoArray[^1];
                return (userName, MSSV);
            }
            catch
            {
                return (string.Empty, string.Empty);
            }
        }

        #endregion

        #region DKMH_Quydinhdangky
        /// <summary>
        /// If navigate fail, throw exception
        /// </summary>
        /// <exception cref="Exception"></exception>
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
                    throw new Exception("Khong dung web roi");
                }
            }
            catch
            {
                Debug.WriteLine("Exception when GoToRegistrationRulesPage");
                throw;
            } 
        }

        private JsonElement? ToRegistrationRulesPageJsonData(JsonElement? rawJson)
        {
            if (rawJson is not JsonElement dataElement) return null;
            try
            {
                var jsonData = JsonHelper.ChangeRoot(dataElement, "data");
                return HasRequiredFields(dataElement) ? dataElement : null;
                //check valid
                bool HasRequiredFields(JsonElement element)
                {
                    return element.TryGetProperty("quyDinh", out _)
                        && element.TryGetProperty("namhoc", out _)
                        && element.TryGetProperty("hocky", out _)
                        && element.TryGetProperty("thoiGianDangKy", out _);
                }
            }
            catch
            {
                return null;
            }
        }

        #endregion

        public void Dispose()
        {
            
        }

        
    }
}
