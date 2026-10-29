using System.Data;
using System.Data.Common;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.ExitStaff;

namespace ULExitInterview.Infrastructure.ExitStaff;

internal sealed class OracleExitStaffRepository(IOracleConnectionFactory connectionFactory) : IStaffInfoRepository
{
    private const string Query = """
        SELECT
            EMP_NUMBER,
            FIRST_NAME,
            LAST_NAME,
            DIVISION_CODE,
            DIVISION,
            COST_CENTER_CODE,
            COST_CENTER,
            MOBILE,
            DESIGNATION,
            INITIAL_JOINING_DATE
        FROM HSENID.VW_HS_HR_EMPLOYEE_DETL_SLA_NEW
        WHERE EMP_NUMBER = :StaffId
          AND ROWNUM = 1
        """;

    public async Task<StaffInfoDto?> GetStaffInfoAsync(string staffId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = Query;
        command.CommandType = CommandType.Text;
        AddParameter(command, "StaffId", DbType.String, staffId.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var joinedDate = GetNullableDateTime(reader, "INITIAL_JOINING_DATE");

        return new StaffInfoDto(
            GetString(reader, "EMP_NUMBER"),
            BuildName(GetString(reader, "FIRST_NAME"), GetString(reader, "LAST_NAME")),
            GetString(reader, "DIVISION_CODE"),
            GetString(reader, "DIVISION"),
            GetString(reader, "COST_CENTER_CODE"),
            GetString(reader, "COST_CENTER"),
            GetString(reader, "MOBILE"),
            GetString(reader, "DESIGNATION"),
            joinedDate,
            CalculateExperience(joinedDate));
    }

    private static string BuildName(string firstName, string lastName) => string.Join(" ", new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static string CalculateExperience(DateTime? joinedDate)
    {
        if (joinedDate is null) return string.Empty;

        var start = joinedDate.Value.Date;
        var end = DateTime.Today;
        if (start > end) return "0 years 0 days";

        var years = end.Year - start.Year;
        var anniversary = start.AddYears(years);
        if (anniversary > end)
        {
            years--;
            anniversary = start.AddYears(years);
        }

        var days = (end - anniversary).Days;
        return $"{years} years {days} days";
    }

    private static string GetString(IDataRecord record, string columnName)
    {
        var ordinal = record.GetOrdinal(columnName);
        return record.IsDBNull(ordinal) ? string.Empty : Convert.ToString(record.GetValue(ordinal))?.Trim() ?? string.Empty;
    }

    private static DateTime? GetNullableDateTime(IDataRecord record, string columnName)
    {
        var ordinal = record.GetOrdinal(columnName);
        return record.IsDBNull(ordinal) ? null : Convert.ToDateTime(record.GetValue(ordinal));
    }

    private static void AddParameter(DbCommand command, string name, DbType type, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
