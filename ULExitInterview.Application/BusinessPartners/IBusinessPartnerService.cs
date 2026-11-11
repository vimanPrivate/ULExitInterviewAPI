namespace ULExitInterview.Application.BusinessPartners;

public interface IBusinessPartnerService
{
    Task<int> AddBusinessPartnerAsync(string staffId, string createdUser, CancellationToken cancellationToken = default);
}
