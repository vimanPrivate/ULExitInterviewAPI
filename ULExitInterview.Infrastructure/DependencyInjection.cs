using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.Authentication;
using ULExitInterview.Application.Divisions;
using ULExitInterview.Infrastructure.Authentication;
using ULExitInterview.Infrastructure.Divisions;
using ULExitInterview.Infrastructure.Persistence;

namespace ULExitInterview.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var sqlConnectionString = configuration.GetConnectionString("SqlServer");
        if (string.IsNullOrWhiteSpace(sqlConnectionString))
        {
            throw new InvalidOperationException("Connection string 'SqlServer' is not configured.");
        }

        var oracleConnectionString = configuration.GetConnectionString("Oracle");
        if (string.IsNullOrWhiteSpace(oracleConnectionString))
        {
            throw new InvalidOperationException("Connection string 'Oracle' is not configured.");
        }

        var adAuthenticationSection = configuration.GetSection(AdAuthenticationOptions.SectionName);
        var adAuthenticationOptions = adAuthenticationSection.Get<AdAuthenticationOptions>()
            ?? throw new InvalidOperationException("AD authentication configuration is missing.");

        if (string.IsNullOrWhiteSpace(adAuthenticationOptions.Url))
        {
            throw new InvalidOperationException("AD authentication URL is not configured.");
        }

        if (!Uri.TryCreate(adAuthenticationOptions.Url, UriKind.Absolute, out var authenticationUri) ||
            authenticationUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("AD authentication URL must be a valid HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(adAuthenticationOptions.ApiKey))
        {
            throw new InvalidOperationException("AD authentication API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(adAuthenticationOptions.AppSecAppId))
        {
            throw new InvalidOperationException("AD authentication APPSECAPPID is not configured.");
        }

        services.Configure<AdAuthenticationOptions>(adAuthenticationSection);

        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(sqlConnectionString));
        services.AddSingleton<IOracleConnectionFactory>(_ => new OracleConnectionFactory(oracleConnectionString));

        services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(
                adAuthenticationOptions.TimeoutSeconds is > 0 and <= 120
                    ? adAuthenticationOptions.TimeoutSeconds
                    : 30);
        });

        services.AddScoped<IDivisionRepository, OracleDivisionRepository>();
        services.AddScoped<IDivisionService, DivisionService>();

        return services;
    }
}