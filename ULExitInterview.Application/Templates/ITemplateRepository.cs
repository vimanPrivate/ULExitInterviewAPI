namespace ULExitInterview.Application.Templates;

public interface ITemplateRepository
{
    Task<IReadOnlyList<TemplateDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
