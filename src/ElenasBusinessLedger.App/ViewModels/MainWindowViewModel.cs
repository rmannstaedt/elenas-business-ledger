using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using ElenasBusinessLedger.App.Commands;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Students;
using ElenasBusinessLedger.Core.UseCases.Lessons;
using ElenasBusinessLedger.Core.UseCases.Startup;

namespace ElenasBusinessLedger.App.ViewModels;

// Owns dashboard selection and coordinates refresh, scheduling, and invoice commands for the main window.
public sealed class MainWindowViewModel : ViewModelBase
{
    private static readonly TimeSpan ActivationRefreshMinimumInterval = TimeSpan.FromSeconds(10);
    private readonly IStartupDashboardService dashboardService;
    private readonly IInvoicePaymentStatusService paymentStatusService;
    private readonly IConfirmationService confirmationService;
    private readonly ICalendarLauncher calendarLauncher;
    private readonly IStudentOverviewLauncher studentOverviewLauncher;
    private readonly ILessonEditorLauncher lessonEditorLauncher;
    private readonly ILessonEditorService lessonEditorService;
    private readonly ILessonStateService lessonStateService;
    private readonly IInvoiceWorkflowLauncher invoiceWorkflowLauncher;
    private readonly IInvoicePdfLauncher invoicePdfLauncher;
    private readonly ISettingsLauncher settingsLauncher;
    private readonly IPlanCatalogLauncher planCatalogLauncher;
    private readonly IEconomyOverviewLauncher economyOverviewLauncher;
    private readonly IAboutDialogLauncher aboutDialogLauncher;
    private readonly IUiClock clock;
    private readonly TimeZoneInfo lessonTimeZone;
    private readonly DraftLessonAutosave draftAutosave;
    private readonly List<LessonDisplayModel> draftLessonRows = [];
    private readonly HashSet<LessonDisplayModel> savingPlanLessons = [];
    private readonly Dictionary<LessonDisplayModel, int> pendingPlanCatalogIds = [];
    private readonly HashSet<LessonDisplayModel> acceptingPlanChangeLessons = [];
    private long studentOverviewSelectionVersion;
    private DateTimeOffset? lastRefreshAt;
    private string? currentStudentShortName;
    private string[] studentShortNames = [];
    private int? currentStudentId;
    private DashboardStudentNavigationEntry[] studentNavigationEntries = [];
    private int? selectedStudentNavigationIndex;
    private bool suppressNavigationSelectionOnce;
    private DateOnly displayMonth;
    private DashboardStudentLessonDefaults? currentStudentLessonDefaults;
    private Task? activationRefreshTask;
    private bool isBusy;
    private bool isCalendarAvailable = true;
    private int activationRefreshSuspendCount;
    private LessonDisplayModel? selectedLesson;
    private InvoiceDisplayModel? selectedInvoice;
    private bool hasPendingInvoice;
    private string? pendingInvoiceNotice;
    private bool isAdminMode;
    private string? errorMessage;

