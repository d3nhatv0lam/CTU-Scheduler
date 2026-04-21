using System;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Shared.Results;
using System.Threading;

namespace CTUScheduler.AppServices.Abstractions;

public interface IMainHomeService
{
    Task<string> GetStudentIdAsync(CancellationToken cancellationToken = default);
    Task<OperationResult> EnsureReadyAsync();
}