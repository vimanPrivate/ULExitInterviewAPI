using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ULExitInterview.Application.Abstractions.Persistence;

namespace ULExitInterview.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/master")]
public sealed class MasterController(IPrimarySqlConnectionFactory connectionFactory) : ControllerBase
{
    [HttpGet("getStatus")]
    [ProducesResponseType(typeof(IReadOnlyList<StatusItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StatusItemDto>>> GetStatus(CancellationToken cancellationToken)
    {
        var statuses = new List<StatusItemDto>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "SELECT [ID], [Status] FROM dbo.tblStatus ORDER BY [ID];";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            statuses.Add(new StatusItemDto(
                Convert.ToInt32(reader["ID"]),
                reader["Status"] == DBNull.Value ? string.Empty : Convert.ToString(reader["Status"]) ?? string.Empty));
        }
        return Ok(statuses);
    }
}

public sealed record StatusItemDto(int Id, string Name);
