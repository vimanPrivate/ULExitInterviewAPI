using System.Data.Common;

namespace ULExitInterview.Application.Abstractions.Persistence;

public interface ISqlConnectionFactory
{
    DbConnection CreateConnection();
}

public interface IOracleConnectionFactory
{
    DbConnection CreateConnection();
}