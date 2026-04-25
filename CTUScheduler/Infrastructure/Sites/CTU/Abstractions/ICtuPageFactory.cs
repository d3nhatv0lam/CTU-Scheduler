using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using CTUScheduler.Infrastructure.Sites.Base;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface ICtuPageFactory
{
    T GetPage<T>(IWebTab tab) where T : class, ISitePage;
}