using System.Data;
using System.Data.Common;
using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.Templates;

namespace ULExitInterview.Infrastructure.Templates;

internal sealed class SQLTemplateRepository(
    IPrimarySqlConnectionFactory primaryConnectionFactory,
    ISecondarySqlConnectionFactory secondaryConnectionFactory) : ITemplateRepository
{
    private const string CostCenterQuery = """
        SELECT DISTINCT DIVISION_CODE, COST_CENTER_CODE, COST_CENTER_NAME
        FROM TBL_DIVISION_DEPARTMENT
        WHERE IS_ACTIVE = 1;
        """;

    public Task<IReadOnlyList<TemplateDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => LoadTemplatesAsync(null, null, cancellationToken);

    public Task<IReadOnlyList<TemplateDto>> SearchAsync(int? templateId, string? templateName, CancellationToken cancellationToken = default)
        => LoadTemplatesAsync(templateId, templateName, cancellationToken);

    private async Task<IReadOnlyList<TemplateDto>> LoadTemplatesAsync(int? templateId, string? templateName, CancellationToken cancellationToken)
    {
        var costCenterNames = await GetCostCenterNamesAsync(cancellationToken);
        var templateBuilders = new Dictionary<int, TemplateBuilder>();

        await using var connection = primaryConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT ID, Name, Description, CreatedDate, CreatedUser, UpdatedDate, UpdateUser, IsActive
            FROM dbo.tblTemplate
            WHERE (@TemplateId IS NULL OR ID = @TemplateId)
              AND (@TemplateName IS NULL OR Name LIKE '%' + @TemplateName + '%')
            ORDER BY ID;

            SELECT q.ID, q.Template_ID, qt.Code AS QuestionTypeCode, q.Question, q.[Order], q.IsRequired
            FROM dbo.tblQuestions q
            INNER JOIN dbo.tblQuestionTypes qt ON qt.ID = q.QuestionTypes_ID
            INNER JOIN dbo.tblTemplate t ON t.ID = q.Template_ID
            WHERE (@TemplateId IS NULL OR t.ID = @TemplateId)
              AND (@TemplateName IS NULL OR t.Name LIKE '%' + @TemplateName + '%')
            ORDER BY q.Template_ID, q.[Order], q.ID;

            SELECT c.ID, c.Questions_ID, c.ChoiceName, c.[Order]
            FROM dbo.tblMultipleQuestionedOrChoices c
            INNER JOIN dbo.tblQuestions q ON q.ID = c.Questions_ID
            INNER JOIN dbo.tblTemplate t ON t.ID = q.Template_ID
            WHERE (@TemplateId IS NULL OR t.ID = @TemplateId)
              AND (@TemplateName IS NULL OR t.Name LIKE '%' + @TemplateName + '%')
            ORDER BY c.Questions_ID, c.[Order], c.ID;

            SELECT a.ID, a.Template_ID, a.Division, a.CostCenter
            FROM dbo.tblAssignedCostCenters a
            INNER JOIN dbo.tblTemplate t ON t.ID = a.Template_ID
            WHERE (@TemplateId IS NULL OR t.ID = @TemplateId)
              AND (@TemplateName IS NULL OR t.Name LIKE '%' + @TemplateName + '%')
            ORDER BY a.Template_ID, a.ID;
            """;

        AddParameter(command, "@TemplateId", templateId);
        AddParameter(command, "@TemplateName", string.IsNullOrWhiteSpace(templateName) ? null : templateName.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt32(reader.GetOrdinal("ID"));
            templateBuilders[id] = new TemplateBuilder(
                id,
                GetString(reader, "Name"),
                reader.GetBoolean(reader.GetOrdinal("IsActive")) ? "Active" : "Inactive",
                GetNullableString(reader, "Description"),
                reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                GetString(reader, "CreatedUser"),
                GetNullableDateTime(reader, "UpdatedDate"),
                GetNullableString(reader, "UpdateUser"));
        }

        await reader.NextResultAsync(cancellationToken);

        var questionBuilders = new Dictionary<int, QuestionBuilder>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var questionId = reader.GetInt32(reader.GetOrdinal("ID"));
            var currentTemplateId = reader.GetInt32(reader.GetOrdinal("Template_ID"));
            if (!templateBuilders.TryGetValue(currentTemplateId, out var template)) continue;

            var question = new QuestionBuilder(
                questionId,
                GetString(reader, "QuestionTypeCode"),
                GetString(reader, "Question"),
                reader.GetBoolean(reader.GetOrdinal("IsRequired")),
                reader.GetInt32(reader.GetOrdinal("Order")));

            template.Questions.Add(question);
            questionBuilders[questionId] = question;
        }

        await reader.NextResultAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var questionId = reader.GetInt32(reader.GetOrdinal("Questions_ID"));
            if (!questionBuilders.TryGetValue(questionId, out var question)) continue;

            question.Choices.Add(new QuestionChoiceDto(
                reader.GetInt32(reader.GetOrdinal("ID")),
                GetString(reader, "ChoiceName"),
                reader.GetInt32(reader.GetOrdinal("Order"))));
        }

        await reader.NextResultAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var currentTemplateId = reader.GetInt32(reader.GetOrdinal("Template_ID"));
            if (!templateBuilders.TryGetValue(currentTemplateId, out var template)) continue;

            var division = GetString(reader, "Division");
            var code = GetString(reader, "CostCenter");
            costCenterNames.TryGetValue(CreateCostCenterKey(division, code), out var costCenterName);
            template.AssignedCostCenters.Add(new AssignedCostCenterDto(code, costCenterName ?? string.Empty, division));
        }

        return templateBuilders.Values.Select(x => x.ToDto()).ToList();
    }

    private async Task<Dictionary<string, string>> GetCostCenterNamesAsync(CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using var connection = secondaryConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = CostCenterQuery;
        command.CommandType = CommandType.Text;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var division = GetString(reader, "DIVISION_CODE");
            var code = GetString(reader, "COST_CENTER_CODE");
            var name = CleanCostCenterName(GetString(reader, "COST_CENTER_NAME"));
            result[CreateCostCenterKey(division, code)] = name;
        }

        return result;
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static string CreateCostCenterKey(string division, string costCenter) => $"{division}|{costCenter}";

    private static string GetString(IDataRecord record, string columnName)
    {
        var ordinal = record.GetOrdinal(columnName);
        return record.IsDBNull(ordinal) ? string.Empty : Convert.ToString(record.GetValue(ordinal))?.Trim() ?? string.Empty;
    }

    private static string? GetNullableString(IDataRecord record, string columnName)
    {
        var ordinal = record.GetOrdinal(columnName);
        return record.IsDBNull(ordinal) ? null : Convert.ToString(record.GetValue(ordinal))?.Trim();
    }

    private static DateTime? GetNullableDateTime(IDataRecord record, string columnName)
    {
        var ordinal = record.GetOrdinal(columnName);
        return record.IsDBNull(ordinal) ? null : record.GetDateTime(ordinal);
    }

    private static string CleanCostCenterName(string costCenterName)
    {
        const string prefix = "MANAGER ";
        return costCenterName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? costCenterName[prefix.Length..].Trim() : costCenterName;
    }

    private sealed class TemplateBuilder(int templateId, string name, string status, string? description, DateTime createdDate, string createdUser, DateTime? updatedDate, string? updateUser)
    {
        public int TemplateId { get; } = templateId;
        public string Name { get; } = name;
        public string Status { get; } = status;
        public string? Description { get; } = description;
        public DateTime CreatedDate { get; } = createdDate;
        public string CreatedUser { get; } = createdUser;
        public DateTime? UpdatedDate { get; } = updatedDate;
        public string? UpdateUser { get; } = updateUser;
        public List<QuestionBuilder> Questions { get; } = [];
        public List<AssignedCostCenterDto> AssignedCostCenters { get; } = [];

        public TemplateDto ToDto() => new(TemplateId, Name, Status, Description, CreatedDate, CreatedUser, UpdatedDate, UpdateUser, Questions.Select(x => x.ToDto()).ToList(), AssignedCostCenters);
    }

    private sealed class QuestionBuilder(int questionId, string questionTypeCode, string questionText, bool isRequired, int sortOrder)
    {
        public int QuestionId { get; } = questionId;
        public string QuestionTypeCode { get; } = questionTypeCode;
        public string QuestionText { get; } = questionText;
        public bool IsRequired { get; } = isRequired;
        public int SortOrder { get; } = sortOrder;
        public List<QuestionChoiceDto> Choices { get; } = [];

        public TemplateQuestionDto ToDto() => new(QuestionId, QuestionTypeCode, QuestionText, IsRequired, SortOrder, Choices);
    }
}
