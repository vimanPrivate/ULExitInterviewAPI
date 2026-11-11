using System.Data;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.BusinessPartners;

namespace ULExitInterview.Infrastructure.BusinessPartners;

internal sealed class BusinessPartnerService(IPrimarySqlConnectionFactory connectionFactory) : IBusinessPartnerService
{
    public async Task<bool> SetActingStaffAsync(string masterStaffId, string actingStaffId, string updatedUser, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.tblBusinessPartners
            SET ActingStaffID = @ActingStaffId,
                UpdatedDate = GETDATE(),
                UpdatedUser = @UpdatedUser
            WHERE ID = (
                SELECT TOP (1) ID
                FROM dbo.tblBusinessPartners
                WHERE MasterStaffId = @MasterStaffId AND IsMasterActive = 1
                ORDER BY ID DESC
            );
            """;
        static void AddTextParameter(System.Data.Common.DbCommand command, string name, string value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.DbType = DbType.AnsiString;
            parameter.Size = 20;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }
        AddTextParameter(command, "@MasterStaffId", masterStaffId);
        AddTextParameter(command, "@ActingStaffId", actingStaffId);
        AddTextParameter(command, "@UpdatedUser", updatedUser);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

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
