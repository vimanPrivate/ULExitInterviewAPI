namespace ULExitInterview.Application.BusinessPartners;

public interface IBusinessPartnerService
{
    Task<bool> SetActingStaffAsync(string masterStaffId, string actingStaffId, string updatedUser, CancellationToken cancellationToken = default);
    Task<int> AddBusinessPartnerAsync(string staffId, string createdUser, CancellationToken cancellationToken = default);
}
