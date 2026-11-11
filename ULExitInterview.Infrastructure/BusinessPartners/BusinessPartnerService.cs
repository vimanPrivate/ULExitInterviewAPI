using System.Data;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.BusinessPartners;

namespace ULExitInterview.Infrastructure.BusinessPartners;

internal sealed class BusinessPartnerService(IPrimarySqlConnectionFactory connectionFactory) : IBusinessPartnerService
{
    public async Task<int> AssignCostCentersAsync(AssignCostCentersRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var check = connection.CreateCommand())
            {
                check.Transaction = transaction;
                check.CommandText = "SELECT COUNT(1) FROM dbo.tblBusinessPartners WHERE ID = @BpId AND IsMasterActive = 1;";
                var bp = check.CreateParameter(); bp.ParameterName = "@BpId"; bp.DbType = DbType.Int32; bp.Value = request.BpId; check.Parameters.Add(bp);
                if (Convert.ToInt32(await check.ExecuteScalarAsync(cancellationToken)) == 0)
                    throw new KeyNotFoundException("Active business partner not found.");
            }

            var inserted = 0;
            foreach (var item in request.CostCenters
                         .Select(x => new { Division = x.Division.Trim(), CostCenter = x.CostCenter.Trim() })
                         .DistinctBy(x => (x.Division.ToUpperInvariant(), x.CostCenter.ToUpperInvariant())))
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = """
                    UPDATE dbo.tblBpAssignedCostCenters WITH (UPDLOCK, HOLDLOCK)
                    SET IsActive = 1
                    WHERE tblBusinessPartners_ID = @BpId
                      AND Division = @Division
                      AND CostCenter = @CostCenter
                      AND IsActive = 0;

                    IF @@ROWCOUNT = 0 AND NOT EXISTS (
                        SELECT 1 FROM dbo.tblBpAssignedCostCenters WITH (UPDLOCK, HOLDLOCK)
                        WHERE tblBusinessPartners_ID = @BpId AND Division = @Division AND CostCenter = @CostCenter
                    )
                    BEGIN
                        INSERT INTO dbo.tblBpAssignedCostCenters (tblBusinessPartners_ID, Division, CostCenter, IsActive)
                        VALUES (@BpId, @Division, @CostCenter, 1);
                    END;
                    """;
                var id = command.CreateParameter(); id.ParameterName = "@BpId"; id.DbType = DbType.Int32; id.Value = request.BpId; command.Parameters.Add(id);
                var division = command.CreateParameter(); division.ParameterName = "@Division"; division.DbType = DbType.String; division.Size = 200; division.Value = item.Division; command.Parameters.Add(division);
                var costCenter = command.CreateParameter(); costCenter.ParameterName = "@CostCenter"; costCenter.DbType = DbType.String; costCenter.Size = 200; costCenter.Value = item.CostCenter; command.Parameters.Add(costCenter);
                inserted += await command.ExecuteNonQueryAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            return inserted;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

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
