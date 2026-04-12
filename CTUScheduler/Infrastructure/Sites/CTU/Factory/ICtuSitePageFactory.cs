namespace CTUScheduler.Infrastructure.Sites.CTU.Factory;

public interface ICtuSitePageFactory
{
    T GetPage<T>();
}