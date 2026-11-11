using System.Data;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.BusinessPartners;

namespace ULExitInterview.Infrastructure.BusinessPartners;

internal sealed class BusinessPartnerService(IPrimarySqlConnectionFactory connectionFactory) : IBusinessPartnerService
{
    public async Task<int> AddBusinessPartnerAsync(string staffId, string createdUser, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.tblBusinessPartners (MasterStaffId, IsMasterActive, ActingStaffID, CreatedDate, CreatedUser)
            OUTPUT INSERTED.ID
            VALUES (@StaffId, 1, NULL, GETDATE(), @CreatedUser);
            """;
        var staffParameter = command.CreateParameter();
        staffParameter.ParameterName = "@StaffId"; staffParameter.DbType = DbType.AnsiString;
        staffParameter.Size = 20; staffParameter.Value = staffId;
        command.Parameters.Add(staffParameter);
        var userParameter = command.CreateParameter();
        userParameter.ParameterName = "@CreatedUser"; userParameter.DbType = DbType.AnsiString;
        userParameter.Size = 20; userParameter.Value = createdUser;
        command.Parameters.Add(userParameter);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }
}
