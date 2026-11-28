using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Grades;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public interface IGradeStore : IStateStore<GradeTranscript>
{
}
