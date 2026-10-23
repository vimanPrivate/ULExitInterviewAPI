namespace ULExitInterview.Application.Divisions;

public interface IDivisionRepository
{
    Task<IReadOnlyList<DivisionDto>> GetAllAsync(CancellationToken cancellationToken = default);
}