using System.Data.Common;
using Microsoft.Data.SqlClient;
using ULExitInterview.Application.Abstractions.Persistence;

namespace ULExitInterview.Infrastructure.Persistence;

internal sealed class PrimarySqlConnectionFactory(string connectionString) : IPrimarySqlConnectionFactory
{
    public DbConnection CreateConnection() => new SqlConnection(connectionString);
}

internal sealed class SecondarySqlConnectionFactory(string connectionString) : ISecondarySqlConnectionFactory
{
    public DbConnection CreateConnection() => new SqlConnection(connectionString);
}
