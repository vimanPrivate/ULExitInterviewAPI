namespace ULExitInterview.Application.ExitStaff;

public interface IExitStaffRepository
{
    Task InsertAnswersAsync(InsertAnswersRequest request, CancellationToken cancellationToken = default);
    Task InsertAnswersOnBehalfOfStaffAsync(InsertAnswersRequest request, ExitInterviewAttachment? attachment, CancellationToken cancellationToken = default);
    Task<int> CreateExitStaffFormAsync(ExitStaffFlowInfoDto staffInfo, CancellationToken cancellationToken = default);
    Task MarkEmailSentAsync(int flowId, CancellationToken cancellationToken = default);
    Task SetBeginStatusAsync(int flowId, CancellationToken cancellationToken = default);
    Task<bool> SetFinalRemarksAsync(int flowId, string remarks, CancellationToken cancellationToken = default);
    Task<AssignedTemplateDto?> GetAssignedTemplateAsync(string staffId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExitInterviewFlowSummaryDto>> GetExitInterviewFlowsAsync(CancellationToken cancellationToken = default);
}
