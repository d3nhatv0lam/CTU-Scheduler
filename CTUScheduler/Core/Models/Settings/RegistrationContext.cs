namespace CTUScheduler.Core.Models.Settings;

public record RegistrationContext
{
    public int AcademicYear { get; init; }
    public string Semester { get; init; } = string.Empty;
    public int MaxCreditPerSemester { get; init; }
    
    public static readonly RegistrationContext Unknown = new() 
    { 
        AcademicYear = 0, 
        Semester = "Unknown",
        MaxCreditPerSemester = 0 
    };
    
    public bool IsUnknown() => this == Unknown;
    
    public string GetContextId() => $"{Semester}_{AcademicYear}";
    public override string ToString() => $"HK {Semester}/{AcademicYear} (max: {MaxCreditPerSemester})";
}