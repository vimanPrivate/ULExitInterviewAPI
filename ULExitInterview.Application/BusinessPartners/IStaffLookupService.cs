namespace ULExitInterview.Application.BusinessPartners;

public interface IStaffLookupService
{
    Task<IReadOnlyList<StaffLookupDto>> GetStaffAsync(string nameOrStaffId, CancellationToken cancellationToken = default);
}
