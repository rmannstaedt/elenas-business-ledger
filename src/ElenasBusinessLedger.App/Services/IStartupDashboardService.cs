using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.UseCases.Startup;

namespace ElenasBusinessLedger.App.Services;

// Supplies the dashboard's refreshed month and student data through a replaceable service.
public interface IStartupDashboardService
{
    Task<OperationResult<StartupDashboardResult>> LoadAsync(
        DateOnly month,
        string? selectedStudentShortName,
        CancellationToken cancellationToken,
        int? studentId = null);
}
