using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.Authentication;
using ULExitInterview.Application.Divisions;
using ULExitInterview.Application.Templates;
using ULExitInterview.Infrastructure.Authentication;
using ULExitInterview.Infrastructure.Divisions;
using ULExitInterview.Infrastructure.Persistence;
using ULExitInterview.Infrastructure.Templates;

namespace ULExitInterview.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var primarySqlConnectionString = GetRequiredConnectionString(configuration, "SqlServerPrimary");
        var secondarySqlConnectionString = GetRequiredConnectionString(configuration, "SqlServerSecondary");
        var oracleConnectionString = GetRequiredConnectionString(configuration, "Oracle");

        var adAuthenticationSection = configuration.GetSection(AdAuthenticationOptions.SectionName);
        var adAuthenticationOptions = adAuthenticationSection.Get<AdAuthenticationOptions>()
            ?? throw new InvalidOperationException("AD authentication configuration is missing.");

        if (string.IsNullOrWhiteSpace(adAuthenticationOptions.Url) ||
            !Uri.TryCreate(adAuthenticationOptions.Url, UriKind.Absolute, out var authenticationUri) ||
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

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
            string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
            string.IsNullOrWhiteSpace(jwtOptions.Key) ||
            jwtOptions.Key.Length < 32)
        {
            throw new InvalidOperationException("JWT Issuer, Audience and a signing key of at least 32 characters must be configured.");
        }

        services.Configure<AdAuthenticationOptions>(adAuthenticationSection);
        services.Configure<JwtOptions>(jwtSection);

        services.AddSingleton<IPrimarySqlConnectionFactory>(
            _ => new PrimarySqlConnectionFactory(primarySqlConnectionString));
        services.AddSingleton<ISecondarySqlConnectionFactory>(
            _ => new SecondarySqlConnectionFactory(secondarySqlConnectionString));
        services.AddSingleton<IOracleConnectionFactory>(
            _ => new OracleConnectionFactory(oracleConnectionString));

        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(
                adAuthenticationOptions.TimeoutSeconds is > 0 and <= 120
                    ? adAuthenticationOptions.TimeoutSeconds
                    : 30);
        });

        services.AddScoped<IDivisionRepository, SQLDivisionRepository>();
        services.AddScoped<IDivisionService, DivisionService>();
        services.AddScoped<ITemplateRepository, SQLTemplateRepository>();
        services.AddScoped<ITemplateService, TemplateService>();

        return services;
    }

    private static string GetRequiredConnectionString(
        IConfiguration configuration,
        string name)
    {
        var connectionString = configuration.GetConnectionString(name);

        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException($"Connection string '{name}' is not configured.")
            : connectionString;
    }
}
