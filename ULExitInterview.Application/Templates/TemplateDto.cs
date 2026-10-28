namespace ULExitInterview.Application.Templates;

public sealed record TemplateDto(
    int TemplateId,
    string Name,
    string Status,
    string? Description,
    DateTime CreatedDate,
    string CreatedUser,
    DateTime? UpdatedDate,
    string? UpdateUser,
    IReadOnlyList<TemplateQuestionDto> Questions,
    IReadOnlyList<AssignedCostCenterDto> AssignedCostCenters);

public sealed record TemplateQuestionDto(
    int QuestionId,
    string QuestionTypeCode,
    string QuestionText,
    bool IsRequired,
    int SortOrder,
    IReadOnlyList<QuestionChoiceDto> Choices);

public sealed record QuestionChoiceDto(
    int ChoiceId,
    string ChoiceText,
    int SortOrder);

public sealed record AssignedCostCenterDto(
    string Code,
    string Name,
    string Division);
