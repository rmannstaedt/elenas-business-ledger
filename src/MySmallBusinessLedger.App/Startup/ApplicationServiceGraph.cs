using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.UseCases.Invoices;
using MySmallBusinessLedger.Core.UseCases.Lessons;
using MySmallBusinessLedger.Core.UseCases.Startup;
using MySmallBusinessLedger.Infrastructure.Calendar;
using MySmallBusinessLedger.Infrastructure.Invoices.Pdf;
using MySmallBusinessLedger.Infrastructure.Invoices.Printing;
using MySmallBusinessLedger.Infrastructure.Persistence;

namespace MySmallBusinessLedger.App.Startup;

internal sealed class ApplicationServiceGraph
{
    private const string ApplicationName = "My Small Business Ledger";
    private static readonly TimeSpan DefaultLessonDuration = TimeSpan.FromMinutes(60);

    private ApplicationServiceGraph(
        AppPaths appPaths,
        IStudentRepository studentRepository,
        IInvoiceRepository invoiceRepository,
        ICompanyProfileRepository companyProfileRepository,
        SqliteCalendarSettingsRepository calendarSettingsRepository,
        IPlanCatalogRepository planCatalogRepository,
        IPlanCatalogService planCatalogService,
        IStudentEditorService studentEditorService,
        IStudentOverviewService studentOverviewService,
        ILessonEditorService lessonEditorService,
        IInvoiceWorkflowService invoiceWorkflowService,
        IInvoiceWorkflowLauncher invoiceWorkflowLauncher,
        IAppSettingsService settingsService,
        IGoogleCalendarGateway calendarGateway,
        SystemUiClock systemClock)
    {
        AppPaths = appPaths;
        StudentRepository = studentRepository;
        InvoiceRepository = invoiceRepository;
        CompanyProfileRepository = companyProfileRepository;
        CalendarSettingsRepository = calendarSettingsRepository;
        PlanCatalogRepository = planCatalogRepository;
        PlanCatalogService = planCatalogService;
        StudentEditorService = studentEditorService;
        StudentOverviewService = studentOverviewService;
        LessonEditorService = lessonEditorService;
        InvoiceWorkflowService = invoiceWorkflowService;
        InvoiceWorkflowLauncher = invoiceWorkflowLauncher;
        SettingsService = settingsService;
        CalendarGateway = calendarGateway;
        SystemClock = systemClock;
    }

    public AppPaths AppPaths { get; }

    public IStudentRepository StudentRepository { get; }

    public IInvoiceRepository InvoiceRepository { get; }

    public ICompanyProfileRepository CompanyProfileRepository { get; }

    public SqliteCalendarSettingsRepository CalendarSettingsRepository { get; }

    public IPlanCatalogRepository PlanCatalogRepository { get; }

    public IPlanCatalogService PlanCatalogService { get; }

    public IStudentEditorService StudentEditorService { get; }

    public IStudentOverviewService StudentOverviewService { get; }

    public ILessonEditorService LessonEditorService { get; }

    public IInvoiceWorkflowService InvoiceWorkflowService { get; }

    public IInvoiceWorkflowLauncher InvoiceWorkflowLauncher { get; }

    public IAppSettingsService SettingsService { get; }

    public IGoogleCalendarGateway CalendarGateway { get; }

    public SystemUiClock SystemClock { get; }

    public static ApplicationServiceGraph Create(AppPaths appPaths, Func<LedgerDbContext> createContext)
    {
        var studentRepository = new SqliteStudentRepository(createContext);
        var invoiceRepository = new SqliteInvoiceRepository(createContext);
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
            ApplicationName,
            TimeZoneInfo.Local.Id);
        var calendarGateway = new ConfiguredGoogleCalendarGateway(
            googleCalendarClientFactory.CreateAsync,
            TimeZoneInfo.Local,
            DefaultLessonDuration);
        var studentEditorService = new CoreStudentEditorService(studentRepository);
        var studentOverviewService = new CoreStudentOverviewService(studentRepository, invoiceRepository);
        var lessonEditorService = new CoreLessonEditorService(
            new AddLessonUseCase(calendarGateway),
            new EditLessonUseCase(calendarGateway),
            new DeleteLessonUseCase(calendarGateway));
        var invoiceWorkflowService = new CoreInvoiceWorkflowService(
            new InvoiceCandidateSelectionUseCase(studentRepository, calendarGateway, invoiceRepository),
            new InvoicePreviewUseCase(studentRepository, companyProfileRepository, calendarGateway),
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
            googleCalendarClientFactory);
        var invoiceWorkflowLauncher = new InvoiceWorkflowWindowLauncher(
            invoiceWorkflowService,
            settingsService,
            systemClock);

        return new ApplicationServiceGraph(
            appPaths,
            studentRepository,
            invoiceRepository,
            companyProfileRepository,
            calendarSettingsRepository,
            planCatalogRepository,
            planCatalogService,
            studentEditorService,
            studentOverviewService,
            lessonEditorService,
            invoiceWorkflowService,
            invoiceWorkflowLauncher,
            settingsService,
            calendarGateway,
            systemClock);
    }
}
