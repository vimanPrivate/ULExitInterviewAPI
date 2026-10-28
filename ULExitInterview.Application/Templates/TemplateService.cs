namespace ULExitInterview.Application.Templates;

public sealed class TemplateService(ITemplateRepository templateRepository) : ITemplateService
{
    public Task<IReadOnlyList<TemplateDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => templateRepository.GetAllAsync(cancellationToken);
}
