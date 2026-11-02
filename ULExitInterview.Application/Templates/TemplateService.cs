namespace ULExitInterview.Application.Templates;

public sealed class TemplateService(ITemplateRepository templateRepository) : ITemplateService
{
    public Task<IReadOnlyList<TemplateDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => templateRepository.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<TemplateDto>> SearchAsync(int? templateId, string? templateName, CancellationToken cancellationToken = default)
        => templateRepository.SearchAsync(templateId, templateName, cancellationToken);

    public Task<int> CreateAsync(CreateTemplateRequest request, CancellationToken cancellationToken = default)
        => templateRepository.CreateAsync(request, cancellationToken);

    public async Task<int> EditAsync(EditTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await templateRepository.SearchAsync(request.TemplateId, null, cancellationToken);
        if (existing.Count == 0) throw new InvalidOperationException($"Active template ID '{request.TemplateId}' does not exist.");

        var createRequest = new CreateTemplateRequest
        {
            Name = request.Name,
            Description = request.Description,
            Status = request.Status,
            CreatedUser = request.CreatedUser,
            Questions = request.Questions
        };

        var newTemplateId = await templateRepository.CreateAsync(createRequest, cancellationToken);
        await templateRepository.DeleteTemplatesAsync(new DeleteTemplatesRequest
        {
            CreatedUser = request.CreatedUser,
            TemplateIds = [request.TemplateId]
        }, cancellationToken);

        return newTemplateId;
    }

    public Task AssignCostCentersAsync(AssignCostCentersRequest request, CancellationToken cancellationToken = default)
        => templateRepository.AssignCostCentersAsync(request, cancellationToken);

    public Task UnassignCostCentersAsync(UnassignCostCentersRequest request, CancellationToken cancellationToken = default)
        => templateRepository.UnassignCostCentersAsync(request, cancellationToken);

    public Task DeleteTemplatesAsync(DeleteTemplatesRequest request, CancellationToken cancellationToken = default)
        => templateRepository.DeleteTemplatesAsync(request, cancellationToken);
}
