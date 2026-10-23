namespace ULExitInterview.Application.Divisions;

public sealed class DivisionService(IDivisionRepository divisionRepository) : IDivisionService
{
    public Task<IReadOnlyList<DivisionDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => divisionRepository.GetAllAsync(cancellationToken);
}