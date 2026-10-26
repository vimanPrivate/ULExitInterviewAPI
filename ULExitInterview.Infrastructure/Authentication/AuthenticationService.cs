using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ULExitInterview.Application.Authentication;

namespace ULExitInterview.Infrastructure.Authentication;

internal sealed class AuthenticationService(
    HttpClient httpClient,
    IOptions<AdAuthenticationOptions> options,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    private readonly AdAuthenticationOptions _options = options.Value;

    public async Task<LoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var username = request.Username.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return InvalidCredentials();
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
                return InvalidCredentials();
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "AD authentication service returned HTTP status {StatusCode}.",
                    (int)response.StatusCode);

                return new LoginResult(false, null, null, "Authentication service is currently unavailable.");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            // The upstream API's success response contract is preserved for the next mapping step.
            // A successful HTTP response confirms the credentials were accepted by the configured service.
            return new LoginResult(true, responseBody, null, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("AD authentication service request timed out.");
            return new LoginResult(false, null, null, "Authentication service timed out.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Unable to reach the AD authentication service.");
            return new LoginResult(false, null, null, "Authentication service is currently unavailable.");
        }
    }

    private static LoginResult InvalidCredentials()
        => new(false, null, null, "Invalid username or password.");

    private sealed record AdLoginRequest(
        [property: JsonPropertyName("USERNAME")] string Username,
        [property: JsonPropertyName("PASSWORD")] string Password,
        [property: JsonPropertyName("APPSECAPPID")] string AppSecAppId);
}