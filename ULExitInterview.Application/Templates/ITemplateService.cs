namespace ULExitInterview.Application.Templates;

public interface ITemplateService
{
    Task<IReadOnlyList<TemplateDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TemplateDto>> SearchAsync(int? templateId, string? templateName, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(CreateTemplateRequest request, CancellationToken cancellationToken = default);
    Task AssignCostCentersAsync(AssignCostCentersRequest request, CancellationToken cancellationToken = default);
}
