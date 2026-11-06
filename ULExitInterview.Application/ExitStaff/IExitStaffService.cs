namespace ULExitInterview.Application.ExitStaff;

public interface IExitStaffService
{
    Task<ExitInterviewAttachment?> GetAttachmentAsync(int flowId, CancellationToken cancellationToken = default);
    Task InsertAnswersAsync(InsertAnswersRequest request, CancellationToken cancellationToken = default);
    Task InsertAnswersOnBehalfOfStaffAsync(InsertAnswersRequest request, ExitInterviewAttachment? attachment, CancellationToken cancellationToken = default);
    Task<StaffInfoDto?> GetStaffInfoAsync(string staffId, CancellationToken cancellationToken = default);
    Task<int> CreateExitStaffFormAsync(string staffId, CancellationToken cancellationToken = default);
    Task SetBeginStatusAsync(int flowId, CancellationToken cancellationToken = default);
    Task<bool> SetFinalRemarksAsync(int flowId, string remarks, CancellationToken cancellationToken = default);
    Task<AssignedTemplateResponseDto?> GetAssignedTemplatesAsync(string staffId, CancellationToken cancellationToken = default);
    Task<ExitInterviewInfoResponseDto> GetExitInterviewInfoAsync(GetExitInterviewInfoRequest request, CancellationToken cancellationToken = default);
}
