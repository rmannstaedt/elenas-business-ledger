using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.UseCases.Startup;

namespace ElenasBusinessLedger.App.Services;

public interface IStartupDashboardService
{
    Task<OperationResult<StartupDashboardResult>> LoadAsync(
        DateOnly month,
        string? selectedStudentShortName,
        CancellationToken cancellationToken);
}