    public MainWindowViewModel(
        IStartupDashboardService dashboardService,
        IInvoicePaymentStatusService paymentStatusService,
        IConfirmationService confirmationService,
        ICalendarLauncher calendarLauncher,
        IStudentOverviewLauncher studentOverviewLauncher,
        ILessonEditorLauncher lessonEditorLauncher,
        ILessonEditorService lessonEditorService,
        ILessonStateService lessonStateService,
        IInvoiceWorkflowLauncher invoiceWorkflowLauncher,
        IInvoicePdfLauncher invoicePdfLauncher,
        ISettingsLauncher settingsLauncher,
        IPlanCatalogLauncher planCatalogLauncher,
        IEconomyOverviewLauncher economyOverviewLauncher,
        IAboutDialogLauncher aboutDialogLauncher,
        IUiClock clock,
        TimeZoneInfo? lessonTimeZone = null)
    {
        this.dashboardService = dashboardService;
        this.paymentStatusService = paymentStatusService;
        this.confirmationService = confirmationService;
        this.calendarLauncher = calendarLauncher;
        this.studentOverviewLauncher = studentOverviewLauncher;
        this.lessonEditorLauncher = lessonEditorLauncher;
        this.lessonEditorService = lessonEditorService;
        this.lessonStateService = lessonStateService;
        this.invoiceWorkflowLauncher = invoiceWorkflowLauncher;
        this.invoicePdfLauncher = invoicePdfLauncher;
        this.settingsLauncher = settingsLauncher;
        this.planCatalogLauncher = planCatalogLauncher;
        this.economyOverviewLauncher = economyOverviewLauncher;
        this.aboutDialogLauncher = aboutDialogLauncher;
        this.clock = clock;
        this.lessonTimeZone = lessonTimeZone ?? TimeZoneInfo.Local;
        draftAutosave = new DraftLessonAutosave(SaveDraftLessonAsync);
        displayMonth = new DateOnly(clock.Now.Year, clock.Now.Month, 1);

        RefreshCommand = new AsyncRelayCommand(() => RefreshAsync(), () => !IsBusy);
        PreviousMonthCommand = new AsyncRelayCommand(() => ChangeMonthAsync(-1), () => !IsBusy);
        NextMonthCommand = new AsyncRelayCommand(() => ChangeMonthAsync(1), () => !IsBusy);
        PreviousStudentCommand = new AsyncRelayCommand(() => SelectAdjacentStudentAsync(-1), CanNavigateStudents);
        NextStudentCommand = new AsyncRelayCommand(() => SelectAdjacentStudentAsync(1), CanNavigateStudents);
        SelectStudentCommand = new AsyncRelayCommand(
            parameter => parameter is StudentChoice student
                ? SelectStudentFromDropdownAsync(student.ShortName, student.StudentId)
                : SelectStudentFromDropdownAsync((string)parameter!),
            parameter => !IsBusy && (parameter switch
            {
                StudentChoice student => student.StudentId != currentStudentId,
                string shortName => !StringComparer.OrdinalIgnoreCase.Equals(shortName, CurrentStudentShortName),
                _ => false
            }));
        ToggleInvoicePaymentStatusCommand = new AsyncRelayCommand(
            parameter => ToggleInvoicePaymentStatusAsync((InvoiceDisplayModel)parameter!),
            parameter => parameter is InvoiceDisplayModel { IsPending: false } && !IsBusy);
        OpenCalendarCommand = new AsyncRelayCommand(
            () => calendarLauncher.OpenAsync(CancellationToken.None),
            () => !IsBusy);
        WriteInvoiceCommand = new AsyncRelayCommand(WriteInvoiceAsync, CanStartWriteInvoice);
        ResumeInvoiceCommand = new AsyncRelayCommand(ResumeInvoiceAsync, () => !IsBusy);
        OpenInvoiceCommand = new AsyncRelayCommand(
            parameter => OpenInvoiceAsync((InvoiceDisplayModel)parameter!),
            parameter => parameter is InvoiceDisplayModel { IsPending: false } && !IsBusy);
        OpenInvoicePdfCommand = new AsyncRelayCommand(
            parameter => OpenInvoicePdfAsync((InvoiceDisplayModel)parameter!),
            parameter => parameter is InvoiceDisplayModel { IsPending: false } && !IsBusy);
        OpenManageStudentsCommand = new AsyncRelayCommand(OpenManageStudentsAsync, () => !IsBusy);
        OpenStudentDetailsCommand = new AsyncRelayCommand(
            OpenStudentDetailsAsync,
            () => !IsBusy && CurrentStudentShortName is not null);
        OpenSettingsCommand = new AsyncRelayCommand(OpenSettingsAsync, () => !IsBusy);
        OpenManagePlansCommand = new AsyncRelayCommand(OpenManagePlansAsync, () => !IsBusy);
        OpenEconomyOverviewCommand = new AsyncRelayCommand(OpenEconomyOverviewAsync, () => !IsBusy);
        OpenAboutCommand = new AsyncRelayCommand(OpenAboutAsync);
        AddLessonCommand = new AsyncRelayCommand(AddLessonAsync, CanScheduleLessons);
        PopulateCalendarCommand = new AsyncRelayCommand(PopulateCalendarAsync, CanScheduleLessons);
        SaveLessonCommand = new AsyncRelayCommand(
            parameter => SaveLessonAsync((LessonDisplayModel)parameter!),
            parameter => parameter is LessonDisplayModel && !IsBusy);
        DeleteLessonCommand = new AsyncRelayCommand(
            parameter => DeleteLessonAsync((LessonDisplayModel)parameter!),
            parameter => parameter is LessonDisplayModel lesson && lesson.CanEdit && !IsBusy);
        ToggleLessonStateCommand = new AsyncRelayCommand(
            parameter => ToggleLessonStateAsync((LessonDisplayModel)parameter!),
            parameter => parameter is LessonDisplayModel lesson && lesson.CanToggleState && !IsBusy);
        DismissErrorCommand = new AsyncRelayCommand(() =>
        {
            ErrorMessage = null;
            return Task.CompletedTask;
        });
    }

    public ObservableCollection<LessonDisplayModel> Lessons { get; } = [];

    public ObservableCollection<DashboardPlanOption> PlanOptions { get; } = [];

    public ObservableCollection<InvoiceDisplayModel> Invoices { get; } = [];

    public ObservableCollection<string> SelectableStudentShortNames { get; } = [];

    public ObservableCollection<StudentChoice> SelectableStudents { get; } = [];

    public LessonDisplayModel? SelectedLesson
    {
        get => selectedLesson;
        set => SetProperty(ref selectedLesson, value);
    }

    public InvoiceDisplayModel? SelectedInvoice
    {
        get => selectedInvoice;
        set => SetProperty(ref selectedInvoice, value);
    }

    public bool HasPendingInvoice
    {
        get => hasPendingInvoice;
        private set => SetProperty(ref hasPendingInvoice, value);
    }

    public string? PendingInvoiceNotice
    {
        get => pendingInvoiceNotice;
        private set => SetProperty(ref pendingInvoiceNotice, value);
    }

    public string? CurrentStudentShortName
    {
        get => currentStudentShortName;
        private set => SetProperty(ref currentStudentShortName, value);
    }

    public DateOnly DisplayMonth
    {
        get => displayMonth;
        private set
        {
            if (SetProperty(ref displayMonth, value))
            {
                OnPropertyChanged(nameof(DisplayMonthText));
            }
        }
    }

