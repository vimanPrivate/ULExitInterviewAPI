using System.Data;
using System.Data.Common;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.ExitStaff;

namespace ULExitInterview.Infrastructure.ExitStaff;

internal sealed class SQLExitStaffRepository(IPrimarySqlConnectionFactory connectionFactory) : IExitStaffRepository
{
    public async Task InsertAnswersAsync(InsertAnswersRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var isDraft = request.Status.Equals("draft", StringComparison.OrdinalIgnoreCase);
            foreach (var answer in request.Answers)
            {
                var answerId = await InsertAnswerAsync(connection, transaction, request.TemplateId, answer.QuestionId, request.CreatedUser.Trim(), isDraft, cancellationToken);
                if (!string.IsNullOrWhiteSpace(answer.Answer)) await InsertTypingAnswerAsync(connection, transaction, answerId, answer.Answer.Trim(), cancellationToken);
                foreach (var choice in answer.Choices.DistinctBy(x => x.Id)) await InsertChoiceAnswerAsync(connection, transaction, answerId, choice.Id, cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<int> CreateExitStaffFormAsync(ExitStaffFlowInfoDto staffInfo, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var templateId = await GetAssignedTemplateIdAsync(connection, transaction, staffInfo.DivisionCode, staffInfo.CostCenterCode, cancellationToken);
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO dbo.tblExitInterviewFlow (ExitingStaff, Template_ID, ExpireDate, Status_ID)
                OUTPUT INSERTED.ID
                VALUES (@ExitingStaff, @TemplateId, @ExpireDate, 1);
                """;
            AddParameter(command, "@ExitingStaff", DbType.String, staffInfo.StaffId);
            AddParameter(command, "@TemplateId", DbType.Int32, templateId);
            AddParameter(command, "@ExpireDate", DbType.DateTime2, staffInfo.ExpireDate);
            var flowId = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            await transaction.CommitAsync(cancellationToken);
            return flowId;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task<int> GetAssignedTemplateIdAsync(DbConnection connection, DbTransaction transaction, string divisionCode, string costCenterCode, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT TOP (1) a.Template_ID
            FROM dbo.tblAssignedCostCenters a
            INNER JOIN dbo.tblTemplate t ON t.ID = a.Template_ID
            WHERE a.Division = @Division
              AND a.CostCenter = @CostCenter
              AND a.IsActive = 1
              AND t.IsActive = 1
            ORDER BY a.ID DESC;
            """;
        AddParameter(command, "@Division", DbType.String, divisionCode);
        AddParameter(command, "@CostCenter", DbType.String, costCenterCode);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null || result == DBNull.Value) throw new InvalidOperationException($"No active template is assigned to division '{divisionCode}' and cost center '{costCenterCode}'.");
        return Convert.ToInt32(result);
    }

    private static async Task<int> InsertAnswerAsync(DbConnection connection, DbTransaction transaction, int templateId, int questionId, string answeredUser, bool isDraft, CancellationToken cancellationToken)
    {
        const string query = "INSERT INTO dbo.tblAnswers (Template_ID, Questions_ID, AnsweredUser, AnsweredDate, IsDraft) OUTPUT INSERTED.ID VALUES (@TemplateId, @QuestionId, @AnsweredUser, GETDATE(), @IsDraft);";
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = query;
        AddParameter(command, "@TemplateId", DbType.Int32, templateId);
        AddParameter(command, "@QuestionId", DbType.Int32, questionId);
        AddParameter(command, "@AnsweredUser", DbType.String, answeredUser);
        AddParameter(command, "@IsDraft", DbType.Boolean, isDraft);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task InsertTypingAnswerAsync(DbConnection connection, DbTransaction transaction, int answerId, string answer, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO dbo.tblTypingAnswers (Answers_ID, Answer) VALUES (@AnswerId, @Answer);";
        AddParameter(command, "@AnswerId", DbType.Int32, answerId);
        AddParameter(command, "@Answer", DbType.String, answer);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertChoiceAnswerAsync(DbConnection connection, DbTransaction transaction, int answerId, int choiceId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO dbo.tblChoicingAnswers (Answers_ID, Answers_MultipleQuestionedOrChoices_ID) VALUES (@AnswerId, @ChoiceId);";
        AddParameter(command, "@AnswerId", DbType.Int32, answerId);
        AddParameter(command, "@ChoiceId", DbType.Int32, choiceId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(DbCommand command, string name, DbType type, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
