using System;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface ISchedulePage: IStudentInfoPage
{
    IObservable<RawThongTinHocPhiPayload> HocPhiResponse { get; }
}