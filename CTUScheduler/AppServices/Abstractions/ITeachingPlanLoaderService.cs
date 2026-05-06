using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Core.Models.TeachingPlan;

namespace CTUScheduler.AppServices.Abstractions;

public interface ITeachingPlanLoaderService
{
    Task<OperationResult<TeachingPlanData>> LoadLatestAsync();
}