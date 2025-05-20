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
        public const string CTU_SIGN_IN_URL = "https://htql.ctu.edu.vn/htql/login.php";
        public const string CTU_SIGN_IN_USERNAME = "//*[@id=\"txtDinhDanh\"]";
        public const string CTU_SIGN_IN_PASSWORD = "//*[@id=\"txtMatKhau\"]";
        public const string CTU_SIGN_IN_CAPCHA = "//*[@id=\"txtMaBaoVe\"]";
        public const string CTU_SIGN_IN_CAPCHA_IMAGE = "//*[@id=\"verify_code\"]";
        public const string CTU_SIGN_IN_BUTTON = "//*[@id=\"login-sv\"]/tbody/tr[4]/td/input";
        // Home Page
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
        public const string CTU_DKMH_DANHMUCHOCPHAN_SEARCHBOX = "#rc_select_2";
        public const string CTU_DKMH_DANHMUCHOCPHAN_SEARCH_BUTTON = "//*[@id=\"root\"]/div/div/main/div[3]/div[1]/div[1]/div/div[3]/span";
    }
}
