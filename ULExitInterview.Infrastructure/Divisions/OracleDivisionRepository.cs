using System.Data;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.Divisions;

namespace ULExitInterview.Infrastructure.Divisions;

internal sealed class OracleDivisionRepository(IOracleConnectionFactory connectionFactory) : IDivisionRepository
{
    // Replace the table/view and column names below with the actual Oracle HR schema names.
    // The query deliberately returns a flat result set; grouping is performed in .NET so the API
    // exposes one division with its departments instead of repeating the division for every row.
    private const string Query = """
        SELECT
            d.DIVISION_CODE,
            d.DIVISION_NAME,
            dep.DEPARTMENT_CODE,
            dep.DEPARTMENT_NAME
        FROM DIVISION d
        LEFT JOIN DEPARTMENT dep
            ON dep.DIVISION_CODE = d.DIVISION_CODE
        ORDER BY d.DIVISION_NAME, dep.DEPARTMENT_NAME
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
        var divisionNameOrdinal = reader.GetOrdinal("DIVISION_NAME");
        var departmentCodeOrdinal = reader.GetOrdinal("DEPARTMENT_CODE");
        var departmentNameOrdinal = reader.GetOrdinal("DEPARTMENT_NAME");

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

            var departmentCode = GetString(reader, departmentCodeOrdinal);
            var departmentName = GetString(reader, departmentNameOrdinal);

            if (!string.IsNullOrWhiteSpace(departmentCode) &&
                !division.Departments.Any(x => string.Equals(
                    x.DepartmentCode,
                    departmentCode,
                    StringComparison.OrdinalIgnoreCase)))
            {
                division.Departments.Add(new DepartmentDto(departmentCode, departmentName));
            }
        }

        return divisions.Values
            .Select(x => new DivisionDto(x.DivisionCode, x.DivisionName, x.Departments))
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
        public List<DepartmentDto> Departments { get; } = [];
    }
}