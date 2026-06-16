using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.UseCases.Startup;

namespace ElenaSmallBusinessLedger.App.Services;

public interface IStartupDashboardService
{
    Task<OperationResult<StartupDashboardResult>> LoadAsync(
        DateOnly month,
        string? selectedStudentShortName,
        CancellationToken cancellationToken);
}
