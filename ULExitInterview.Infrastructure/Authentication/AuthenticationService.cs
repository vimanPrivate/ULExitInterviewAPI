using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ULExitInterview.Application.Authentication;

namespace ULExitInterview.Infrastructure.Authentication;

internal sealed class AuthenticationService(
    HttpClient httpClient,
    IOptions<AdAuthenticationOptions> options,
    IJwtTokenService jwtTokenService,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    private readonly AdAuthenticationOptions _options = options.Value;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<LoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var username = request.Username.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return AuthenticationFailed();
        }

        var payload = new AdLoginRequest(username, request.Password, _options.AppSecAppId);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.Url)
        {
            Content = JsonContent.Create(payload)
        };

        httpRequest.Headers.TryAddWithoutValidation("x-api-key", _options.ApiKey);

        try
        {
            using var response = await httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest)
            {
                return AuthenticationFailed();
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "AD authentication service returned HTTP status {StatusCode}.",
                    (int)response.StatusCode);

                return AuthenticationFailed();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var adResults = await JsonSerializer.DeserializeAsync<List<AdLoginResponse>>(
                stream,
                JsonOptions,
                cancellationToken);

            var adUser = adResults?.FirstOrDefault();
            if (adUser is null || adUser.ResponseCode != "1")
            {
                return AuthenticationFailed();
            }

            var roles = adUser.Roles?
                .Where(role => !string.IsNullOrWhiteSpace(role.RoleName))
                .Select(role => role.RoleName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];

            // AD credentials are valid, but AppSec has not authorized this user for the application.
            if (roles.Count == 0)
            {
                return new LoginResult(
                    2,
                    null,
                    adUser.UserId ?? username,
                    adUser.Email ?? string.Empty,
                    adUser.Mobile ?? string.Empty,
                    adUser.Title ?? string.Empty,
                    adUser.Department ?? string.Empty,
                    []);
            }

            var staffNo = adUser.UserId ?? username;
            var email = adUser.Email ?? string.Empty;
            var designation = adUser.Title ?? string.Empty;
            var department = adUser.Department ?? string.Empty;

            var token = jwtTokenService.GenerateToken(
                staffNo,
                email,
                designation,
                department,
                roles);

            return new LoginResult(
                1,
                token,
                staffNo,
                email,
                adUser.Mobile ?? string.Empty,
                designation,
                department,
                roles);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "AD authentication service returned an invalid response payload.");
            return AuthenticationFailed();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("AD authentication service request timed out.");
            return AuthenticationFailed();
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Unable to reach the AD authentication service.");
            return AuthenticationFailed();
        }
    }

    private static LoginResult AuthenticationFailed()
        => new(0, null, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, []);

    private sealed record AdLoginRequest(
        [property: JsonPropertyName("USERNAME")] string Username,
        [property: JsonPropertyName("PASSWORD")] string Password,
        [property: JsonPropertyName("APPSECAPPID")] string AppSecAppId);

    private sealed class AdLoginResponse
    {
        [JsonPropertyName("RESPONSE_CODE")]
        public string? ResponseCode { get; init; }

        [JsonPropertyName("USERID")]
        public string? UserId { get; init; }

        [JsonPropertyName("EMAIL")]
        public string? Email { get; init; }

        [JsonPropertyName("TITLE")]
        public string? Title { get; init; }

        [JsonPropertyName("DEPARTMENT")]
        public string? Department { get; init; }

        [JsonPropertyName("MOBILE")]
        public string? Mobile { get; init; }

        [JsonPropertyName("ROLES")]
        public List<AdRole>? Roles { get; init; }
    }

    private sealed class AdRole
    {
        [JsonPropertyName("ROLE_NAME")]
        public string RoleName { get; init; } = string.Empty;
    }
}