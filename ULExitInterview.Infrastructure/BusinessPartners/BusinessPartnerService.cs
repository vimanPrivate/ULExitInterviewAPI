using System.Data;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.BusinessPartners;

namespace ULExitInterview.Infrastructure.BusinessPartners;

internal sealed class BusinessPartnerService(IPrimarySqlConnectionFactory connectionFactory) : IBusinessPartnerService
{
    public async Task<IReadOnlyList<AssignedBusinessPartnerDto>> GetAssignedBusinessPartnersAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT bp.ID AS BpId, bp.MasterStaffId, bp.ActingStaffID,
                   cc.Division, cc.CostCenter
            FROM dbo.tblBusinessPartners AS bp
            LEFT JOIN dbo.tblBpAssignedCostCenters AS cc
                ON cc.tblBusinessPartners_ID = bp.ID
               AND cc.IsActive = 1
            WHERE bp.IsMasterActive = 1
            ORDER BY bp.ID, cc.Division, cc.CostCenter;
            """;

        var partners = new List<AssignedBusinessPartnerDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        int? currentId = null;
        List<BpCostCenterItem>? currentCostCenters = null;
        while (await reader.ReadAsync(cancellationToken))
        {
            var bpId = Convert.ToInt32(reader["BpId"]);
            if (currentId != bpId)
            {
                currentId = bpId;
                currentCostCenters = new List<BpCostCenterItem>();
                partners.Add(new AssignedBusinessPartnerDto(
                    bpId,
                    Convert.ToString(reader["MasterStaffId"]) ?? string.Empty,
                    reader["ActingStaffID"] == DBNull.Value ? null : Convert.ToString(reader["ActingStaffID"]),
                    currentCostCenters));
            }
            if (reader["Division"] != DBNull.Value && reader["CostCenter"] != DBNull.Value)
                currentCostCenters!.Add(new BpCostCenterItem(
                    Convert.ToString(reader["Division"]) ?? string.Empty,
                    Convert.ToString(reader["CostCenter"]) ?? string.Empty));
        }
        return partners;
    }

    public async Task<int> RemoveBusinessPartnerAsync(string masterStaffId, string createdUser, IReadOnlyList<BpCostCenterItem> costCenters, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var uniqueCostCenters = costCenters
                .Select(x => new { Division = x.Division.Trim(), CostCenter = x.CostCenter.Trim() })
                .DistinctBy(x => (x.Division.ToUpperInvariant(), x.CostCenter.ToUpperInvariant()))
                .ToList();

            var predicates = new List<string>();
            await using var deactivate = connection.CreateCommand();
            deactivate.Transaction = transaction;
            AddParameter(deactivate, "@MasterStaffId", DbType.AnsiString, 20, masterStaffId);
            for (var i = 0; i < uniqueCostCenters.Count; i++)
            {
                predicates.Add($"(cc.Division = @Division{i} AND cc.CostCenter = @CostCenter{i})");
                AddParameter(deactivate, $"@Division{i}", DbType.String, 200, uniqueCostCenters[i].Division);
                AddParameter(deactivate, $"@CostCenter{i}", DbType.String, 200, uniqueCostCenters[i].CostCenter);
            }

            deactivate.CommandText = $"""
                UPDATE cc
                SET IsActive = 0
                FROM dbo.tblBpAssignedCostCenters AS cc
                INNER JOIN dbo.tblBusinessPartners AS bp
                    ON bp.ID = cc.tblBusinessPartners_ID
                WHERE bp.MasterStaffId = @MasterStaffId
                  AND bp.IsMasterActive = 1
                  AND cc.IsActive = 1
                  AND ({string.Join(" OR ", predicates)});
                """;
            var affectedRows = await deactivate.ExecuteNonQueryAsync(cancellationToken);

            if (affectedRows > 0)
            {
                await using var updatePartner = connection.CreateCommand();
                updatePartner.Transaction = transaction;
                updatePartner.CommandText = """
                    UPDATE bp
                    SET IsMasterActive = 0,
                        UpdatedUser = @CreatedUser,
                        UpdatedDate = GETDATE()
                    FROM dbo.tblBusinessPartners AS bp
                    WHERE bp.MasterStaffId = @MasterStaffId
                      AND bp.IsMasterActive = 1
                      AND NOT EXISTS (
                          SELECT 1
                          FROM dbo.tblBpAssignedCostCenters AS cc
                          WHERE cc.tblBusinessPartners_ID = bp.ID
                            AND cc.IsActive = 1
                      );
                    """;
                AddParameter(updatePartner, "@MasterStaffId", DbType.AnsiString, 20, masterStaffId);
                AddParameter(updatePartner, "@CreatedUser", DbType.AnsiString, 20, createdUser);
                await updatePartner.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return affectedRows;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> RemoveActingStaffAsync(string masterStaffId, string updatedUser, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.tblBusinessPartners
            SET ActingStaffID = NULL,
                UpdatedDate = GETDATE(),
                UpdatedUser = @UpdatedUser
            WHERE MasterStaffId = @MasterStaffId
              AND IsMasterActive = 1
              AND ActingStaffID IS NOT NULL;
            """;
        AddParameter(command, "@MasterStaffId", DbType.AnsiString, 20, masterStaffId);
        AddParameter(command, "@UpdatedUser", DbType.AnsiString, 20, updatedUser);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<int> AssignActingStaffAsync(string masterStaffId, string actingStaffId, string createdUser, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.tblBusinessPartners
            SET ActingStaffID = @ActingStaff,
                UpdatedUser = @CreatedUser,
                UpdatedDate = GETDATE()
            WHERE MasterStaffId = @MasterStaffId
              AND IsMasterActive = 1;
            """;
        AddParameter(command, "@MasterStaffId", DbType.AnsiString, 20, masterStaffId);
        AddParameter(command, "@ActingStaff", DbType.AnsiString, 20, actingStaffId);
        AddParameter(command, "@CreatedUser", DbType.AnsiString, 20, createdUser);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> AddBusinessPartnerWithCostCentersAsync(string staffId, IReadOnlyList<BpCostCenterItem> costCenters, string createdUser, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            int bpId;
            await using (var create = connection.CreateCommand())
            {
                create.Transaction = transaction;
                create.CommandText = """
                    INSERT INTO dbo.tblBusinessPartners
                        (MasterStaffId, IsMasterActive, ActingStaffID, CreatedDate, CreatedUser)
                    OUTPUT INSERTED.ID
                    VALUES (@StaffId, 1, NULL, GETDATE(), @CreatedUser);
                    """;
                AddParameter(create, "@StaffId", DbType.AnsiString, 20, staffId);
                AddParameter(create, "@CreatedUser", DbType.AnsiString, 20, createdUser);
                bpId = Convert.ToInt32(await create.ExecuteScalarAsync(cancellationToken));
            }

            foreach (var item in costCenters
                         .Select(x => new { Division = x.Division.Trim(), CostCenter = x.CostCenter.Trim() })
                         .DistinctBy(x => (x.Division.ToUpperInvariant(), x.CostCenter.ToUpperInvariant())))
            {
                await using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO dbo.tblBpAssignedCostCenters
                        (tblBusinessPartners_ID, Division, CostCenter, IsActive)
                    VALUES (@BpId, @Division, @CostCenter, 1);
                    """;
                AddParameter(insert, "@BpId", DbType.Int32, 0, bpId);
                AddParameter(insert, "@Division", DbType.String, 200, item.Division);
                AddParameter(insert, "@CostCenter", DbType.String, 200, item.CostCenter);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return bpId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, DbType type, int size, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        if (size > 0) parameter.Size = size;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

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
