using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;

namespace CTUScheduler.AppServices.Services.WebDriver
{
    public interface ICTUWebDriverService
    {
        // sign in 
        Task GoToSignInPageAsync();
        Task<bool> TrySignInAsync(string userName, string password);
        // home 
        Task<(string userName, string userMSSV)> TryGetUserInfomation();
        // dkmh RegistrationInfo
        IObservable<RegistrationInformation> RegistrationInformationResponse { get; }
        Task GoToRegistrationRulesPage();
        // course catalog
        IObservable<ObservableCollection<QuickSelectCourse>> CourseCatalogQuickSelectResponse { get; }
        IObservable<Course> CourseCatalogResponse { get; }
        Task GoToCourseCatalogPage();
        Task FillCourseKey(string courseStr);

        Task SearchCourse(string courseKey);
    }
}
