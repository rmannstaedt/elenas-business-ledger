using Microsoft.EntityFrameworkCore;

namespace MySmallBusinessLedger.Infrastructure.Persistence;

public sealed class LedgerDbContext : DbContext
{
    public LedgerDbContext(DbContextOptions<LedgerDbContext> options)
        : base(options)
    {
    }

    public DbSet<CompanyProfileEntity> CompanyProfiles => Set<CompanyProfileEntity>();

    public DbSet<CalendarSettingsEntity> CalendarSettings => Set<CalendarSettingsEntity>();

    public DbSet<PlanCatalogEntity> PlanCatalog => Set<PlanCatalogEntity>();

    public DbSet<StudentEntity> Students => Set<StudentEntity>();

    public DbSet<StudentPlanPeriodEntity> StudentPlanPeriods => Set<StudentPlanPeriodEntity>();

    public DbSet<StudentScheduleEntryEntity> StudentScheduleEntries => Set<StudentScheduleEntryEntity>();

    public DbSet<IssuedInvoiceEntity> IssuedInvoices => Set<IssuedInvoiceEntity>();

    public DbSet<IssuedInvoiceLineEntity> IssuedInvoiceLines => Set<IssuedInvoiceLineEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyProfileEntity>(entity =>
        {
            entity.HasKey(profile => profile.Id);
            entity.Property(profile => profile.Name).HasMaxLength(200).IsRequired();
            entity.Property(profile => profile.Address).HasMaxLength(500).IsRequired();
            entity.Property(profile => profile.Phone).HasMaxLength(100).IsRequired();
            entity.Property(profile => profile.Email).HasMaxLength(200).IsRequired();
            entity.Property(profile => profile.Cvr).HasMaxLength(50);
            entity.Property(profile => profile.VatStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(profile => profile.InvoiceFolderPath).HasMaxLength(1000).IsRequired();
        });

        modelBuilder.Entity<CalendarSettingsEntity>(entity =>
        {
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.SelectedCalendarId).HasMaxLength(500).IsRequired();
            entity.Property(settings => settings.GoogleAccountEmail).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<PlanCatalogEntity>(entity =>
        {
            entity.HasKey(plan => plan.Id);
            entity.Property(plan => plan.Name).HasMaxLength(200).IsRequired();
            entity.Property(plan => plan.PriceWithoutVat).HasMaxLength(50).IsRequired();
            entity.Property(plan => plan.PaymentTerms).HasMaxLength(500).IsRequired();
            entity.Property(plan => plan.LessonsPerWeek).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(plan => plan.Flexibility).HasConversion<string>().HasMaxLength(100).IsRequired();
            entity.Property(plan => plan.State).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.HasIndex(plan => plan.Name);
        });

        modelBuilder.Entity<StudentEntity>(entity =>
        {
            entity.HasKey(student => student.Id);
            entity.HasIndex(student => student.ShortName).IsUnique();
            entity.Property(student => student.ShortName).HasMaxLength(100).IsRequired();
            entity.Property(student => student.FullName).HasMaxLength(200).IsRequired();
            entity.Property(student => student.Address).HasMaxLength(500).IsRequired();
            entity.Property(student => student.Phone).HasMaxLength(100).IsRequired();
            entity.Property(student => student.Email).HasMaxLength(200).IsRequired();
            entity.Property(student => student.AgeBracket).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.HasMany(student => student.PlanPeriods)
                .WithOne(plan => plan.Student)
                .HasForeignKey(plan => plan.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(student => student.ScheduleEntries)
                .WithOne(schedule => schedule.Student)
                .HasForeignKey(schedule => schedule.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudentPlanPeriodEntity>(entity =>
        {
            entity.HasKey(plan => plan.Id);
            entity.Property(plan => plan.PlanName).HasMaxLength(200).IsRequired();
            entity.Property(plan => plan.PaymentTerms).HasMaxLength(500).IsRequired();
            entity.Property(plan => plan.LessonsPerWeek).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(plan => plan.Flexibility).HasConversion<string>().HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<StudentScheduleEntryEntity>(entity =>
        {
            entity.HasKey(schedule => schedule.Id);
            entity.Property(schedule => schedule.Weekday).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(schedule => new { schedule.StudentId, schedule.Weekday, schedule.StartsAt }).IsUnique();
        });

        modelBuilder.Entity<IssuedInvoiceEntity>(entity =>
        {
            entity.HasKey(invoice => invoice.Id);
            entity.HasIndex(invoice => invoice.InvoiceNumber).IsUnique();
            entity.Property(invoice => invoice.CompanyName).HasMaxLength(200).IsRequired();
            entity.Property(invoice => invoice.CompanyAddress).HasMaxLength(500).IsRequired();
            entity.Property(invoice => invoice.CompanyCvr).HasMaxLength(50);
            entity.Property(invoice => invoice.StudentShortName).HasMaxLength(100).IsRequired();
            entity.Property(invoice => invoice.StudentFullName).HasMaxLength(200).IsRequired();
            entity.Property(invoice => invoice.StudentAddress).HasMaxLength(500).IsRequired();
            entity.Property(invoice => invoice.Currency).HasMaxLength(3).IsRequired();
            entity.Property(invoice => invoice.PaymentStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(invoice => invoice.PdfFilePath).HasMaxLength(1000).IsRequired();
            entity.HasMany(invoice => invoice.Lines)
                .WithOne(line => line.Invoice)
                .HasForeignKey(line => line.IssuedInvoiceEntityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IssuedInvoiceLineEntity>(entity =>
        {
            entity.HasKey(line => line.Id);
            entity.HasIndex(line => line.LessonDateTime);
            entity.Property(line => line.LessonDateTime).HasMaxLength(40).IsRequired();
            entity.Property(line => line.PlanName).HasMaxLength(200).IsRequired();
            entity.Property(line => line.Currency).HasMaxLength(3).IsRequired();
        });
    }
}
