using Microsoft.EntityFrameworkCore;
using System.Globalization;
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
        entity.PriceWithoutVat = FormatPrice(plan.PriceWithoutVat);
        entity.LessonsPerWeek = plan.LessonsPerWeek;
        entity.DurationMinutes = plan.DurationMinutes;
        entity.Flexibility = plan.Flexibility;
        entity.NumberOfStudents = plan.NumberOfStudents;
        entity.EffectiveFrom = plan.EffectiveFrom;
        entity.PaymentTerms = plan.PaymentTerms;
        entity.RetireOn = plan.RetireOn;
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

    public async Task<OperationResult<PlanCatalogItem>> RenameAsync(
        int id,
        string newName,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken)
    {
        var trimmedName = newName.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return OperationResult<PlanCatalogItem>.Failure(
                new ValidationError("Plan.NameRequired", "Plan name is required."));
        }

        await using var context = createContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entity = await context.PlanCatalog
            .SingleOrDefaultAsync(plan => plan.Id == id, cancellationToken);
        if (entity is null)
        {
            return OperationResult<PlanCatalogItem>.Failure(
                new ValidationError("Plan.NotFound", "Plan was not found."));
        }

        var duplicateExists = await context.PlanCatalog
            .AnyAsync(
                plan => plan.Id != id && EF.Functions.Collate(plan.Name, "NOCASE") == trimmedName,
                cancellationToken);
        if (duplicateExists)
        {
            return OperationResult<PlanCatalogItem>.Failure(
                new ValidationError("Plan.DuplicateName", "A plan with this name already exists."));
        }

        if (string.Equals(entity.Name, trimmedName, StringComparison.Ordinal))
        {
            return OperationResult<PlanCatalogItem>.Success(ToModel(entity));
        }

        var oldName = entity.Name;
        entity.Name = trimmedName;
        context.PlanNameChanges.Add(new PlanNameChangeEntity
        {
            PlanCatalogId = id,
            OldName = oldName,
            NewName = trimmedName,
            ChangedAt = changedAt
        });

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return OperationResult<PlanCatalogItem>.Failure(
                new ValidationError("Plan.RenameFailed", ex.Message));
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
            ParsePrice(entity.PriceWithoutVat),
            entity.LessonsPerWeek,
            entity.DurationMinutes,
            entity.Flexibility,
            entity.NumberOfStudents,
            entity.EffectiveFrom,
            entity.PaymentTerms,
            entity.RetireOn,
            entity.State);

    private static string FormatPrice(decimal? price) =>
        price is null ? string.Empty : price.Value.ToString(CultureInfo.InvariantCulture);

    private static decimal? ParsePrice(string price) =>
        string.IsNullOrWhiteSpace(price)
            ? null
            : decimal.Parse(price, CultureInfo.InvariantCulture);
}
