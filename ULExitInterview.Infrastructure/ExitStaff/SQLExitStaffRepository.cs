using ULExitInterview.Application.Abstractions.Persistence;
using ULExitInterview.Application.ExitStaff;

namespace ULExitInterview.Infrastructure.ExitStaff;

internal sealed class SQLExitStaffRepository(IPrimarySqlConnectionFactory connectionFactory) : IExitStaffRepository
{
}
