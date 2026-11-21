using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.UseCases.Startup;

namespace ElenasBusinessLedger.App.Services;

public sealed class CoreStartupDashboardService : IStartupDashboardService
{
    private readonly StartupDashboardQuery query;

    public CoreStartupDashboardService(StartupDashboardQuery query)
    {
        this.query = query;
    }

    public async Task<OperationResult<StartupDashboardResult>> LoadAsync(
        DateOnly month,
        string? selectedStudentShortName,
        CancellationToken cancellationToken)
    {
        try
        {
            return await query.ExecuteAsync(month, selectedStudentShortName, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<StartupDashboardResult>.Failure(
                new ValidationError("Dashboard.RefreshFailed", ex.Message));
        }
    }
}
