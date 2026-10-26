namespace ULExitInterview.Infrastructure.Authentication;

internal sealed class AdAuthenticationOptions
{
    public const string SectionName = "AdAuthentication";

    public string Url { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string AppSecAppId { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
}