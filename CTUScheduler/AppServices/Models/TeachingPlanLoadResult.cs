using CTUScheduler.Core.Models.TeachingPlan;

namespace CTUScheduler.AppServices.Models;

public class TeachingPlanLoadResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public TeachingPlanData? Data { get; set; }

    public static TeachingPlanLoadResult Success(TeachingPlanData data)
    {
        return new TeachingPlanLoadResult
        {
            IsSuccess = true,
            Data = data
        };
    }

    public static TeachingPlanLoadResult Failed(string errorMessage)
    {
        return new TeachingPlanLoadResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}

