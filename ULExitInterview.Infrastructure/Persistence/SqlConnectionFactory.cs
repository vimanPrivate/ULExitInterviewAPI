using System.Data.Common;
using Microsoft.Data.SqlClient;
using ULExitInterview.Application.Abstractions.Persistence;
namespace ULExitInterview.Infrastructure.Persistence;
internal sealed class SqlConnectionFactory(string connectionString) : ISqlConnectionFactory { public DbConnection CreateConnection() => new SqlConnection(connectionString); }