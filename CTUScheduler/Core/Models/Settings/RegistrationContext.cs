namespace CTUScheduler.Core.Models.Settings;

public record RegistrationContext(
    int AcademicYear,
    string Semester,
    int MaxCreditPerSemester)
{
    public override string ToString() => $"HK {Semester}/{AcademicYear} (max: {MaxCreditPerSemester})";
}