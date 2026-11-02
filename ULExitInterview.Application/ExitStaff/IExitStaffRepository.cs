namespace ULExitInterview.Application.ExitStaff;

public interface IExitStaffRepository
{
    Task InsertAnswersAsync(InsertAnswersRequest request, CancellationToken cancellationToken = default);
    Task<int> CreateExitStaffFormAsync(ExitStaffFlowInfoDto staffInfo, CancellationToken cancellationToken = default);
    Task MarkEmailSentAsync(int flowId, CancellationToken cancellationToken = default);
    Task<AssignedTemplateDto?> GetAssignedTemplateAsync(string staffId, CancellationToken cancellationToken = default);
}
