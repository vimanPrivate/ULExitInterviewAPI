namespace ULExitInterview.Application.Authentication;

public interface IJwtTokenService
{
    string GenerateToken(
        string staffNo,
        string email,
        string designation,
        string department,
        IReadOnlyCollection<string> roles);
}