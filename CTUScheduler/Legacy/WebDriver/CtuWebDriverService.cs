using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CTUScheduler.AppServices;
using CTUScheduler.AppServices.Helpers.Json;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Infrastructure.DriverCore;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace CTUScheduler.Legacy.WebDriver
{
    public class CtuWebDriverService: ICTUWebDriverService, IDisposable
    {
        private readonly IWebDriverService _webDriverService;
        private readonly ILogger<CtuWebDriverService> _logger;

        public IObservable<RegistrationInformation> RegistrationInformationResponse { get; }
        public IObservable<ObservableCollection<QuickSelectCourse>> CourseCatalogQuickSelectResponse { get; }
        public IObservable<Course> CourseCatalogResponse { get; }

        public CtuWebDriverService(IWebDriverService webDriverService,ILogger<CtuWebDriverService> logger)
        {
            _webDriverService = webDriverService;
            _logger = logger;

            // //Registration Rules Page Response
            // RegistrationInformationResponse =
            //     _webDriverService.JsonResponse
            //     .Select(rawJsonData => ToRegistrationRulesPageJsonData(rawJsonData))
            //     .WhereNotNull()
            //     // convert to class
            //     .Select(jsonData => JsonHelper.Deserialize<RawRegistrationInformation>((JsonElement)jsonData!))
            //     .WhereNotNull()
            //     .SelectMany(async x => 
            //     {
            //         // get userKey(ID) & userUnit
            //         try
            //         {
            //             var user = await TryGetUserKeyAndUnit();
            //             return x.ToRegistrationInformation(user.userKey, user.userUnit);
            //         }
            //         catch (Exception e)
            //         {
            //             _logger.LogWarning(e,"Failed to get user key and user unit from CTUWebDriverService");
            //             return null!;
            //         }
            //     })
            //     .WhereNotNull();
            //
            //
            // Course Catalog quick select response
            // CourseCatalogQuickSelectResponse =
            //     _webDriverService.JsonResponse
            //     .Select(rawJasonData => ToCourseCatalogQuickSelectJsonData(rawJasonData))
            //     .WhereNotNull()
            //     .Select(jsonData => JsonHelper.Deserialize<ObservableCollection<QuickSelectCourse>>((JsonElement)jsonData!))
            //     .WhereNotNull()
            //     .Publish()
            //     .RefCount();
            //
            // // Course Catalog response
            // CourseCatalogResponse =
            //      _webDriverService.JsonResponse
            //     .Select(rawJsonData => ToCourseCatalogJsonData(rawJsonData))
            //     .WhereNotNull()
            //     .Select(jsonData =>
            //     {
            //         try
            //         {
            //             return JsonHelper.Deserialize<RawCourse>((JsonElement)jsonData!);
            //         }
            //         catch (Exception e)
            //         {
            //             _logger.LogWarning(e,"Exception when Deserialize RawCourse, may by empty SearchBox");
            //             return null;
            //         }
            //     })
            //     .WhereNotNull()
            //     .Select(rawCourse => rawCourse.ToCourse())
            //     .Publish()
            //     .RefCount();

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
            if (_webDriverService.PageUrl == AppConstants.CTU_HOME_URL) 
                return true;
            try
            {
                ILocator userNameInput = _webDriverService.GetLocator(AppConstants.CTU_SIGN_IN_USERNAME);
                ILocator passwordInput = _webDriverService.GetLocator(AppConstants.CTU_SIGN_IN_PASSWORD);
                ILocator loginButton = _webDriverService.GetLocator(AppConstants.CTU_SIGN_IN_BUTTON);

                // await _webDriverService.FillElementAsync(userNameInput, userName);
                // await _webDriverService.FillElementAsync(passwordInput, password);
                await userNameInput.FillAsync(userName);
                await passwordInput.FillAsync(password);
                await _webDriverService.ClickNavigateElementAsync(loginButton);

                var waitUrl = _webDriverService.TryWaitForUrlAsync(AppConstants.CTU_HOME_URL_PATTERN);
                var validInput = IsSignInSuccess();
                
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
            await Task.Delay(300);
            
            var iLocators = new[]
            {
                _webDriverService.GetLocator(AppConstants.CTU_SIGN_IN_USERNAME_ERROR),
                _webDriverService.GetLocator(AppConstants.CTU_SIGN_IN_PASSWORD_ERROR),
                _webDriverService.GetLocator(AppConstants.CTU_SIGN_IN_FAIL)
            };
            
            // kiểm tra element có display none không
            var tasks = iLocators.Select(locator =>
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
                string userName;
                string MSSV;
                ILocator userInformationElement = _webDriverService.GetLocator(AppConstants.CTU_HOME_USER_INFO);
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
            string currentUrl = _webDriverService.PageUrl;
            try
            {
                // first Navigate to DKMH page
                if (currentUrl.Contains(AppConstants.CTU_HOME_URL))
                {
                    ILocator navigateElement = _webDriverService.GetLocator(AppConstants.CTU_HOME_DKMH_BUTTON);
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
                    ILocator DKMH_RegistrationRulenavigateElement = _webDriverService.GetLocator(AppConstants.CTU_DKMH_QUYDINHDANGKY_BUTTON);
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
                if (!_webDriverService.PageUrl.Contains(AppConstants.CTU_DKMH_URL_KEY)) 
                    throw new Exception("Not in DKMH page");

                ILocator userInfoTab = _webDriverService.GetLocator(AppConstants.CTU_DKMH_INFO_TAB);
                await userInfoTab.ClickAsync();

                ILocator userInfoButton = _webDriverService.GetLocator(AppConstants.CTU_DKMH_INFO_BUTTON);
                await userInfoButton.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible });
                await userInfoButton.ClickAsync();

                await _webDriverService.GetLocator(".ant-modal-content").WaitForAsync(new() { State = WaitForSelectorState.Visible });
                await _webDriverService.GetLocator(".ant-modal-mask").WaitForAsync(new() { State = WaitForSelectorState.Visible });

                ILocator userKeyElement = _webDriverService.GetLocator(AppConstants.CTU_DKMH_INFO_KEY);
                ILocator userUnitElement = _webDriverService.GetLocator(AppConstants.CTU_DKMH_INFO_UNIT);

                var result = await Task.WhenAll(userKeyElement.InnerTextAsync(), userUnitElement.InnerTextAsync());
                string userKey = result[0];
                string userUnit = result[1];

                // close dialog
                await _webDriverService.GetLocator(".ant-modal-close")
                    .WaitForAsync(new() { State = WaitForSelectorState.Visible });
                ILocator userInfoCloseButton = _webDriverService.GetLocator(AppConstants.CTU_DKMH_INFO_CLOSE_BUTTON);
                await userInfoCloseButton.ClickAsync();
                
                return (userKey, userUnit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Exception when TryGetUserKeyAndUnit");
                return (string.Empty, string.Empty);
            }
           
        }

        #endregion

        #region DKMH_DanhMucHocPhan

        private bool IsCourseCatalogPage(string? url = null)
        {
            string currentUrl = url ?? _webDriverService.PageUrl;
            return currentUrl.Contains(AppConstants.CTU_DKMH_URL_KEY) && currentUrl.EndsWith(AppConstants.CTU_DKMH_DANHMUCHOCPHAN_URL_KEY);;
        }
        
        public async Task GoToCourseCatalogPage()
        {
            string currentUrl = _webDriverService.PageUrl;
            if (IsCourseCatalogPage(currentUrl)) return;
            try
            {
                // Current url is DKMH page
                if (currentUrl.Contains(AppConstants.CTU_DKMH_URL_KEY))
                {
                    ILocator navigateElement = _webDriverService.GetLocator(AppConstants.CTU_DKMH_DANHMUCHOCPHAN_BUTTON);
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
            if (rawJson is not { } dataElement) return null;

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
                ILocator courseKeyInput = _webDriverService.GetLocator(AppConstants.CTU_DKMH_DANHMUCHOCPHAN_SEARCHBOX);
                // await _webDriverService.FillElementAsync(courseKeyInput, courseStr);
                await courseKeyInput.FillAsync(courseStr);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception when FillCourseKey");
            }
        }

        public async Task SearchCourse(string courseKey)
        {
            try
            {
                await FillCourseKey(courseKey);

                ILocator searchButton = _webDriverService.GetLocator(AppConstants.CTU_DKMH_DANHMUCHOCPHAN_SEARCH_BUTTON);
                // await _webDriverService.ClickElementAsync(searchButton);
                await searchButton.ClickAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e,"Exception when Search Course");
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
            catch (Exception e)
            {
                _logger.LogError(e,"Exception when ToCourseCatalogJsonData");
                return null;
            }
        }

        #endregion

        public void Dispose()
        {
            
        }

        
    }
}
