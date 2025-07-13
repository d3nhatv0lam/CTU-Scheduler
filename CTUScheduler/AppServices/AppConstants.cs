using Avalonia.Animation;
using Avalonia.Xaml.Interactions.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices
{
    public static class AppConstants
    {
        public static readonly string PWD = AppDomain.CurrentDomain.BaseDirectory;
        public const string USERCONFG_FILENAME = "UserConfig.bin";
      
        // Login Page
        public const string CTU_SIGN_IN_URL = "https://htql.ctu.edu.vn/";
        public const string CTU_SIGN_IN_USERNAME = "#usernameUserInput";
        public const string CTU_SIGN_IN_PASSWORD = "#password";
        public const string CTU_SIGN_IN_BUTTON = "#sign-in-button";
        public const string CTU_SIGN_IN_USERNAME_ERROR = "#usernameError";
        public const string CTU_SIGN_IN_PASSWORD_ERROR = "#passwordError";
        public const string CTU_SIGN_IN_FAIL = "#error-msg";
        // Home Page
        public const string CTU_HOME_URL_PATTERN = "**/htql/sinhvien/hindex.php";
        public const string CTU_HOME_URL = "https://dkmh.ctu.edu.vn/htql/sinhvien/hindex.php";
        public const string CTU_HOME_USER_INFO = "//*[@id=\"user-login\"]";
        public const string CTU_HOME_DKMH_BUTTON = "//*[@id=\"page-body\"]/div[1]/table/tbody/tr[1]/td[2]/div/table/tbody/tr[1]/td[2]/div/span/img";
        // DKMH Page
        public const string CTU_DKMH_URL_KEY = "dkmhfe.ctu.edu.vn/dangkyhocphan";
        public const string CTU_DKMH_INFO_TAB = "//*[@id=\"root\"]/div/header/div/div[3]/div/div";
        public const string CTU_DKMH_INFO_BUTTON = ":text-is('Thông tin')";
        public const string CTU_DKMH_INFO_KEY = "li:has-text('Khóa học') p:nth-of-type(2)";
        public const string CTU_DKMH_INFO_UNIT = "li:has-text('Đơn vị') p:nth-of-type(2)";
        public const string CTU_DKMH_INFO_CLOSE_BUTTON = ".ant-modal-close";
        // navigate button
        public const string CTU_DKMH_QUYDINHDANGKY_BUTTON = "//*[@id=\"root\"]/div/div/aside/div/ul/li[1]";
        public const string CTU_DKMH_DANHMUCHOCPHAN_BUTTON = "//*[@id=\"root\"]/div/div/aside/div/ul/li[2]";
        // quy dinh dang ky
       
        // danh muc hoc phan
        public const string CTU_DKMH_DANHMUCHOCPHAN_SEARCHBOX = "//p[text()=\"Mã học phần\"]/following-sibling::div//input[@role=\"combobox\"]";
        public const string CTU_DKMH_DANHMUCHOCPHAN_SEARCH_BUTTON = "span[role=\"img\"][aria-label=\"search\"].anticon-search";


        // Contracts
        public const string FACEBOOK_URL = "https://www.facebook.com/profile.php?id=100088452777261";
        public const string YOUTUBE_URL = "https://www.youtube.com/@ucduong9984";
        public const string GITHUB_URL = "https://github.com/d3nhatv0lam";
    }
}
