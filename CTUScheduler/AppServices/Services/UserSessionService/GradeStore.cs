using CTUScheduler.AppServices.Services.Abstractions;
using CTUScheduler.Core.Models.Academic.Grades;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public sealed class GradeStore : StateStore<GradeTranscript>, IGradeStore
{
    public GradeStore(ILogger<GradeStore> logger) : base(logger)
    {
    }
}
