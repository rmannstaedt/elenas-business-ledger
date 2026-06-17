using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.UseCases.Startup;

namespace MySmallBusinessLedger.App.Services;

public interface IStartupDashboardService
{
    Task<OperationResult<StartupDashboardResult>> LoadAsync(
        DateOnly month,
        string? selectedStudentShortName,
        CancellationToken cancellationToken);
}
