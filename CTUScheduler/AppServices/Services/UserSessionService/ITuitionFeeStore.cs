using System;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;

namespace CTUScheduler.AppServices.Services.UserSessionService;

public interface ITuitionFeeStore
{
    IObservable<TuitionFeeSummary?> TuitionFeeSummaryChanged { get; }
    TuitionFeeSummary? CurrentTuitionFeeSummary { get; }
    void Update(TuitionFeeSummary tuitionFeeSummary);
    void Clear();
}