    public string DisplayMonthText => DisplayMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                RaiseCommandAvailabilityChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => errorMessage;
        private set
        {
            if (SetProperty(ref errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsAdminMode
    {
        get => isAdminMode;
        set => SetProperty(ref isAdminMode, value);
    }

    public AsyncRelayCommand RefreshCommand { get; }

    public AsyncRelayCommand PreviousMonthCommand { get; }

    public AsyncRelayCommand NextMonthCommand { get; }

    public AsyncRelayCommand PreviousStudentCommand { get; }

    public AsyncRelayCommand NextStudentCommand { get; }

    public AsyncRelayCommand SelectStudentCommand { get; }

    public AsyncRelayCommand ToggleInvoicePaymentStatusCommand { get; }

    public AsyncRelayCommand OpenCalendarCommand { get; }

    public AsyncRelayCommand WriteInvoiceCommand { get; }

    public AsyncRelayCommand ResumeInvoiceCommand { get; }

    public AsyncRelayCommand OpenInvoiceCommand { get; }

    public AsyncRelayCommand OpenInvoicePdfCommand { get; }

    public AsyncRelayCommand OpenManageStudentsCommand { get; }

    public AsyncRelayCommand OpenStudentDetailsCommand { get; }

    public AsyncRelayCommand OpenSettingsCommand { get; }

    public AsyncRelayCommand OpenManagePlansCommand { get; }

    public AsyncRelayCommand OpenEconomyOverviewCommand { get; }

    public AsyncRelayCommand OpenAboutCommand { get; }

    public AsyncRelayCommand AddLessonCommand { get; }

    public AsyncRelayCommand PopulateCalendarCommand { get; }

    public AsyncRelayCommand SaveLessonCommand { get; }

    public AsyncRelayCommand DeleteLessonCommand { get; }

    public AsyncRelayCommand ToggleLessonStateCommand { get; }

    public AsyncRelayCommand DismissErrorCommand { get; }

    // Loads the initial dashboard using the current display month and student selection.
    public Task LoadAsync() => RefreshAsync();

    // Refreshes when focus returns, while avoiding duplicate requests and refreshes during suspended editing.
    public async Task RefreshAfterActivationAsync()
    {
        if (activationRefreshSuspendCount > 0)
        {
            return;
        }

        if (lastRefreshAt is not null && clock.Now - lastRefreshAt.Value < ActivationRefreshMinimumInterval)
        {
            return;
        }

        var refreshTask = RefreshAsync();
        activationRefreshTask = refreshTask;
        RaiseCommandAvailabilityChanged();

        try
        {
            await refreshTask;
        }
        finally
        {
            if (ReferenceEquals(activationRefreshTask, refreshTask))
            {
                activationRefreshTask = null;
                RaiseCommandAvailabilityChanged();
            }
        }
    }

    private async Task RefreshAsync(Func<bool>? shouldApply = null, string? lessonIdToSelect = null)
    {
        IsBusy = true;
        try
        {
            var result = await dashboardService.LoadAsync(DisplayMonth, CurrentStudentShortName, CancellationToken.None, currentStudentId);
            if (shouldApply is not null && !shouldApply())
            {
                return;
            }

            ErrorMessage = result.Succeeded
                ? null
                : result.Errors.FirstOrDefault()?.Message ?? "Could not refresh dashboard.";
            // A failed calendar read can still return a fresh projection from the local ledger.
            if (result.Value is { } dashboard)
            {
                isCalendarAvailable = result.Succeeded;
                ApplyDashboard(dashboard, lessonIdToSelect);
                lastRefreshAt = clock.Now;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ChangeMonthAsync(int delta)
    {
        DisplayMonth = DisplayMonth.AddMonths(delta);
        await RefreshAsync();
    }

    private int FindCurrentNavigationIndex()
    {
        if (selectedStudentNavigationIndex is { } index &&
            index >= 0 &&
            index < studentNavigationEntries.Length &&
            MatchesStudent(studentNavigationEntries[index].StudentId, studentNavigationEntries[index].StudentShortName,
                currentStudentId, CurrentStudentShortName))
        {
            return index;
        }

        if (SelectedLesson is { } selectedLesson &&
            selectedLesson.TryGetStartsAt(out var selectedStartsAt))
        {
            var selectedLessonIndex = Array.FindIndex(
                studentNavigationEntries,
                entry =>
                    MatchesStudent(entry.StudentId, entry.StudentShortName, selectedLesson.StudentId, selectedLesson.StudentShortName) &&
                    entry.StartsAt == selectedStartsAt);
            if (selectedLessonIndex >= 0)
            {
                return selectedLessonIndex;
            }
        }

        return FindStartingNavigationIndex(CurrentStudentShortName);
    }

    private async Task SelectAdjacentStudentAsync(int delta)
    {
        if (studentNavigationEntries.Length == 0)
        {
            return;
        }

        var currentIndex = FindCurrentNavigationIndex();
        var nextIndex = currentIndex < 0
            ? delta > 0 ? 0 : studentNavigationEntries.Length - 1
            : (currentIndex + delta + studentNavigationEntries.Length) % studentNavigationEntries.Length;

        var nextEntry = studentNavigationEntries[nextIndex];
        selectedStudentNavigationIndex = nextIndex;
        currentStudentId = nextEntry.StudentId;
        CurrentStudentShortName = nextEntry.StudentShortName;
        DisplayMonth = new DateOnly(nextEntry.StartsAt.Year, nextEntry.StartsAt.Month, 1);
        await RefreshAsync();
    }

    private async Task SelectStudentFromDropdownAsync(string studentShortName, int? studentId = null)
    {
        selectedStudentNavigationIndex = null;
        suppressNavigationSelectionOnce = true;
        currentStudentId = studentId;
        CurrentStudentShortName = studentShortName;
        SelectedLesson = null;
        await RefreshAsync();
        suppressNavigationSelectionOnce = false;
    }

    private async Task ToggleInvoicePaymentStatusAsync(InvoiceDisplayModel invoice)
    {
        if (invoice.PaymentStatus == PaymentStatus.Paid)
        {
            await RunWithActivationRefreshSuspendedAsync(async () =>
            {
                var confirmed = await confirmationService.ConfirmMarkInvoiceOutstandingAsync(invoice, CancellationToken.None);
                if (confirmed)
                {
                    await ToggleInvoicePaymentStatusCoreAsync(invoice, confirmMarkOutstanding: true);
                }
            });
            return;
        }

        await ToggleInvoicePaymentStatusCoreAsync(invoice, confirmMarkOutstanding: false);
    }

    private async Task ToggleInvoicePaymentStatusCoreAsync(InvoiceDisplayModel invoice, bool confirmMarkOutstanding)
    {
        var result = await paymentStatusService.ToggleAsync(invoice.InvoiceNumber, confirmMarkOutstanding, CancellationToken.None);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Could not update payment status.";
            return;
        }

        invoice.PaymentStatus = result.Value;
        ApplyInvoicePaymentStatusToLessons(invoice);
        ErrorMessage = null;
    }

    private void ApplyInvoicePaymentStatusToLessons(InvoiceDisplayModel invoice)
    {
        foreach (var lesson in Lessons.Where(lesson => lesson.InvoiceNumber == invoice.InvoiceNumber))
        {
            lesson.PaymentStatus = invoice.PaymentStatus;
        }
    }

    private async Task OpenManageStudentsAsync()
    {
        var selectedStudent = await studentOverviewLauncher.ShowStudentOverviewForAsync(
            CurrentStudentChoice(),
            ApplyStudentOverviewSelectionAsync,
            CancellationToken.None);
        await ApplyStudentOverviewSelectionAsync(selectedStudent, CancellationToken.None);
    }

    private async Task OpenStudentDetailsAsync()
    {
        var selectedStudent = await studentOverviewLauncher.ShowStudentDetailsForAsync(
            CurrentStudentChoice(),
            CancellationToken.None);
        await ApplyStudentOverviewSelectionAsync(selectedStudent, CancellationToken.None);
    }

    private StudentChoice? CurrentStudentChoice() =>
        CurrentStudentShortName is { } name ? new StudentChoice(currentStudentId ?? 0, name) : null;

    private async Task ApplyStudentOverviewSelectionAsync(StudentChoice? selectedStudent, CancellationToken cancellationToken)
    {
        if (selectedStudent is null || string.IsNullOrWhiteSpace(selectedStudent.ShortName))
        {
            return;
        }

        currentStudentId = selectedStudent.StudentId > 0 ? selectedStudent.StudentId : null;
        CurrentStudentShortName = selectedStudent.ShortName;
        var selectionVersion = Interlocked.Increment(ref studentOverviewSelectionVersion);
        await RefreshAsync(() => selectionVersion == Interlocked.Read(ref studentOverviewSelectionVersion));
    }

    private async Task OpenSettingsAsync()
    {
        var result = await settingsLauncher.ShowSettingsAsync(IsAdminMode, CancellationToken.None);
        if (result.WasSaved)
        {
            IsAdminMode = result.IsAdminMode;
            await RefreshAsync();
        }
    }

    private Task OpenManagePlansAsync() =>
        planCatalogLauncher.ShowPlanCatalogAsync(CancellationToken.None);

    private Task OpenEconomyOverviewAsync() =>
        economyOverviewLauncher.ShowEconomyOverviewAsync(IsAdminMode, CancellationToken.None);

    private Task OpenAboutAsync() =>
        aboutDialogLauncher.ShowAboutAsync(CancellationToken.None);

    private async Task WriteInvoiceAsync()
    {
        await WaitForActivationRefreshAsync();

        if (IsBusy || HasPendingInvoice || CurrentStudentChoice() is not { } student)
        {
            return;
        }

        await RunWithActivationRefreshSuspendedAsync(async () =>
        {
            if (ResolveInvoiceForWriteButton() is { } existingInvoice)
            {
                await OpenInvoiceAsync(existingInvoice);
                return;
            }

            var refreshRequired = await invoiceWorkflowLauncher.ShowWriteInvoiceAsync(
                DisplayMonth,
                student,
                CancellationToken.None);
            if (refreshRequired)
            {
                await RefreshAsync();
            }
        });
    }

    private async Task ResumeInvoiceAsync()
    {
        await WaitForActivationRefreshAsync();
        await RunWithActivationRefreshSuspendedAsync(async () =>
        {
            if (await invoiceWorkflowLauncher.ShowResumeInvoiceAsync(CancellationToken.None))
            {
                await RefreshAsync();
            }
        });
    }

    private Task OpenInvoiceAsync(InvoiceDisplayModel invoice) =>
        invoiceWorkflowLauncher.ShowInvoiceViewerAsync(invoice.InvoiceNumber, CancellationToken.None);

    private async Task OpenInvoicePdfAsync(InvoiceDisplayModel invoice)
    {
        SelectedInvoice = invoice;
        if (string.IsNullOrWhiteSpace(invoice.PdfFilePath))
        {
            ErrorMessage = "The selected invoice does not have a saved PDF file yet.";
            return;
        }

        try
        {
            await invoicePdfLauncher.OpenAsync(invoice.PdfFilePath, CancellationToken.None);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not open invoice PDF. {ex.Message}";
        }
    }

    private async Task AddLessonAsync()
    {
        if (CurrentStudentShortName is not { } studentShortName)
        {
            return;
        }

        if (PlanOptions.Count == 0)
        {
            ErrorMessage = "Create or publish an active plan first.";
            return;
        }

        var today = DateOnly.FromDateTime(clock.Now.DateTime);
        var defaultDate = DisplayMonth == new DateOnly(today.Year, today.Month, 1) ? today : DisplayMonth;
        TimeOnly? defaultTime = null;
        DashboardPlanOption? defaultPlan = null;
        if (currentStudentLessonDefaults is { } defaults &&
            StringComparer.Ordinal.Equals(defaults.StudentShortName, studentShortName))
        {
            var slot = defaults.Schedule.FirstOrDefault(entry => entry.Weekday == defaultDate.DayOfWeek)
                ?? defaults.Schedule.FirstOrDefault();
            defaultTime = slot?.StartsAt;
            var plan = StudentPlanPeriod.GetActivePlan(defaults.PlanPeriods, defaultDate);
            defaultPlan = PlanOptions.FirstOrDefault(option => option.PlanCatalogId == plan?.PlanCatalogId);
        }

        var draft = LessonDisplayModel.CreateDraft(
            studentShortName, lessonTimeZone, defaultDate.ToDateTime(TimeOnly.MinValue), defaultTime, defaultPlan, currentStudentId);
        draftLessonRows.Add(draft);
        AddLessonRow(draft);
        SelectedLesson = draft;
        RaiseCommandAvailabilityChanged();
        if (defaultPlan is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await SaveLessonAsync(draft);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PopulateCalendarAsync()
    {
        if (CurrentStudentShortName is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await lessonEditorService.PopulateCalendarAsync(
                CurrentStudentShortName,
                DisplayMonth,
                CancellationToken.None,
                currentStudentId);
            if (!result.Succeeded)
            {
                ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Google Calendar could not be populated.";
                return;
            }

            ErrorMessage = null;
            await RefreshAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ClearError()
    {
        ErrorMessage = null;
    }

    private async Task SaveLessonAsync(LessonDisplayModel lesson)
    {
        if (!lesson.CanEdit)
        {
            ErrorMessage = lesson.State == LessonState.Completed
                ? "Completed lessons cannot be edited."
                : "Invoiced lessons cannot be edited.";
            return;
        }

        if (lesson.IsDraft)
        {
            await draftAutosave.SaveNowAsync(lesson);
            return;
        }

        if (!lesson.TryGetStartsAt(out var startsAt, out var hasKnownStartTime))
        {
            ErrorMessage = null;
            return;
        }
        if (!lesson.HasDateTimeChange)
        {
            ErrorMessage = null;
            return;
        }

        var result = await lessonEditorService.EditAsync(
            new LessonEditRequest(lesson.Id, lesson.StudentShortName, startsAt, hasKnownStartTime),
            DisplayMonth,
            CancellationToken.None);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Google Calendar lesson update failed.";
            return;
        }

        ErrorMessage = null;
        lesson.AcceptDateTimeChange();
        await RefreshAsync();
    }

    private async Task SaveDraftLessonAsync(LessonDisplayModel lesson)
    {
        var requestedPlanCatalogId = lesson.PlanCatalogId;
        if (requestedPlanCatalogId <= 0 || !lesson.TryGetStartsAt(out var startsAt, out var hasKnownStartTime))
        {
            ErrorMessage = null;
            return;
        }

        var result = await lessonEditorService.AddAsync(
            new LessonAddRequest(lesson.StudentShortName, startsAt, requestedPlanCatalogId, hasKnownStartTime, lesson.StudentId),
            DisplayMonth,
            CancellationToken.None);
        if (result.Value is { CreatedLesson: { } createdLesson })
        {
            lesson.AcceptCreatedLesson(createdLesson);
            draftLessonRows.Remove(lesson);
            RaiseCommandAvailabilityChanged();
        }

        if (!result.Succeeded)
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Google Calendar lesson creation failed.";
            return;
        }

        ErrorMessage = null;
        await RefreshAsync(lessonIdToSelect: ReferenceEquals(SelectedLesson, lesson) ? lesson.Id : null);
    }

    private async Task DeleteLessonAsync(LessonDisplayModel lesson)
    {
        if (!lesson.CanEdit)
        {
            ErrorMessage = lesson.State == LessonState.Completed
                ? "Completed lessons cannot be deleted."
                : "Invoiced lessons cannot be deleted.";
            return;
        }

        if (lesson.IsDraft)
        {
            RemoveLessonRow(lesson);
            if (ReferenceEquals(SelectedLesson, lesson))
            {
                SelectedLesson = null;
            }

            ErrorMessage = null;
            RaiseCommandAvailabilityChanged();
            return;
        }

        await RunWithActivationRefreshSuspendedAsync(async () =>
        {
            var confirmed = await confirmationService.ConfirmDeleteLessonAsync(lesson, CancellationToken.None);
            if (confirmed)
            {
                await DeleteConfirmedLessonAsync(lesson);
            }
        });
    }

    private async Task DeleteConfirmedLessonAsync(LessonDisplayModel lesson)
    {
        var result = await lessonEditorService.DeleteAsync(lesson.Id, DisplayMonth, CancellationToken.None);
        var refreshError = result.Errors.FirstOrDefault(error => error.Code == "Calendar.RefreshFailed");
        if (!result.Succeeded && refreshError is null)
        {
            await RefreshAsync();
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Google Calendar lesson deletion failed.";
            return;
        }

        ErrorMessage = refreshError?.Message;
        RemoveLessonRow(lesson);
        if (ReferenceEquals(SelectedLesson, lesson))
        {
            SelectedLesson = null;
        }

        RaiseCommandAvailabilityChanged();
    }

    private async Task ToggleLessonStateAsync(LessonDisplayModel lesson)
    {
        if (lesson.NeedsCalendarReview)
        {
            await RunWithActivationRefreshSuspendedAsync(async () =>
            {
                var review = await lessonEditorService.ReviewCalendarMutationAsync(
                    lesson.Id, DisplayMonth, confirmRemoved: false, CancellationToken.None);
                if (review.RequiresConfirmation)
                {
                    if (!await confirmationService.ConfirmCalendarRemovalAsync(lesson, CancellationToken.None))
                    {
                        return;
                    }
                    review = await lessonEditorService.ReviewCalendarMutationAsync(
                        lesson.Id, DisplayMonth, confirmRemoved: true, CancellationToken.None);
                }

                if (review.Succeeded)
                {
                    await RefreshAsync();
                }
                else
                {
                    ErrorMessage = review.Errors[0].Message;
                }
            });
            return;
        }

        if (lesson.IsInvoiced)
        {
            ErrorMessage = "Invoiced lessons cannot be changed.";
            return;
        }

        if (lesson.State == LessonState.Completed)
        {
            await RunWithActivationRefreshSuspendedAsync(async () =>
            {
                var confirmed = await confirmationService.ConfirmMarkLessonPlannedAsync(lesson, CancellationToken.None);
                if (confirmed)
                {
                    await ToggleLessonStateCoreAsync(lesson, confirmReturnToPlanned: true);
                }
            });
            return;
        }

        await ToggleLessonStateCoreAsync(lesson, confirmReturnToPlanned: false);
    }

    private async Task ToggleLessonStateCoreAsync(LessonDisplayModel lesson, bool confirmReturnToPlanned)
    {
        var result = await lessonStateService.ToggleAsync(
            lesson.Id,
            lesson.State,
            confirmReturnToPlanned,
            CancellationToken.None);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Could not update lesson status.";
            return;
        }

        lesson.State = result.Value;
        ErrorMessage = null;
        RaiseCommandAvailabilityChanged();
    }

    private async Task EditLessonAsync(LessonDisplayModel lesson)
    {
        if (!lesson.CanEdit)
        {
            ErrorMessage = lesson.State == LessonState.Completed
                ? "Completed lessons cannot be edited."
                : "Invoiced lessons cannot be edited.";
            return;
        }

        await SaveLessonAsync(lesson);
    }

    private async Task RunWithActivationRefreshSuspendedAsync(Func<Task> action)
    {
        activationRefreshSuspendCount++;
        try
        {
            await action();
        }
        finally
        {
            activationRefreshSuspendCount--;
        }
    }

    private async Task<T> RunWithActivationRefreshSuspendedAsync<T>(Func<Task<T>> action)
    {
        activationRefreshSuspendCount++;
        try
        {
            return await action();
        }
        finally
        {
            activationRefreshSuspendCount--;
        }
    }

    private void ApplyDashboard(StartupDashboardResult dashboard, string? lessonIdToSelect)
    {
        var selectedLesson = SelectedLesson;
        var selectedLessonId = selectedLesson?.Id;
        var selectedInvoiceNumber = SelectedInvoice?.InvoiceNumber;
        var preferredNavigationEntry = selectedStudentNavigationIndex is { } index &&
            index >= 0 && index < studentNavigationEntries.Length
            ? studentNavigationEntries[index]
            : null;
        currentStudentId = dashboard.CurrentStudentId;
        CurrentStudentShortName = dashboard.CurrentStudentShortName;
        SelectableStudents.Clear();
        foreach (var student in dashboard.StudentChoices ?? [])
        {
            if (student.StudentId > 0 && student.StudentId != currentStudentId)
            {
                SelectableStudents.Add(student);
            }
        }
        currentStudentLessonDefaults = dashboard.LessonDefaults;
        studentShortNames = dashboard.StudentShortNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        RefreshSelectableStudentShortNames();
        studentNavigationEntries = dashboard.StudentNavigationEntries.ToArray();
        PlanOptions.Clear();
        foreach (var planOption in dashboard.PlanOptions ?? [])
        {
            PlanOptions.Add(planOption);
        }

        var suppressNavigationSelection = suppressNavigationSelectionOnce;
        suppressNavigationSelectionOnce = false;
        selectedStudentNavigationIndex = suppressNavigationSelection
            ? null
            : ResolveNavigationIndex(
                preferredNavigationEntry,
                dashboard.CurrentStudentShortName,
                selectedLesson);

        ClearLessonRows();
        foreach (var lesson in dashboard.Lessons
                     .Where(lesson => IsInDisplayMonth(lesson.StartsAt))
                     .OrderBy(lesson => lesson.StartsAt))
        {
            AddLessonRow(new LessonDisplayModel(lesson, lessonTimeZone));
        }

        foreach (var draft in draftLessonRows.Where(IsVisibleDraftForDisplayMonth))
        {
            AddLessonRow(draft);
        }

        SortLessonsPreservingSelection(selectedLesson);

        var navigationSelectedLesson = suppressNavigationSelection
            ? null
            : FindSelectedNavigationLesson();
        var refreshedSelectedLesson = selectedLessonId is null
            ? null
            : Lessons.FirstOrDefault(lesson => string.Equals(lesson.Id, selectedLessonId, StringComparison.OrdinalIgnoreCase));
        refreshedSelectedLesson ??= FindRefreshedLessonByDateTime(selectedLesson);
        if (lessonIdToSelect is not null)
        {
            SelectedLesson = Lessons.FirstOrDefault(lesson =>
                StringComparer.OrdinalIgnoreCase.Equals(lesson.Id, lessonIdToSelect));
        }
        else if (selectedLesson is { IsDraft: true } && Lessons.Contains(selectedLesson))
        {
            SelectedLesson = selectedLesson;
        }
        else if (navigationSelectedLesson is not null)
        {
            SelectedLesson = navigationSelectedLesson;
        }
        else if (refreshedSelectedLesson is not null)
        {
            SelectedLesson = refreshedSelectedLesson;
        }
        else if (!suppressNavigationSelection && HasSelectedNavigationEntry())
        {
            SelectedLesson = null;
        }
        else
        {
            SelectedLesson = null;
        }

        HasPendingInvoice = dashboard.PendingInvoiceForAnotherStudent is not null ||
            dashboard.Invoices.Any(invoice => invoice.IssueStatus == InvoiceIssueStatus.Issuing);
        PendingInvoiceNotice = dashboard.PendingInvoiceForAnotherStudent is { } pending
            ? $"Invoice {pending.InvoiceNumber} for {pending.StudentShortName} is pending. Use Resume Invoice in the Invoice menu before issuing another invoice."
            : null;

        Invoices.Clear();
        foreach (var invoice in dashboard.Invoices
            .OrderByDescending(invoice => invoice.IssueStatus == InvoiceIssueStatus.Issuing)
            .ThenByDescending(invoice => invoice.InvoiceDate))
        {
            Invoices.Add(new InvoiceDisplayModel(invoice));
        }

        SelectedInvoice = selectedInvoiceNumber is null
            ? null
            : Invoices.FirstOrDefault(invoice => invoice.InvoiceNumber == selectedInvoiceNumber.Value);

        RaiseCommandAvailabilityChanged();
    }

    private LessonDisplayModel? FindRefreshedLessonByDateTime(LessonDisplayModel? selectedLesson)
    {
        if (selectedLesson is null ||
            !selectedLesson.TryGetStartsAt(out var selectedStartsAt, out var selectedHasKnownStartTime))
        {
            return null;
        }

        return Lessons.FirstOrDefault(lesson =>
            MatchesStudent(lesson.StudentId, lesson.StudentShortName, selectedLesson.StudentId, selectedLesson.StudentShortName) &&
            lesson.TryGetStartsAt(out var startsAt, out var hasKnownStartTime) &&
            startsAt == selectedStartsAt &&
            hasKnownStartTime == selectedHasKnownStartTime);
    }

    private InvoiceDisplayModel? ResolveInvoiceForWriteButton()
    {
        if (Lessons.Any(lesson => !lesson.IsInvoiced))
        {
            return null;
        }

        var lessonInvoiceNumbers = Lessons
            .Where(lesson => lesson.IsInvoiced && lesson.InvoiceNumber is not null)
            .Select(lesson => lesson.InvoiceNumber!.Value)
            .Distinct()
            .ToArray();

        if (lessonInvoiceNumbers.Length == 1)
        {
            var invoiceNumber = lessonInvoiceNumbers[0];
            return Invoices.FirstOrDefault(invoice => invoice.InvoiceNumber == invoiceNumber);
        }

        return Lessons.Count == 0 && SelectedInvoice is { IsPending: false } ? SelectedInvoice : null;
    }

    private void RefreshSelectableStudentShortNames()
    {
        SelectableStudentShortNames.Clear();
        foreach (var shortName in studentShortNames.Where(
                     shortName => !StringComparer.OrdinalIgnoreCase.Equals(shortName, CurrentStudentShortName)))
        {
            SelectableStudentShortNames.Add(shortName);
        }
    }

    private int? ResolveNavigationIndex(
        DashboardStudentNavigationEntry? preferredEntry,
        string? currentStudent,
        LessonDisplayModel? selectedLesson)
    {
        if (studentNavigationEntries.Length == 0)
        {
            return null;
        }

        if (preferredEntry is not null &&
            IsInDisplayMonth(preferredEntry.StartsAt) &&
            MatchesStudent(preferredEntry.StudentId, preferredEntry.StudentShortName, currentStudentId, currentStudent))
        {
            var preferredIndex = Array.IndexOf(studentNavigationEntries, preferredEntry);
            if (preferredIndex >= 0)
            {
                return preferredIndex;
            }
        }

        if (selectedLesson is { IsDraft: false } &&
            IsInDisplayMonth(selectedLesson.StartsAt) &&
            MatchesStudent(selectedLesson.StudentId, selectedLesson.StudentShortName, currentStudentId, currentStudent))
        {
            var matchingLessonIndex = Array.FindIndex(
                studentNavigationEntries,
                entry => NavigationEntryMatchesLesson(entry, selectedLesson));
            if (matchingLessonIndex >= 0)
            {
                return matchingLessonIndex;
            }
        }

        var startingIndex = FindStartingNavigationIndex(currentStudent);
        return startingIndex >= 0 ? startingIndex : null;
    }

    private int FindStartingNavigationIndex(string? student)
    {
        var now = clock.Now;
        var studentEntries = studentNavigationEntries.Where(entry =>
            MatchesStudent(entry.StudentId, entry.StudentShortName, currentStudentId, student));
        var displayedEntries = studentEntries.Where(entry => IsInDisplayMonth(entry.StartsAt));
        var startingEntry = displayedEntries
            .Where(entry => entry.HasCalendarLesson && entry.StartsAt.Date == now.Date)
            .MinBy(entry => (entry.StartsAt - now).Duration())
            ?? displayedEntries.FirstOrDefault()
            ?? studentEntries.FirstOrDefault();

        return startingEntry is null ? -1 : Array.IndexOf(studentNavigationEntries, startingEntry);
    }

    private bool HasSelectedNavigationEntry() =>
        selectedStudentNavigationIndex is { } index &&
        index >= 0 &&
        index < studentNavigationEntries.Length;

    private LessonDisplayModel? FindSelectedNavigationLesson()
    {
        if (selectedStudentNavigationIndex is not { } index ||
            index < 0 ||
            index >= studentNavigationEntries.Length)
        {
            return null;
        }

        var entry = studentNavigationEntries[index];
        if (!entry.HasCalendarLesson)
        {
            return null;
        }

        return Lessons.FirstOrDefault(lesson => NavigationEntryMatchesLesson(entry, lesson));
    }

    private static bool NavigationEntryMatchesLesson(
        DashboardStudentNavigationEntry entry,
        LessonDisplayModel lesson) =>
        MatchesStudent(entry.StudentId, entry.StudentShortName, lesson.StudentId, lesson.StudentShortName) &&
        lesson.TryGetStartsAt(out var startsAt) &&
        startsAt == entry.StartsAt;

    private static bool MatchesStudent(int? firstId, string? firstName, int? secondId, string? secondName) =>
        firstId is > 0 && secondId is > 0 ? firstId == secondId :
            StringComparer.OrdinalIgnoreCase.Equals(firstName, secondName);

    private void AddLessonRow(LessonDisplayModel lesson)
    {
        lesson.PropertyChanged += OnLessonPropertyChanged;
        Lessons.Add(lesson);
    }

    private bool IsInDisplayMonth(DateTimeOffset startsAt) =>
        startsAt.Year == DisplayMonth.Year &&
        startsAt.Month == DisplayMonth.Month;

    private bool IsVisibleDraftForDisplayMonth(LessonDisplayModel lesson) =>
        (lesson.StudentId is null || lesson.StudentId == currentStudentId) &&
        (lesson.StartsOnDate is null ||
         (lesson.StartsOnDate.Value.Year == DisplayMonth.Year && lesson.StartsOnDate.Value.Month == DisplayMonth.Month));

    private void RemoveLessonRow(LessonDisplayModel lesson)
    {
        lesson.PropertyChanged -= OnLessonPropertyChanged;
        draftAutosave.CancelPending(lesson);
        draftLessonRows.Remove(lesson);
        Lessons.Remove(lesson);
    }

    private void ClearLessonRows()
    {
        foreach (var lesson in Lessons)
        {
            lesson.PropertyChanged -= OnLessonPropertyChanged;
        }

        Lessons.Clear();
    }

    private void OnLessonPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not LessonDisplayModel lesson ||
            e.PropertyName is not (nameof(LessonDisplayModel.StartsAt) or
                                   nameof(LessonDisplayModel.StartsAtTimeText) or
                                   nameof(LessonDisplayModel.PlanCatalogId)))
        {
            return;
        }

        if (e.PropertyName == nameof(LessonDisplayModel.PlanCatalogId))
        {
            if (acceptingPlanChangeLessons.Contains(lesson))
            {
                return;
            }

            if (lesson.IsDraft)
            {
                draftAutosave.Schedule(lesson);
                return;
            }

            TrySaveLessonPlanChange(lesson);
            return;
        }

        SortLessonsPreservingSelection(lesson);
        draftAutosave.Schedule(lesson);
    }

    private void TrySaveLessonPlanChange(LessonDisplayModel lesson)
    {
        if (lesson.IsDraft ||
            !lesson.CanEditPlan ||
            (!lesson.NeedsPlan && lesson.LocalLessonId <= 0) ||
            lesson.PlanCatalogId <= 0)
        {
            return;
        }

        pendingPlanCatalogIds[lesson] = lesson.PlanCatalogId;
        if (savingPlanLessons.Contains(lesson))
        {
            return;
        }

        savingPlanLessons.Add(lesson);
        _ = SaveLessonPlanChangeAsync(lesson);
    }

    private async Task SaveLessonPlanChangeAsync(LessonDisplayModel lesson)
    {
        try
        {
            while (pendingPlanCatalogIds.Remove(lesson, out var requestedPlanCatalogId))
            {
                var result = lesson.NeedsPlan
                    ? await lessonEditorService.LinkToPlanAsync(
                        new LessonRecord(0, lesson.StudentId ?? 0, lesson.StudentShortName, 0, string.Empty,
                            lesson.StartsAt, 0, lesson.State, lesson.Id, null, lesson.HasKnownStartTime),
                        requestedPlanCatalogId, CancellationToken.None)
                    : await lessonEditorService.UpdatePlanAsync(
                        lesson.LocalLessonId, requestedPlanCatalogId, CancellationToken.None);
                if (!result.Succeeded)
                {
                    if (lesson.PlanCatalogId == requestedPlanCatalogId)
                    {
                        ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Could not update lesson plan.";
                        return;
                    }

                    continue;
                }

                if (lesson.PlanCatalogId != requestedPlanCatalogId)
                {
                    continue;
                }

                acceptingPlanChangeLessons.Add(lesson);
                try
                {
                    lesson.AcceptPlanChange(result.Value!);
                }
                finally
                {
                    acceptingPlanChangeLessons.Remove(lesson);
                }

                ErrorMessage = null;
            }
        }
        finally
        {
            savingPlanLessons.Remove(lesson);
        }
    }

    private void SortLessonsPreservingSelection(LessonDisplayModel? lessonToKeepSelected)
    {
        var selected = SelectedLesson ?? lessonToKeepSelected;
        var orderedLessons = Lessons
            .OrderBy(lesson => lesson.TryGetStartsAt(out var startsAt) ? startsAt : DateTimeOffset.MaxValue)
            .ThenBy(lesson => lesson.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        for (var targetIndex = 0; targetIndex < orderedLessons.Length; targetIndex++)
        {
            var currentIndex = Lessons.IndexOf(orderedLessons[targetIndex]);
            if (currentIndex >= 0 && currentIndex != targetIndex)
            {
                Lessons.Move(currentIndex, targetIndex);
            }
        }

        SelectedLesson = selected is not null && Lessons.Contains(selected)
            ? selected
            : null;
    }

    private bool CanNavigateStudents() =>
        !IsBusy && studentNavigationEntries.Length > 1;

    private bool CanScheduleLessons() => !IsBusy && isCalendarAvailable && CurrentStudentShortName is not null;

    private bool CanStartWriteInvoice() =>
        !HasPendingInvoice && CurrentStudentShortName is not null && (!IsBusy || IsActivationRefreshRunning());

    private bool IsActivationRefreshRunning() =>
        activationRefreshTask is { IsCompleted: false };

    private async Task WaitForActivationRefreshAsync()
    {
        var refreshTask = activationRefreshTask;
        if (refreshTask is { IsCompleted: false })
        {
            await refreshTask;
        }
    }

    private void RaiseCommandAvailabilityChanged()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        PreviousMonthCommand.RaiseCanExecuteChanged();
        NextMonthCommand.RaiseCanExecuteChanged();
        PreviousStudentCommand.RaiseCanExecuteChanged();
        NextStudentCommand.RaiseCanExecuteChanged();
        SelectStudentCommand.RaiseCanExecuteChanged();
        ToggleInvoicePaymentStatusCommand.RaiseCanExecuteChanged();
        OpenCalendarCommand.RaiseCanExecuteChanged();
        WriteInvoiceCommand.RaiseCanExecuteChanged();
        ResumeInvoiceCommand.RaiseCanExecuteChanged();
        OpenInvoiceCommand.RaiseCanExecuteChanged();
        OpenInvoicePdfCommand.RaiseCanExecuteChanged();
        OpenManageStudentsCommand.RaiseCanExecuteChanged();
        OpenStudentDetailsCommand.RaiseCanExecuteChanged();
        OpenSettingsCommand.RaiseCanExecuteChanged();
        OpenManagePlansCommand.RaiseCanExecuteChanged();
        OpenEconomyOverviewCommand.RaiseCanExecuteChanged();
        OpenAboutCommand.RaiseCanExecuteChanged();
        AddLessonCommand.RaiseCanExecuteChanged();
        PopulateCalendarCommand.RaiseCanExecuteChanged();
        SaveLessonCommand.RaiseCanExecuteChanged();
        DeleteLessonCommand.RaiseCanExecuteChanged();
        ToggleLessonStateCommand.RaiseCanExecuteChanged();
    }
}
