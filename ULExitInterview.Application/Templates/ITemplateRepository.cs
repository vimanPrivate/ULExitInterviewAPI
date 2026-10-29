namespace ULExitInterview.Application.Templates;

public interface ITemplateRepository
{
    Task<IReadOnlyList<TemplateDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TemplateDto>> SearchAsync(int? templateId, string? templateName, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(CreateTemplateRequest request, string createdUser, CancellationToken cancellationToken = default);
}
