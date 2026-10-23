using System.Data.Common;
using Oracle.ManagedDataAccess.Client;
using ULExitInterview.Application.Abstractions.Persistence;
namespace ULExitInterview.Infrastructure.Persistence;
internal sealed class OracleConnectionFactory(string connectionString) : IOracleConnectionFactory { public DbConnection CreateConnection() => new OracleConnection(connectionString); }