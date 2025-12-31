using System;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;

namespace CTUScheduler.Legacy.RegistrationInfor;

public interface IRegistrationInformationService
{
    public static readonly int UnfoundRegistrationInformation = -1;
    RegistrationInformation CurrentRegistrationInformation { get; }
    IObservable<RegistrationInformation> RegistrationInformationResponse { get; }
    bool HasRegistrationInformation();
    bool IsEqualSemester(string semester, int academicYear);
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    (int academicYear, string semester) GetSemester();
}