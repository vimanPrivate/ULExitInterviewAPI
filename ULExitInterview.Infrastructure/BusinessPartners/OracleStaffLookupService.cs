using System.Data;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.BusinessPartners;

namespace ULExitInterview.Infrastructure.BusinessPartners;

internal sealed class OracleStaffLookupService(IOracleConnectionFactory connectionFactory) : IStaffLookupService
{
    public async Task<IReadOnlyList<StaffLookupDto>> GetStaffAsync(string nameOrStaffId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT EMP_NUMBER, CALLING_NAME
            FROM VW_HS_HR_EMPLOYEE_CHARIKA_ALL
            WHERE UPPER(CALLING_NAME) LIKE :SearchName ESCAPE '\\'
               OR UPPER(EMP_NUMBER) LIKE :SearchId ESCAPE '\\'
            ORDER BY CALLING_NAME, EMP_NUMBER
            FETCH FIRST 50 ROWS ONLY
            """;
        var search = nameOrStaffId.Trim().ToUpperInvariant()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        foreach (var parameterName in new[] { "SearchName", "SearchId" })
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.DbType = DbType.String;
            parameter.Value = $"%{search}%";
            command.Parameters.Add(parameter);
        }
        var result = new List<StaffLookupDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new StaffLookupDto(
                reader["EMP_NUMBER"] == DBNull.Value ? "" : Convert.ToString(reader["EMP_NUMBER"]) ?? "",
                reader["CALLING_NAME"] == DBNull.Value ? "" : Convert.ToString(reader["CALLING_NAME"]) ?? ""));
        }
        return result;
    }
}
