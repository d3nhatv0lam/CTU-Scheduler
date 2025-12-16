namespace CTUScheduler.Core.Interfaces.WebDriver;

public interface ISitePageFactory<in TRootPage> where TRootPage: class
{
    string SiteName { get; }

    T GetPage<T>() where T: TRootPage;
}