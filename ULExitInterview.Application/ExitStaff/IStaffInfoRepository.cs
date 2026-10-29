namespace ULExitInterview.Application.ExitStaff;

public interface IStaffInfoRepository
{
    Task<StaffInfoDto?> GetStaffInfoAsync(string staffId, CancellationToken cancellationToken = default);
}
