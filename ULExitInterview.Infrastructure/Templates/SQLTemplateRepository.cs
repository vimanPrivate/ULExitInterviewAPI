using System.Data;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.Templates;

namespace ULExitInterview.Infrastructure.Templates;

internal sealed class SQLTemplateRepository(IPrimarySqlConnectionFactory connectionFactory) : ITemplateRepository
{
    private const string Query = """
        SELECT
            ID,
            Name,
            Description
        FROM dbo.tblTemplate
        WHERE IsActive = 1
        ORDER BY ID
        """;

    public async Task<IReadOnlyList<TemplateDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = Query;
        command.CommandType = CommandType.Text;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var templates = new List<TemplateDto>();

        var idOrdinal = reader.GetOrdinal("ID");
        var nameOrdinal = reader.GetOrdinal("Name");
        var descriptionOrdinal = reader.GetOrdinal("Description");

        while (await reader.ReadAsync(cancellationToken))
        {
            templates.Add(new TemplateDto(
                reader.GetInt32(idOrdinal),
                reader.GetString(nameOrdinal).Trim(),
                reader.IsDBNull(descriptionOrdinal) ? null : reader.GetString(descriptionOrdinal).Trim()));
        }

        return templates;
    }
}
