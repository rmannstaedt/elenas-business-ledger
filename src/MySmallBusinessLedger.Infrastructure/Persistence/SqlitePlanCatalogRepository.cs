using Microsoft.EntityFrameworkCore;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;

namespace MySmallBusinessLedger.Infrastructure.Persistence;

public sealed class SqlitePlanCatalogRepository : IPlanCatalogRepository
{
    private readonly Func<LedgerDbContext> createContext;

    public SqlitePlanCatalogRepository(Func<LedgerDbContext> createContext)
    {
        this.createContext = createContext;
    }

    public async Task<IReadOnlyList<PlanCatalogItem>> ListAllAsync(CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var entities = await context.PlanCatalog
            .AsNoTracking()
            .OrderBy(plan => plan.State)
            .ThenBy(plan => plan.Name)
            .ToArrayAsync(cancellationToken);

        return entities.Select(ToModel).ToArray();
    }

    public async Task<IReadOnlyList<PlanCatalogItem>> ListAssignableAsync(CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var entities = await context.PlanCatalog
            .AsNoTracking()
            .Where(plan => plan.State == PlanCatalogState.Active)
            .OrderBy(plan => plan.Name)
            .ToArrayAsync(cancellationToken);

        return entities.Select(ToModel).ToArray();
    }

    public async Task<PlanCatalogItem?> GetAsync(int id, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var entity = await context.PlanCatalog
            .AsNoTracking()
            .SingleOrDefaultAsync(plan => plan.Id == id, cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<OperationResult<PlanCatalogItem>> SaveAsync(
        PlanCatalogItem plan,
        CancellationToken cancellationToken)
    {
        await using var context = createContext();
        PlanCatalogEntity entity;
        if (plan.Id == 0)
        {
            entity = new PlanCatalogEntity();
            context.PlanCatalog.Add(entity);
        }
        else
        {
            entity = await context.PlanCatalog
                .SingleOrDefaultAsync(storedPlan => storedPlan.Id == plan.Id, cancellationToken)
                ?? new PlanCatalogEntity { Id = plan.Id };
            if (entity.Id == plan.Id && context.Entry(entity).State == EntityState.Detached)
            {
                context.PlanCatalog.Add(entity);
            }
        }

        entity.Name = plan.Name;
        entity.PriceWithoutVat = plan.PriceWithoutVat;
        entity.LessonsPerWeek = plan.LessonsPerWeek;
        entity.DurationMinutes = plan.DurationMinutes;
        entity.Flexibility = plan.Flexibility;
        entity.NumberOfStudents = plan.NumberOfStudents;
        entity.EffectiveFrom = plan.EffectiveFrom;
        entity.State = plan.State;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return OperationResult<PlanCatalogItem>.Failure(
                new ValidationError("Plan.SaveFailed", ex.Message));
        }

        return OperationResult<PlanCatalogItem>.Success(ToModel(entity));
    }

    public async Task<OperationResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var entity = await context.PlanCatalog
            .SingleOrDefaultAsync(plan => plan.Id == id, cancellationToken);
        if (entity is null)
        {
            return OperationResult<bool>.Failure(
                new ValidationError("Plan.NotFound", "Plan was not found."));
        }

        context.PlanCatalog.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }

    private static PlanCatalogItem ToModel(PlanCatalogEntity entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.PriceWithoutVat,
            entity.LessonsPerWeek,
            entity.DurationMinutes,
            entity.Flexibility,
            entity.NumberOfStudents,
            entity.EffectiveFrom,
            entity.State);
}
