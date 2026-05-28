using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic;
using CTUScheduler.Core.Models.Shared.Results;

namespace CTUScheduler.AppServices.Abstractions;

public interface ISchoolAnnouncementService
{
    Task<OperationResult<IReadOnlyList<SchoolAnnouncement>>> FetchAnnouncementsAsync(
        CancellationToken cancellationToken = default);
}