using System.Data;
using System.Data.Common;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.ExitStaff;

namespace ULExitInterview.Infrastructure.ExitStaff;

internal sealed class OracleExitStaffRepository(IOracleConnectionFactory connectionFactory) : IStaffInfoRepository
{
    private const string Query = "SELECT EMP_NUMBER, FIRST_NAME, LAST_NAME, DIVISION_CODE, DIVISION, COST_CENTER_CODE, COST_CENTER_NAME, MOBILE, DESIGNATION, INITIAL_JOINING_DATE FROM VW_HS_HR_EMPLOYEE_CHARIKA_ALL WHERE EMP_NUMBER = :StaffId AND ROWNUM = 1";
    private const string ExitFlowQuery = "SELECT EMP_NUMBER, DIVISION_CODE, COST_CENTER_CODE, STATUS_EFFECTIVE_DATE, EMAIL FROM VW_HS_HR_EMPLOYEE_CHARIKA_ALL WHERE EMP_NUMBER = :StaffId AND ROWNUM = 1";

    public async Task<StaffInfoDto?> GetStaffInfoAsync(string staffId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand(); command.CommandText = Query; command.CommandType = CommandType.Text;
        AddParameter(command, "StaffId", DbType.String, staffId.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken); if (!await reader.ReadAsync(cancellationToken)) return null;
        var joinedDate = GetNullableDateTime(reader, "INITIAL_JOINING_DATE");
        return new StaffInfoDto(GetString(reader, "EMP_NUMBER"), BuildName(GetString(reader, "FIRST_NAME"), GetString(reader, "LAST_NAME")), GetString(reader, "DIVISION_CODE"), GetString(reader, "DIVISION"), GetString(reader, "COST_CENTER_CODE"), RemoveManagerPrefix(GetString(reader, "COST_CENTER_NAME")), GetString(reader, "MOBILE"), GetString(reader, "DESIGNATION"), joinedDate, CalculateExperience(joinedDate));
    }

    public async Task<ExitStaffFlowInfoDto?> GetExitStaffFlowInfoAsync(string staffId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand(); command.CommandText = ExitFlowQuery; command.CommandType = CommandType.Text;
        AddParameter(command, "StaffId", DbType.String, staffId.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken); if (!await reader.ReadAsync(cancellationToken)) return null;
        return new ExitStaffFlowInfoDto(GetString(reader, "EMP_NUMBER"), GetString(reader, "DIVISION_CODE"), GetString(reader, "COST_CENTER_CODE"), GetNullableDateTime(reader, "STATUS_EFFECTIVE_DATE"), GetString(reader, "EMAIL"));
    }

    public async Task<IReadOnlyList<ExitInterviewStaffSearchDto>> SearchExitInterviewStaffAsync(GetExitInterviewInfoRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection(); await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand(); command.CommandType = CommandType.Text;
        var conditions = new List<string>();
        AddContainsFilter(command, conditions, "CALLING_NAME", "StaffName", request.StaffName);
        AddContainsFilter(command, conditions, "EMP_NUMBER", "StaffNo", request.StaffNo);
        AddContainsFilter(command, conditions, "COST_CENTER_NAME", "CostCenterName", request.CostCenterName);
        AddEqualsFilter(command, conditions, "COST_CENTER_CODE", "CostCenterCode", request.CostCenterCode);
        AddContainsFilter(command, conditions, "DIVISION", "DivisionName", request.DivisionName);
        AddEqualsFilter(command, conditions, "DIVISION_CODE", "DivisionCode", request.DivisionCode);
        if (request.LastWorkingDateFrom.HasValue) { conditions.Add("LAST_EFFECTIVE_DATE >= :DateFrom"); AddParameter(command, "DateFrom", DbType.DateTime, request.LastWorkingDateFrom.Value.Date); }
        if (request.LastWorkingDateTo.HasValue) { conditions.Add("LAST_EFFECTIVE_DATE < :DateTo"); AddParameter(command, "DateTo", DbType.DateTime, request.LastWorkingDateTo.Value.Date.AddDays(1)); }
        var where = conditions.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", conditions);
        command.CommandText = $"SELECT EMP_NUMBER, CALLING_NAME, DIVISION_CODE, DIVISION, COST_CENTER_CODE, COST_CENTER_NAME, LAST_EFFECTIVE_DATE FROM VW_HS_HR_EMPLOYEE_CHARIKA_ALL{where}";

        var result = new List<ExitInterviewStaffSearchDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new ExitInterviewStaffSearchDto(GetString(reader, "EMP_NUMBER"), GetString(reader, "CALLING_NAME"), GetString(reader, "DIVISION"), GetString(reader, "DIVISION_CODE"), RemoveManagerPrefix(GetString(reader, "COST_CENTER_NAME")), GetString(reader, "COST_CENTER_CODE"), GetNullableDateTime(reader, "LAST_EFFECTIVE_DATE")));
        return result;
    }

    private static void AddContainsFilter(DbCommand command, List<string> conditions, string column, string parameterName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        conditions.Add($"UPPER({column}) LIKE :{parameterName}");
        AddParameter(command, parameterName, DbType.String, $"%{value.Trim().ToUpperInvariant()}%");
    }

    private static void AddEqualsFilter(DbCommand command, List<string> conditions, string column, string parameterName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        conditions.Add($"UPPER({column}) = :{parameterName}");
        AddParameter(command, parameterName, DbType.String, value.Trim().ToUpperInvariant());
    }

    private static string BuildName(string firstName, string lastName) => string.Join(" ", new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
    private static string RemoveManagerPrefix(string costCenterName) { const string prefix = "MANAGER "; return costCenterName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? costCenterName[prefix.Length..].TrimStart() : costCenterName; }
    private static string CalculateExperience(DateTime? joinedDate)
    {
        if (joinedDate is null) return string.Empty; var start = joinedDate.Value.Date; var end = DateTime.Today; if (start > end) return "0 years 0 days";
        var years = end.Year - start.Year; var anniversary = start.AddYears(years); if (anniversary > end) { years--; anniversary = start.AddYears(years); }
        return $"{years} years {(end - anniversary).Days} days";
    }
    private static string GetString(IDataRecord record, string columnName) { var ordinal = record.GetOrdinal(columnName); return record.IsDBNull(ordinal) ? string.Empty : Convert.ToString(record.GetValue(ordinal))?.Trim() ?? string.Empty; }
    private static DateTime? GetNullableDateTime(IDataRecord record, string columnName) { var ordinal = record.GetOrdinal(columnName); return record.IsDBNull(ordinal) ? null : Convert.ToDateTime(record.GetValue(ordinal)); }
    private static void AddParameter(DbCommand command, string name, DbType type, object value) { var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.DbType = type; parameter.Value = value; command.Parameters.Add(parameter); }
}
