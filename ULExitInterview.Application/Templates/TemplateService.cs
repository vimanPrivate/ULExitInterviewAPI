namespace ULExitInterview.Application.Templates;

public sealed class TemplateService(ITemplateRepository templateRepository) : ITemplateService
{
    public Task<IReadOnlyList<TemplateDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => templateRepository.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<TemplateDto>> SearchAsync(int? templateId, string? templateName, CancellationToken cancellationToken = default)
        => templateRepository.SearchAsync(templateId, templateName, cancellationToken);

    public Task<int> CreateAsync(CreateTemplateRequest request, string createdUser, CancellationToken cancellationToken = default)
        => templateRepository.CreateAsync(request, createdUser, cancellationToken);
}
