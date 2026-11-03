namespace ULExitInterview.Application.ExitStaff;

public sealed record AssignedTemplateResponseDto(
    string StaffID,
    string Name,
    string DivisionCode,
    string DivisionName,
    string CostCenterCode,
    string CostCenterName,
    string Mobile,
    string Designation,
    DateTime? JoinedDate,
    string Grade,
    DateTime? LastEffectiveDate,
    string Experiance,
    int FlowId,
    int FlowStatusId,
    string FlowStatus,
    AssignedTemplateDto Template);

public sealed record AssignedTemplateDto(
    int TemplateId,
    string TemplateName,
    int FlowId,
    int FlowStatusId,
    string FlowStatus,
    IReadOnlyList<AssignedQuestionAnswerDto> QandA);

public sealed record AssignedQuestionAnswerDto(
    int QuestionId,
    string QuestionText,
    int Order,
    string QuestionTypeCode,
    string? TypingAnswer,
    IReadOnlyList<AssignedChoiceDto> Choices);

public sealed record AssignedChoiceDto(
    int ChoiceId,
    string ChoiceText,
    int Order,
    bool IsSelected);
