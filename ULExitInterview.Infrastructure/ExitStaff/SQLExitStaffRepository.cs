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

    public async Task MarkEmailSentAsync(int flowId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.tblExitInterviewFlow
            SET IsEmailSent = 1, EmailSentDate = GETDATE(), EmailSentUser = @EmailSentUser
            WHERE ID = @FlowId;
            """;
        AddParameter(command, "@FlowId", DbType.Int32, flowId);
        AddParameter(command, "@EmailSentUser", DbType.String, "ITSystems@srilankan.com");
        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affectedRows != 1) throw new InvalidOperationException($"Exit interview flow '{flowId}' could not be marked as email sent.");
    }

    public async Task<AssignedTemplateDto?> GetAssignedTemplateAsync(string staffId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT f.ID AS FlowId, f.Status_ID AS FlowStatusId, s.Status AS FlowStatus,
                   t.ID AS TemplateId, t.Name AS TemplateName,
                   q.ID AS QuestionId, q.Question AS QuestionText, q.[Order] AS QuestionOrder,
                   qt.Code AS QuestionTypeCode, ta.Answer AS TypingAnswer,
                   c.ID AS ChoiceId, c.ChoiceName AS ChoiceText, c.[Order] AS ChoiceOrder,
                   CASE WHEN ca.ID IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS IsSelected
            FROM dbo.tblExitInterviewFlow f
            INNER JOIN dbo.tblStatus s ON s.ID = f.Status_ID
            INNER JOIN dbo.tblTemplate t ON t.ID = f.Template_ID
            INNER JOIN dbo.tblQuestions q ON q.Template_ID = t.ID
            INNER JOIN dbo.tblQuestionTypes qt ON qt.ID = q.QuestionTypes_ID
            LEFT JOIN dbo.tblAnswers a ON a.ExitInterviewFlow_ID = f.ID AND a.Questions_ID = q.ID
            LEFT JOIN dbo.tblTypingAnswers ta ON ta.Answers_ID = a.ID
            LEFT JOIN dbo.tblMultipleQuestionedOrChoices c ON c.Questions_ID = q.ID
            LEFT JOIN dbo.tblChoicingAnswers ca ON ca.Answers_ID = a.ID AND ca.Answers_MultipleQuestionedOrChoices_ID = c.ID
            WHERE f.ID = (
                SELECT TOP (1) ID FROM dbo.tblExitInterviewFlow
                WHERE ExitingStaff = @StaffId
                ORDER BY ID DESC
            )
            ORDER BY q.[Order], c.[Order];
            """;
        AddParameter(command, "@StaffId", DbType.String, staffId.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        int? templateId = null;
        string templateName = string.Empty;
        int flowStatusId = 0;
        string flowStatus = string.Empty;
        var questions = new Dictionary<int, QuestionBuilder>();
        while (await reader.ReadAsync(cancellationToken))
        {
            templateId ??= Convert.ToInt32(reader["TemplateId"]);
            templateName = Convert.ToString(reader["TemplateName"]) ?? string.Empty;
            flowStatusId = Convert.ToInt32(reader["FlowStatusId"]);
            flowStatus = Convert.ToString(reader["FlowStatus"]) ?? string.Empty;
            var questionId = Convert.ToInt32(reader["QuestionId"]);
            if (!questions.TryGetValue(questionId, out var question))
            {
                question = new QuestionBuilder(
                    questionId,
                    Convert.ToString(reader["QuestionText"]) ?? string.Empty,
                    Convert.ToInt32(reader["QuestionOrder"]),
                    Convert.ToString(reader["QuestionTypeCode"]) ?? string.Empty,
                    reader["TypingAnswer"] == DBNull.Value ? null : Convert.ToString(reader["TypingAnswer"]));
                questions.Add(questionId, question);
            }

            if (reader["ChoiceId"] != DBNull.Value)
            {
                var choiceId = Convert.ToInt32(reader["ChoiceId"]);
                if (question.Choices.All(x => x.ChoiceId != choiceId))
                    question.Choices.Add(new AssignedChoiceDto(
                        choiceId,
                        Convert.ToString(reader["ChoiceText"]) ?? string.Empty,
                        Convert.ToInt32(reader["ChoiceOrder"]),
                        Convert.ToBoolean(reader["IsSelected"])));
            }
        }

        if (templateId is null) return null;
        return new AssignedTemplateDto(
            templateId.Value,
            templateName,
            flowStatusId,
            flowStatus,
            questions.Values.OrderBy(x => x.Order)
                .Select(x => new AssignedQuestionAnswerDto(x.QuestionId, x.QuestionText, x.Order, x.QuestionTypeCode, x.TypingAnswer, x.Choices.OrderBy(c => c.Order).ToList()))
                .ToList());
    }

    private sealed class QuestionBuilder(int questionId, string questionText, int order, string questionTypeCode, string? typingAnswer)
    {
        public int QuestionId { get; } = questionId;
        public string QuestionText { get; } = questionText;
        public int Order { get; } = order;
        public string QuestionTypeCode { get; } = questionTypeCode;
        public string? TypingAnswer { get; } = typingAnswer;
        public List<AssignedChoiceDto> Choices { get; } = [];
    }

    private static async Task<int> GetAssignedTemplateIdAsync(DbConnection connection, DbTransaction transaction, string divisionCode, string costCenterCode, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT TOP (1) a.Template_ID
            FROM dbo.tblAssignedCostCenters a
            INNER JOIN dbo.tblTemplate t ON t.ID = a.Template_ID
            WHERE a.Division = @Division AND a.CostCenter = @CostCenter AND a.IsActive = 1 AND t.IsActive = 1
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
