namespace ULExitInterview.Application.Authentication;

public sealed record LoginResult(
    bool IsSuccess,
    string? AccessToken,
    DateTimeOffset? ExpiresAtUtc,
    string? ErrorMessage);