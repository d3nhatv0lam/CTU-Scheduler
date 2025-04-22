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
        public const string CTU_LOGIN_URL = "https://htql.ctu.edu.vn/htql/login.php";
        public const string CTU_LOGIN_USERNAME = "//*[@id=\"txtDinhDanh\"]";
        public const string CTU_LOGIN_PASSWORD = "//*[@id=\"txtMatKhau\"]";
        public const string CTU_LOGIN_CAPCHA = "//*[@id=\"txtMaBaoVe\"]";
        public const string CTU_LOGIN_CAPCHA_IMAGE = "//*[@id=\"verify_code\"]";
        public const string CTU_LOGIN_BUTTON = "//*[@id=\"login-sv\"]/tbody/tr[4]/td/input";
    }
}
