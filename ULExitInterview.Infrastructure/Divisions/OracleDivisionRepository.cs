using System.Data;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.Divisions;

namespace ULExitInterview.Infrastructure.Divisions;

internal sealed class OracleDivisionRepository(ISecondarySqlConnectionFactory connectionFactory) : IDivisionRepository
{
    private const string Query = """
        SELECT DISTINCT
            DIVISION_CODE,
            DIVISION,
            COST_CENTER_CODE,
            COST_CENTER_NAME
        FROM TBL_DIVISION_DEPARTMENT
        WHERE IS_ACTIVE = 1
        ORDER BY DIVISION_CODE
        """;

    public async Task<IReadOnlyList<DivisionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = Query;
        command.CommandType = CommandType.Text;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var divisions = new Dictionary<string, DivisionBuilder>(StringComparer.OrdinalIgnoreCase);

        var divisionCodeOrdinal = reader.GetOrdinal("DIVISION_CODE");
        var divisionNameOrdinal = reader.GetOrdinal("DIVISION");
        var costCenterCodeOrdinal = reader.GetOrdinal("COST_CENTER_CODE");
        var costCenterNameOrdinal = reader.GetOrdinal("COST_CENTER_NAME");

        while (await reader.ReadAsync(cancellationToken))
        {
            var divisionCode = GetString(reader, divisionCodeOrdinal);
            var divisionName = GetString(reader, divisionNameOrdinal);

            if (string.IsNullOrWhiteSpace(divisionCode))
            {
                continue;
            }

            if (!divisions.TryGetValue(divisionCode, out var division))
            {
                division = new DivisionBuilder(divisionCode, divisionName);
                divisions.Add(divisionCode, division);
            }

            var costCenterCode = GetString(reader, costCenterCodeOrdinal);
            var costCenterName = GetString(reader, costCenterNameOrdinal);

            if (!string.IsNullOrWhiteSpace(costCenterCode) &&
                !division.CostCenters.Any(x => string.Equals(x.CostCenterCode, costCenterCode, StringComparison.OrdinalIgnoreCase)))
            {
                division.CostCenters.Add(new CostCenterDto(costCenterCode, costCenterName));
            }
        }

        return divisions.Values
            .Select(x => new DivisionDto(x.DivisionCode, x.DivisionName, x.CostCenters))
            .ToList();
    }

    private static string GetString(IDataRecord record, int ordinal)
        => record.IsDBNull(ordinal)
            ? string.Empty
            : record.GetValue(ordinal).ToString()?.Trim() ?? string.Empty;

    private sealed class DivisionBuilder(string divisionCode, string divisionName)
    {
        public string DivisionCode { get; } = divisionCode;
        public string DivisionName { get; } = divisionName;
        public List<CostCenterDto> CostCenters { get; } = [];
    }
}
