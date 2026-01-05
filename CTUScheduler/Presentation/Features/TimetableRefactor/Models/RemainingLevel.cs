namespace CTUScheduler.Presentation.Features.TimetableRefactor.Models;


public enum RemainingLevel
{
    Archived, // not active
    None, // 0%
    Low, // Dưới 10%
    Medium, // 10–40%
    High // Trên 40%
}