using Avalonia;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Helpers
{
    public static class MaterialThemeHelper
    {
        public static readonly MaterialThemeBase MaterialThemeStyles = Application.Current!.LocateMaterialTheme<MaterialThemeBase>();

        public static void UseMaterialUIDarkTheme()
        {
            MaterialThemeStyles.BaseTheme = BaseThemeMode.Dark;
        }

        public static void UseMaterialUILightTheme()
        {
            MaterialThemeStyles.BaseTheme = BaseThemeMode.Light;
        }
    }
}
