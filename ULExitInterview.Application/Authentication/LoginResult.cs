namespace ULExitInterview.Application.Authentication;

public sealed record LoginResult(
    int ResponseCode,
    string? Token,
    string StaffNo,
    string Email,
    string ContactNo,
    string Designation,
    string Department,
    IReadOnlyList<string> Roles);