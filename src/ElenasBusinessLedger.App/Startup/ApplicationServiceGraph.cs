using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.UseCases.Economy;
using ElenasBusinessLedger.Core.UseCases.Invoices;
using ElenasBusinessLedger.Core.UseCases.Lessons;
using ElenasBusinessLedger.Core.UseCases.Startup;
using ElenasBusinessLedger.Infrastructure.Calendar;
using ElenasBusinessLedger.Infrastructure.Invoices.Pdf;
using ElenasBusinessLedger.Infrastructure.Invoices.Printing;
using ElenasBusinessLedger.Infrastructure.Persistence;

namespace ElenasBusinessLedger.App.Startup;

internal sealed class ApplicationServiceGraph
{
    private static readonly TimeSpan DefaultLessonDuration = TimeSpan.FromMinutes(60);

    private ApplicationServiceGraph(
        AppPaths appPaths,
        IStudentRepository studentRepository,
        ILessonRepository lessonRepository,
        IInvoiceRepository invoiceRepository,
        IEconomyRepository economyRepository,
        ICompanyProfileRepository companyProfileRepository,
        SqliteCalendarSettingsRepository calendarSettingsRepository,
        IPlanCatalogRepository planCatalogRepository,
        IPlanCatalogService planCatalogService,
        IStudentEditorService studentEditorService,
        IStudentOverviewService studentOverviewService,
        IEconomyOverviewService economyOverviewService,
        ILessonEditorService lessonEditorService,
        IInvoiceWorkflowService invoiceWorkflowService,
        IInvoiceWorkflowLauncher invoiceWorkflowLauncher,
        IAppSettingsService settingsService,
        IGoogleCalendarGateway calendarGateway,
        SystemUiClock systemClock)
    {
        AppPaths = appPaths;
        StudentRepository = studentRepository;
        LessonRepository = lessonRepository;
        InvoiceRepository = invoiceRepository;
        EconomyRepository = economyRepository;
        CompanyProfileRepository = companyProfileRepository;
        CalendarSettingsRepository = calendarSettingsRepository;
        PlanCatalogRepository = planCatalogRepository;
        PlanCatalogService = planCatalogService;
        StudentEditorService = studentEditorService;
        StudentOverviewService = studentOverviewService;
        EconomyOverviewService = economyOverviewService;
        LessonEditorService = lessonEditorService;
        InvoiceWorkflowService = invoiceWorkflowService;
        InvoiceWorkflowLauncher = invoiceWorkflowLauncher;
        SettingsService = settingsService;
        CalendarGateway = calendarGateway;
        SystemClock = systemClock;
    }

    public AppPaths AppPaths { get; }

    public IStudentRepository StudentRepository { get; }

    public ILessonRepository LessonRepository { get; }

    public IInvoiceRepository InvoiceRepository { get; }

    public IEconomyRepository EconomyRepository { get; }

    public ICompanyProfileRepository CompanyProfileRepository { get; }

    public SqliteCalendarSettingsRepository CalendarSettingsRepository { get; }

    public IPlanCatalogRepository PlanCatalogRepository { get; }

    public IPlanCatalogService PlanCatalogService { get; }

    public IStudentEditorService StudentEditorService { get; }

    public IStudentOverviewService StudentOverviewService { get; }

    public IEconomyOverviewService EconomyOverviewService { get; }

    public ILessonEditorService LessonEditorService { get; }

    public IInvoiceWorkflowService InvoiceWorkflowService { get; }

    public IInvoiceWorkflowLauncher InvoiceWorkflowLauncher { get; }

    public IAppSettingsService SettingsService { get; }

    public IGoogleCalendarGateway CalendarGateway { get; }

    public SystemUiClock SystemClock { get; }

    public static ApplicationServiceGraph Create(AppPaths appPaths, Func<LedgerDbContext> createContext)
    {
        var studentRepository = new SqliteStudentRepository(createContext);
        var lessonRepository = new SqliteLessonRepository(createContext);
        var invoiceRepository = new SqliteInvoiceRepository(createContext);
        var economyRepository = new SqliteEconomyRepository(createContext);
        var companyProfileRepository = new SqliteCompanyProfileRepository(createContext);
        var calendarSettingsRepository = new SqliteCalendarSettingsRepository(createContext);
        var planCatalogRepository = new SqlitePlanCatalogRepository(createContext);
        var systemClock = new SystemUiClock();
        var planCatalogService = new CorePlanCatalogService(planCatalogRepository);

        var googleOAuthClientConfigurationProvider = new GoogleOAuthClientConfigurationProvider(
            GoogleOAuthClientConfigurationProvider.CreateDefaultConfigurationPath());
        var googleCalendarClientFactory = new GoogleCalendarClientFactory(
            calendarSettingsRepository.GetAsync,
            googleOAuthClientConfigurationProvider,
            appPaths.GoogleTokenFolderPath,
            ApplicationInfo.ProductName,
            TimeZoneInfo.Local.Id);
        var calendarGateway = new ConfiguredGoogleCalendarGateway(
            googleCalendarClientFactory.CreateAsync,
            TimeZoneInfo.Local,
            DefaultLessonDuration);
        var studentEditorService = new CoreStudentEditorService(studentRepository);
        var studentOverviewService = new CoreStudentOverviewService(studentRepository, invoiceRepository);
        var economyOverviewService = new CoreEconomyOverviewService(
            new EconomyOverviewQuery(invoiceRepository, economyRepository),
            economyRepository,
            invoiceRepository);
        var lessonEditorService = new CoreLessonEditorService(
            new AddLessonUseCase(calendarGateway, studentRepository, planCatalogRepository, lessonRepository),
            new EditLessonUseCase(calendarGateway, lessonRepository),
            new DeleteLessonUseCase(calendarGateway, lessonRepository),
            new PopulateCalendarUseCase(studentRepository, calendarGateway, TimeZoneInfo.Local, lessonRepository),
            new UpdateLessonPlanUseCase(lessonRepository, planCatalogRepository));
        var invoiceWorkflowService = new CoreInvoiceWorkflowService(
            new InvoiceCandidateSelectionUseCase(
                studentRepository,
                lessonRepository),
            new InvoicePreviewUseCase(
                studentRepository,
                companyProfileRepository,
                lessonRepository,
                planCatalogRepository),
            companyProfileRepository,
            invoiceRepository,
            new QuestPdfInvoicePdfWriter(),
            new SavedPdfPrinter(new ShellPrintProcessLauncher(), new WindowsPrinterCatalog()),
            systemClock);
        var settingsService = new CoreAppSettingsService(
            companyProfileRepository,
            calendarSettingsRepository,
            appPaths,
            googleOAuthClientConfigurationProvider,
            googleCalendarClientFactory,
            economyRepository);
        var printerConfigurationService = new InvoicePrinterConfigurationService(
            settingsService,
            new WindowsInvoicePrinterSelectionDialog());
        var invoiceWorkflowLauncher = new InvoiceWorkflowWindowLauncher(
            invoiceWorkflowService,
            printerConfigurationService,
            systemClock);

        return new ApplicationServiceGraph(
            appPaths,
            studentRepository,
            lessonRepository,
            invoiceRepository,
            economyRepository,
            companyProfileRepository,
            calendarSettingsRepository,
            planCatalogRepository,
            planCatalogService,
            studentEditorService,
            studentOverviewService,
            economyOverviewService,
            lessonEditorService,
            invoiceWorkflowService,
            invoiceWorkflowLauncher,
            settingsService,
            calendarGateway,
            systemClock);
    }
}
