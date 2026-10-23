using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Infrastructure.Persistence;

namespace ULExitInterview.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var sqlConnectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Connection string 'SqlServer' is not configured.");

        var oracleConnectionString = configuration.GetConnectionString("Oracle")
            ?? throw new InvalidOperationException("Connection string 'Oracle' is not configured.");

        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(sqlConnectionString));
        services.AddSingleton<IOracleConnectionFactory>(_ => new OracleConnectionFactory(oracleConnectionString));

        return services;
    }
}