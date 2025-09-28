using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Services.WebDriver;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.RegistrationInfor;

public class RegistrationInformationService: IRegistrationInformationService
{
    private readonly ILogger<RegistrationInformationService> _logger;
    public RegistrationInformation CurrentRegistrationInformation { get; private set; }
    public IObservable<RegistrationInformation> RegistrationInformationResponse { get; }

    public RegistrationInformationService(ICTUWebDriverService ctuWebDriver,
        ILogger<RegistrationInformationService> logger)
    {
        _logger = logger;
        RegistrationInformationResponse = ctuWebDriver.RegistrationInformationResponse
            .Do(registrationInformation => CurrentRegistrationInformation = registrationInformation);
    }
    
    public bool IsEqualSemester(string semester, int academicYear)
    {
        
        if (!HasRegistrationInformation())
            return false;
        return CurrentRegistrationInformation.Semester == semester
            && CurrentRegistrationInformation.AcademicYear == academicYear;
    }
    
    public bool HasRegistrationInformation()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return CurrentRegistrationInformation != null;
    }
    
    public (int academicYear, string semester) GetSemester()
    {
        if (!HasRegistrationInformation())
            throw new ArgumentNullException(nameof(CurrentRegistrationInformation));
        return (CurrentRegistrationInformation.AcademicYear, CurrentRegistrationInformation.Semester);
    }
}