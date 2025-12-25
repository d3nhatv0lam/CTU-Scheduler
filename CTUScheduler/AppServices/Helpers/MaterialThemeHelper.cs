using Avalonia;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;

namespace CTUScheduler.AppServices.Helpers
{
    public static class MaterialThemeHelper
    {
        public static readonly MaterialThemeBase MaterialThemeStyles = Application.Current!.LocateMaterialTheme<MaterialThemeBase>();

        public static void UseMaterialUiDarkTheme()
        {
            MaterialThemeStyles.BaseTheme = BaseThemeMode.Dark;
        }

        public static void UseMaterialUiLightTheme()
        {
            MaterialThemeStyles.BaseTheme = BaseThemeMode.Light;
        }
    }
}
