using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;

namespace ElenasBusinessLedger.Core.UseCases.Plans;

// Processes scheduled retirements so due plans leave the assignment lists.
public sealed class RetireDuePlansUseCase
{
    private readonly IPlanCatalogRepository planCatalogRepository;

    public RetireDuePlansUseCase(IPlanCatalogRepository planCatalogRepository)
    {
        this.planCatalogRepository = planCatalogRepository;
    }

    // Retires active plans whose retirement date has arrived so they leave the assignment list.
    public async Task<OperationResult<int>> ExecuteAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var plans = await planCatalogRepository.ListAllAsync(cancellationToken);
        var duePlans = plans
            .Where(plan => plan.State == PlanCatalogState.Active &&
                plan.RetireOn is not null &&
                plan.RetireOn.Value <= now)
            .ToArray();

        var retiredCount = 0;
        foreach (var plan in duePlans)
        {
            var result = await planCatalogRepository.SaveAsync(plan with { State = PlanCatalogState.Retired }, cancellationToken);
            if (!result.Succeeded)
            {
                return OperationResult<int>.Failure(result.Errors.ToArray());
            }

            retiredCount++;
        }

        return OperationResult<int>.Success(retiredCount);
    }
}
