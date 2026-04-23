using System.Threading.Tasks;

namespace CTUScheduler.Core.Models.Shared.Results;

public static partial class OperationResultExtensions
{
    // ==========================================================
    // CONVERSIONS
    // ==========================================================

    #region OperationResult<T> Conversions

    public static OperationResult ToResult<T>(this OperationResult<T> result)
    {
        return result.IsSuccess
            ? OperationResult.Success()
            : OperationResult.FailureFrom(result);
    }

    #endregion

    #region Task<OperationResult<T>> Conversions

    public static async Task<OperationResult> ToResult<T>(this Task<OperationResult<T>> resultTask)
    {
        var result = await resultTask;
        return result.IsSuccess
            ? OperationResult.Success()
            : OperationResult.FailureFrom(result);
    }

    #endregion
}
