namespace ULExitInterview.Application.BusinessPartners;

public interface IBusinessPartnerService
{
    Task<IReadOnlyList<AssignedBusinessPartnerDto>> GetAssignedBusinessPartnersAsync(CancellationToken cancellationToken = default);
    Task<bool> RemoveActingStaffAsync(string masterStaffId, string updatedUser, CancellationToken cancellationToken = default);
    Task<int> AssignActingStaffAsync(string masterStaffId, string actingStaffId, string createdUser, CancellationToken cancellationToken = default);
    Task<int> AddBusinessPartnerWithCostCentersAsync(string staffId, IReadOnlyList<BpCostCenterItem> costCenters, string createdUser, CancellationToken cancellationToken = default);
    Task<int> AssignCostCentersAsync(AssignCostCentersRequest request, CancellationToken cancellationToken = default);
    Task<bool> SetActingStaffAsync(string masterStaffId, string actingStaffId, string updatedUser, CancellationToken cancellationToken = default);
    Task<int> AddBusinessPartnerAsync(string staffId, string createdUser, CancellationToken cancellationToken = default);
}
