using System.Data.Common;

namespace ULExitInterview.Application.Abstractions.Persistence;

public interface IPrimarySqlConnectionFactory
{
    DbConnection CreateConnection();
}

public interface ISecondarySqlConnectionFactory
{
    DbConnection CreateConnection();
}

public interface IOracleConnectionFactory
{
    DbConnection CreateConnection();
}
