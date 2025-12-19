using CTUScheduler.Core.Interfaces.WebDriver.Sites;

namespace CTUScheduler.Core.Interfaces.WebDriver;

public interface ISitePageFactory<in TRootPage> where TRootPage: ISitePage
{
    string SiteName { get; }

    T GetPage<T>() where T: TRootPage;
}