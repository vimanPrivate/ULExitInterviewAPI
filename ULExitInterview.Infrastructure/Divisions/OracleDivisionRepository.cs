using System.Data;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.Divisions;

namespace ULExitInterview.Infrastructure.Divisions;

internal sealed class OracleDivisionRepository(IOracleConnectionFactory connectionFactory) : IDivisionRepository
{
    private const string Query = """
        SELECT
            DIVISION_CODE,
            DIVISION_NAME
        FROM DIVISION
        ORDER BY DIVISION_NAME
        """;

    public async Task<IReadOnlyList<DivisionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = Query;
        command.CommandType = CommandType.Text;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var divisions = new List<DivisionDto>();

        var codeOrdinal = reader.GetOrdinal("DIVISION_CODE");
        var nameOrdinal = reader.GetOrdinal("DIVISION_NAME");

        while (await reader.ReadAsync(cancellationToken))
        {
            var code = reader.IsDBNull(codeOrdinal) ? string.Empty : reader.GetValue(codeOrdinal).ToString() ?? string.Empty;
            var name = reader.IsDBNull(nameOrdinal) ? string.Empty : reader.GetValue(nameOrdinal).ToString() ?? string.Empty;

            divisions.Add(new DivisionDto(code, name));
        }

        return divisions;
    }
}