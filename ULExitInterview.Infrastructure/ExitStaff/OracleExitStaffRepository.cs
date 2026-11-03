using System.Data;
using System.Data.Common;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.ExitStaff;

namespace ULExitInterview.Infrastructure.ExitStaff;

internal sealed class OracleExitStaffRepository(IOracleConnectionFactory connectionFactory) : IStaffInfoRepository
{
    private const string Query = """
        SELECT EMP_NUMBER, FIRST_NAME, LAST_NAME, DIVISION_CODE, DIVISION, COST_CENTER_CODE, COST_CENTER_NAME, MOBILE, DESIGNATION, INITIAL_JOINING_DATE
        FROM VW_HS_HR_EMPLOYEE_CHARIKA_ALL
        WHERE EMP_NUMBER = :StaffId AND ROWNUM = 1
        """;

    private const string ExitFlowQuery = """
        SELECT EMP_NUMBER, DIVISION_CODE, COST_CENTER_CODE, STATUS_EFFECTIVE_DATE, EMAIL
        FROM VW_HS_HR_EMPLOYEE_CHARIKA_ALL
        WHERE EMP_NUMBER = :StaffId AND ROWNUM = 1
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
        var costCenterName = RemoveManagerPrefix(GetString(reader, "COST_CENTER_NAME"));
        return new StaffInfoDto(GetString(reader, "EMP_NUMBER"), BuildName(GetString(reader, "FIRST_NAME"), GetString(reader, "LAST_NAME")), GetString(reader, "DIVISION_CODE"), GetString(reader, "DIVISION"), GetString(reader, "COST_CENTER_CODE"), costCenterName, GetString(reader, "MOBILE"), GetString(reader, "DESIGNATION"), joinedDate, CalculateExperience(joinedDate));
    }

    public async Task<ExitStaffFlowInfoDto?> GetExitStaffFlowInfoAsync(string staffId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = ExitFlowQuery;
        command.CommandType = CommandType.Text;
        AddParameter(command, "StaffId", DbType.String, staffId.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return new ExitStaffFlowInfoDto(
            GetString(reader, "EMP_NUMBER"),
            GetString(reader, "DIVISION_CODE"),
            GetString(reader, "COST_CENTER_CODE"),
            GetNullableDateTime(reader, "STATUS_EFFECTIVE_DATE"),
            GetString(reader, "EMAIL"));
    }

    public async Task<IReadOnlyList<ExitInterviewStaffSearchDto>> SearchExitInterviewStaffAsync(GetExitInterviewInfoRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT EMP_NUMBER, CALLING_NAME, DIVISION_CODE, DIVISION, COST_CENTER_CODE, COST_CENTER_NAME, LAST_EFFECTIVE_DATE
            FROM VW_HS_HR_EMPLOYEE_CHARIKA_ALL
            WHERE (:StaffName IS NULL OR UPPER(CALLING_NAME) LIKE '%' || UPPER(:StaffName) || '%')
              AND (:StaffNo IS NULL OR UPPER(EMP_NUMBER) LIKE '%' || UPPER(:StaffNo) || '%')
              AND (:CostCenterName IS NULL OR UPPER(COST_CENTER_NAME) LIKE '%' || UPPER(:CostCenterName) || '%')
              AND (:CostCenterCode IS NULL OR UPPER(COST_CENTER_CODE) = UPPER(:CostCenterCode))
              AND (:DivisionName IS NULL OR UPPER(DIVISION) LIKE '%' || UPPER(:DivisionName) || '%')
              AND (:DivisionCode IS NULL OR UPPER(DIVISION_CODE) = UPPER(:DivisionCode))
              AND (:DateFrom IS NULL OR LAST_EFFECTIVE_DATE >= :DateFrom)
              AND (:DateTo IS NULL OR LAST_EFFECTIVE_DATE < :DateTo)
            """;
        AddNullableStringParameter(command, "StaffName", request.StaffName);
        AddNullableStringParameter(command, "StaffNo", request.StaffNo);
        AddNullableStringParameter(command, "CostCenterName", request.CostCenterName);
        AddNullableStringParameter(command, "CostCenterCode", request.CostCenterCode);
        AddNullableStringParameter(command, "DivisionName", request.DivisionName);
        AddNullableStringParameter(command, "DivisionCode", request.DivisionCode);
        AddNullableDateParameter(command, "DateFrom", request.LastWorkingDateFrom?.Date);
        AddNullableDateParameter(command, "DateTo", request.LastWorkingDateTo?.Date.AddDays(1));

        var result = new List<ExitInterviewStaffSearchDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ExitInterviewStaffSearchDto(
                GetString(reader, "EMP_NUMBER"),
                GetString(reader, "CALLING_NAME"),
                GetString(reader, "DIVISION"),
                GetString(reader, "DIVISION_CODE"),
                RemoveManagerPrefix(GetString(reader, "COST_CENTER_NAME")),
                GetString(reader, "COST_CENTER_CODE"),
                GetNullableDateTime(reader, "LAST_EFFECTIVE_DATE")));
        }
        return result;
    }

    private static string BuildName(string firstName, string lastName) => string.Join(" ", new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static string RemoveManagerPrefix(string costCenterName)
    {
        const string prefix = "MANAGER ";
        return costCenterName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? costCenterName[prefix.Length..].TrimStart() : costCenterName;
    }

    private static string CalculateExperience(DateTime? joinedDate)
    {
        if (joinedDate is null) return string.Empty;
        var start = joinedDate.Value.Date;
        var end = DateTime.Today;
        if (start > end) return "0 years 0 days";
        var years = end.Year - start.Year;
        var anniversary = start.AddYears(years);
        if (anniversary > end) { years--; anniversary = start.AddYears(years); }
        return $"{years} years {(end - anniversary).Days} days";
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

    private static void AddNullableStringParameter(DbCommand command, string name, string? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.String;
        parameter.Value = string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        command.Parameters.Add(parameter);
    }

    private static void AddNullableDateParameter(DbCommand command, string name, DateTime? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.DateTime;
        parameter.Value = value.HasValue ? value.Value : DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
