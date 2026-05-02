using System.Threading.Tasks;
using CTUScheduler.AppServices.Models;

namespace CTUScheduler.AppServices.Abstractions;

public interface ITeachingPlanLoaderService
{
    Task<TeachingPlanLoadResult> LoadLatestAsync();
}