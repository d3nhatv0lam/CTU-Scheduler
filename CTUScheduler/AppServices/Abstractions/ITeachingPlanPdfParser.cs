using System;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.TeachingPlan;

namespace CTUScheduler.AppServices.Abstractions;

public interface ITeachingPlanPdfParser
{
    Task<DateTime?> ExtractClosingNoticeDateTimeAsync(string filePath);
    Task<TeachingPlanData> ExtractTeachingPlanAsync(string filePath, DateTime? preciseClosingDateTime = null);
}
