namespace ULExitInterview.Application.Divisions;

public interface IDivisionService
{
    Task<IReadOnlyList<DivisionDto>> GetAllAsync(CancellationToken cancellationToken = default);
}