namespace CTUScheduler.Core.Models.UserSaves;

public record RegistrationContext
{
    public int AcademicYear { get; init; }
    public string Semester { get; init; } = string.Empty;
    public int MaxCreditPerSemester { get; init; }
    
    public string GetContextId() => $"{Semester}_{AcademicYear}";
    public override string ToString() => $"HK {Semester}/{AcademicYear} (max: {MaxCreditPerSemester})";
}