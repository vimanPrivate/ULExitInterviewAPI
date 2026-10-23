using ULExitInterview.Application.Authentication;

namespace ULExitInterview.Infrastructure.Authentication;

internal sealed class AuthenticationService : IAuthenticationService
{
    public Task<LoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        // Authentication will be connected to the authoritative user source next.
        // Do not add hard-coded credentials or plaintext passwords here.
        return Task.FromResult(new LoginResult(
            false,
            null,
            null,
            "Invalid username or password."));
    }
}