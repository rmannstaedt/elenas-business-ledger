using ElenaSmallBusinessLedger.Core.Plans;
using ElenaSmallBusinessLedger.Core.Ports;
using ElenaSmallBusinessLedger.Core.Students;
using Microsoft.EntityFrameworkCore;

namespace ElenaSmallBusinessLedger.Infrastructure.Persistence;

public sealed class SqliteStudentRepository : IStudentRepository
{
    private readonly Func<LedgerDbContext> createContext;

    public SqliteStudentRepository(Func<LedgerDbContext> createContext)
    {
        this.createContext = createContext;
    }

    public async Task<IReadOnlyList<StudentRecord>> ListActiveStudentsAsync(CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var students = await context.Students
            .AsNoTracking()
            .Include(student => student.PlanPeriods)
            .Where(student => student.IsActive)
            .OrderBy(student => student.ShortName)
            .ToArrayAsync(cancellationToken);

        return students.Select(ToDomain).ToArray();
    }

    public async Task<StudentRecord?> GetByShortNameAsync(string shortName, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var student = await context.Students
            .AsNoTracking()
            .Include(entity => entity.PlanPeriods)
            .SingleOrDefaultAsync(entity => entity.ShortName == shortName, cancellationToken);

        return student is null ? null : ToDomain(student);
    }

    public async Task SaveAsync(StudentRecord student, CancellationToken cancellationToken)
    {
        await using var context = createContext();
        var entity = await context.Students
            .Include(storedStudent => storedStudent.PlanPeriods)
            .SingleOrDefaultAsync(storedStudent => storedStudent.ShortName == student.Student.ShortName, cancellationToken);

        if (entity is null)
        {
            entity = new StudentEntity();
            context.Students.Add(entity);
        }

        entity.ShortName = student.Student.ShortName;
        entity.FullName = student.Student.FullName;
        entity.Address = student.Student.Address;
        entity.Phone = student.Student.Phone;
        entity.Email = student.Student.Email;
        entity.AgeBracket = student.Student.AgeBracket;
        entity.Birthdate = student.Student.Birthdate;
        entity.IsActive = student.Student.IsActive;
        entity.PlanPeriods.Clear();

        foreach (var planPeriod in student.PlanPeriods)
        {
            entity.PlanPeriods.Add(new StudentPlanPeriodEntity
            {
                PlanName = planPeriod.Plan.Name,
                PriceWithoutVat = planPeriod.Plan.PriceWithoutVat,
                LessonsPerWeek = planPeriod.Plan.LessonsPerWeek,
                DurationMinutes = planPeriod.Plan.DurationMinutes,
                Flexibility = planPeriod.Plan.Flexibility,
                NumberOfStudents = planPeriod.Plan.NumberOfStudents,
                PlanEffectiveFrom = planPeriod.Plan.EffectiveFrom,
                StartsOn = planPeriod.StartsOn,
                EndsOn = planPeriod.EndsOn
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static StudentRecord ToDomain(StudentEntity entity)
    {
        var student = Student.Create(
            entity.ShortName,
            entity.FullName,
            entity.Address,
            entity.Phone,
            entity.Email,
            entity.AgeBracket,
            entity.Birthdate,
            entity.IsActive).Value!;

        var plans = entity.PlanPeriods
            .OrderBy(plan => plan.StartsOn)
            .Select(plan => new StudentPlanPeriod(
                new Plan(
                    plan.PlanName,
                    plan.PriceWithoutVat,
                    plan.LessonsPerWeek,
                    plan.DurationMinutes,
                    plan.Flexibility,
                    plan.NumberOfStudents,
                    plan.PlanEffectiveFrom),
                plan.StartsOn,
                plan.EndsOn))
            .ToArray();

        return new StudentRecord(student, plans);
    }
}
