namespace CTUScheduler.AppServices.Services.WebDriver.Models;

public record LoginResult(bool IsSuccess, string? ErrorMessage = null)
{
    public bool IsFailed => !IsSuccess;
    
    public static LoginResult Success() => new(true);
    public static LoginResult Failed(string errorMessage) => new(false, errorMessage);
}