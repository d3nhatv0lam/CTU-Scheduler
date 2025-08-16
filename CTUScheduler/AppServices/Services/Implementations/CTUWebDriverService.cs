using Avalonia.Media;
using Avalonia.Media.Imaging;
using CTUScheduler.AppServices.Helpers;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Raw;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Implementations
{
    public class CTUWebDriverService: ICTUWebDriverService, IDisposable
    {
        private readonly IWebDriverService _webDriverService;
        private readonly ILogger<CTUWebDriverService> _logger;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IObservable<RegistrationInformation> _registrationInformationResponse;
        private readonly IObservable<ObservableCollection<QuickSelectCourse>> _courseCatalogQuickSelectResponse;
        private readonly IObservable<Course> _courseCatalogResponse;

        public IObservable<RegistrationInformation> RegistrationInformationResponse => _registrationInformationResponse;
        public IObservable<ObservableCollection<QuickSelectCourse>> CourseCatalogQuickSelectResponse => _courseCatalogQuickSelectResponse;
        public IObservable<Course> CourseCatalogResponse => _courseCatalogResponse;

        public CTUWebDriverService(IWebDriverService webDriverService,ILogger<CTUWebDriverService> logger)
        {
            _webDriverService = webDriverService;
            _logger = logger;

            //Registration Rules Page Response
            _registrationInformationResponse =
                _webDriverService.JsonResponse
                .Select(rawJsonData => ToRegistrationRulesPageJsonData(rawJsonData))
                .WhereNotNull()
                // convert to class
                .Select(jsonData => JsonHelper.Deserialize<RawRegistrationInformation>((JsonElement)jsonData!))
                .WhereNotNull()
                .SelectMany(async x => 
                {
                    // get userKey(ID) & userUnit
                    var user = await TryGetUserKeyAndUnit();
                    return x.ToRegistrationInformation(user.userKey, user.userUnit);
                });

            // Course Catalog quick select response
            _courseCatalogQuickSelectResponse =
                _webDriverService.JsonResponse
                .Select(rawJasonData => ToCourseCatalogQuickSelectJsonData(rawJasonData))
                .WhereNotNull()
                .Select(jsonData => JsonHelper.Deserialize<ObservableCollection<QuickSelectCourse>>((JsonElement)jsonData!))
                .WhereNotNull();

            // Course Catalog response
            _courseCatalogResponse =
                 _webDriverService.JsonResponse
                .Select(rawJsonData => ToCourseCatalogJsonData(rawJsonData))
                .WhereNotNull()
                .Select(jsonData =>
                {
                    try
                    {
                        return JsonHelper.Deserialize<RawCourse>((JsonElement)jsonData!);
                    }
                    catch
                    {
                        _logger.LogDebug("Exception when Derserialize RawCourse, may by empty SearchBox");
                        return default!;
                    }

                })
                .WhereNotNull()
                .Select(rawCourse => rawCourse.ToCourse());

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

        public async Task<bool> TrySignInAsync(string userName, string password)
        {
            if (_webDriverService.GetPageUrl() == AppConstants.CTU_HOME_URL) 
                return true;
            try
            {
                ILocator userNameInput = _webDriverService.LocatorElement(AppConstants.CTU_SIGN_IN_USERNAME);
                ILocator passwordInput = _webDriverService.LocatorElement(AppConstants.CTU_SIGN_IN_PASSWORD);
                ILocator loginButton = _webDriverService.LocatorElement(AppConstants.CTU_SIGN_IN_BUTTON);

                await _webDriverService.FillElementAsync(userNameInput, userName);
                await _webDriverService.FillElementAsync(passwordInput, password);
                await _webDriverService.ClickNavigateElementAsync(loginButton);

                var waitUrl = _webDriverService.TryWaitForUrlAsync(AppConstants.CTU_HOME_URL_PATTERN);
                var validInput = IsSignInSuccess();

                //return await WaitUrl.Amb(validInput).FirstAsync().ToTask();
                var completed = await Task.WhenAny(waitUrl, validInput);
                return await completed;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> IsSignInSuccess()
        {
            var ILocators = new[]
            {
                _webDriverService.LocatorElement(AppConstants.CTU_SIGN_IN_USERNAME_ERROR),
                _webDriverService.LocatorElement(AppConstants.CTU_SIGN_IN_PASSWORD_ERROR),
                _webDriverService.LocatorElement(AppConstants.CTU_SIGN_IN_FAIL)
            };

            // kiểm tra element có display none không
            var tasks = ILocators.Select(locator =>
                    locator.EvaluateAsync<bool>("el => getComputedStyle(el).display === 'none'")
                ).ToArray();

            bool[] results = await Task.WhenAll<bool>(tasks);
            // display none hết => login thành công
            return results.All(x => x);
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
                    LocatorClickOptions options = new LocatorClickOptions()
                    {
                        Delay = 200
                    };
                    await _webDriverService.ClickNavigateElementAsync(navigateElement,options);
                }
                else
                 // Current url is DKMH page
                if (currentUrl.Contains(AppConstants.CTU_DKMH_URL_KEY))
                {
                    ILocator DKMH_RegistrationRulenavigateElement = _webDriverService.LocatorElement(AppConstants.CTU_DKMH_QUYDINHDANGKY_BUTTON);
                    await _webDriverService.ClickNavigateElementAsync(DKMH_RegistrationRulenavigateElement);
                }
                else
                {
                    throw new Exception($"Url: {currentUrl} khong thuoc quan ly cua service");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex + "Exception when GoToRegistrationRulesPage");
                throw;
            } 
        }

        private JsonElement? ToRegistrationRulesPageJsonData(JsonElement? rawJson)
        {
            if (rawJson is not JsonElement dataElement) return null;
            try
            {
                var jsonData = JsonHelper.ChangeRoot(dataElement, "data");
                return HasRequiredFields(jsonData) ? jsonData : null;
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

        private async Task<(string userKey, string userUnit)> TryGetUserKeyAndUnit()
        {
            try
            {
                string userKey = string.Empty;
                string userUnit = string.Empty;
                if (!_webDriverService.GetPageUrl().Contains(AppConstants.CTU_DKMH_URL_KEY)) 
                    throw new Exception("Not in DKMH page");

                ILocator userInfoTab = _webDriverService.LocatorElement(AppConstants.CTU_DKMH_INFO_TAB);
                await userInfoTab.ClickAsync();

                ILocator userInfoButton = _webDriverService.LocatorElement(AppConstants.CTU_DKMH_INFO_BUTTON);
                await userInfoButton.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible });
                await userInfoButton.ClickAsync();

                await _webDriverService.LocatorElement(".ant-modal-content").WaitForAsync(new() { State = WaitForSelectorState.Visible });
                await _webDriverService.LocatorElement(".ant-modal-mask").WaitForAsync(new() { State = WaitForSelectorState.Visible });

                ILocator userKeyElement = _webDriverService.LocatorElement(AppConstants.CTU_DKMH_INFO_KEY);
                ILocator userUnitElement = _webDriverService.LocatorElement(AppConstants.CTU_DKMH_INFO_UNIT);

                var result = await Task.WhenAll(userKeyElement.InnerTextAsync(), userUnitElement.InnerTextAsync());
                userKey = result[0].ToString()!;
                userUnit = result[1].ToString()!;

                // close dialog
                await _webDriverService.LocatorElement(".ant-modal-close").WaitForAsync(new() { State = WaitForSelectorState.Visible });
                ILocator userInfoCloseButton = _webDriverService.LocatorElement(AppConstants.CTU_DKMH_INFO_CLOSE_BUTTON);
                await userInfoCloseButton.ClickAsync();


                return (userKey, userUnit);
            }
            catch
            {
                return (string.Empty, string.Empty);
            }
           
        }

        #endregion

        #region DKMH_DanhMucHocPhan

        public async Task GoToCourseCatalogPage()
        {
            string currentUrl = _webDriverService.GetPageUrl();
            try
            {
                // Current url is DKMH page
                if (currentUrl.Contains(AppConstants.CTU_DKMH_URL_KEY))
                {
                    ILocator navigateElement = _webDriverService.LocatorElement(AppConstants.CTU_DKMH_DANHMUCHOCPHAN_BUTTON);
                    await _webDriverService.ClickNavigateElementAsync(navigateElement);
                }
                else
                {
                    throw new Exception($"Url: {currentUrl} khong thuoc quan ly cua service");
                }
            }
            catch
            {
                _logger.LogError("Exception when GoToCourseCatalogPage");
                throw;
            }
        }

        private JsonElement? ToCourseCatalogQuickSelectJsonData(JsonElement? rawJson)
        {
            if (rawJson is not JsonElement dataElement) return null;

            try
            {
                var jsonData = JsonHelper.ChangeRoot(dataElement, "data");
                return JsonHelper.ChangeRoot(jsonData, "dkmh_tu_dien_hoc_phan_ma_auto_complete");
            }
            catch
            {
                return null;
            }
        }

        public async Task FillCourseKey(string courseStr)
        {
            try
            {
                ILocator courseKeyInput = _webDriverService.LocatorElement(AppConstants.CTU_DKMH_DANHMUCHOCPHAN_SEARCHBOX);
                await _webDriverService.FillElementAsync(courseKeyInput, courseStr);
            }
            catch
            {
                _logger.LogError("Exception when FillCourseKey");
            }
        }

        public async Task SearchCourse(string courseKey)
        {
            try
            {
                await FillCourseKey(courseKey);

                ILocator searchButton = _webDriverService.LocatorElement(AppConstants.CTU_DKMH_DANHMUCHOCPHAN_SEARCH_BUTTON);
                await _webDriverService.ClickElementAsync(searchButton);
            }
            catch
            {
                _logger.LogError("Exception when SearchCourse");
            }
        }

        private JsonElement? ToCourseCatalogJsonData(JsonElement? rawJson)
        {
            if (rawJson is not JsonElement dataElement) return null;

            try
            {
                var jsonData = JsonHelper.ChangeRoot(dataElement, "data");
                return HasRequiredFields(jsonData) ? jsonData : null;

                //check valid
                bool HasRequiredFields(JsonElement element)
                {
                    return element.TryGetProperty("data", out _)
                        && element.TryGetProperty("tuan_max", out _)
                        && element.TryGetProperty("hoc_phan_info", out _);
                }

            }
            catch
            {
                _logger.LogError("Exception when ToCourseCatalogJsonData");
                return null;
            }
        }

        #endregion

        public void Dispose()
        {
            
        }

        
    }
}
