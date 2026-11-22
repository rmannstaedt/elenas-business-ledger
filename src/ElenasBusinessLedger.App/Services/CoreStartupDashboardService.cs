using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.UseCases.Startup;

namespace ElenasBusinessLedger.App.Services;

// Wraps dashboard reconciliation so WPF receives results it can display when a refresh fails.
public sealed class CoreStartupDashboardService : IStartupDashboardService
{
    private readonly LoadDashboardUseCase loadDashboard;

    public CoreStartupDashboardService(LoadDashboardUseCase loadDashboard)
    {
        this.loadDashboard = loadDashboard;
    }

    // Runs dashboard reconciliation and translates refresh failures into a result the UI can display.
    public async Task<OperationResult<StartupDashboardResult>> LoadAsync(
        DateOnly month,
        string? selectedStudentShortName,
        CancellationToken cancellationToken,
        int? studentId = null)
    {
        try
        {
            return await loadDashboard.ExecuteAsync(month, selectedStudentShortName, cancellationToken, studentId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<StartupDashboardResult>.Failure(
                new ValidationError("Dashboard.RefreshFailed", ex.Message));
        }
    }
}
