namespace CTUScheduler.Infrastructure.Sites.Base;

public interface ISitePageFactory<in TRootPage> where TRootPage: ISitePage
{
    string SiteName { get; }

    T GetPage<T>() where T: TRootPage;
}