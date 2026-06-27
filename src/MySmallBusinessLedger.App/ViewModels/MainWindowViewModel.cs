using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using MySmallBusinessLedger.App.Commands;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.UseCases.Lessons;
using MySmallBusinessLedger.Core.UseCases.Startup;

namespace MySmallBusinessLedger.App.ViewModels;

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
    private readonly IUiClock clock;
    private readonly TimeZoneInfo lessonTimeZone;
    private readonly Dictionary<LessonDisplayModel, int> autoSaveDraftVersions = [];
    private readonly HashSet<LessonDisplayModel> savingDraftLessons = [];
    private readonly HashSet<LessonDisplayModel> savingPlanLessons = [];
    private readonly Dictionary<LessonDisplayModel, int> pendingPlanCatalogIds = [];
    private readonly HashSet<LessonDisplayModel> acceptingPlanChangeLessons = [];
    private long studentOverviewSelectionVersion;
    private DateTimeOffset? lastRefreshAt;
    private string? currentStudentShortName;
    private string[] studentShortNames = [];
    private DashboardStudentNavigationEntry[] studentNavigationEntries = [];
    private int? selectedStudentNavigationIndex;
    private bool suppressNavigationSelectionOnce;
    private DateOnly displayMonth;
    private bool currentStudentHasSchedule;
    private Task? activationRefreshTask;
    private bool isBusy;
    private int activationRefreshSuspendCount;
    private LessonDisplayModel? selectedLesson;
    private InvoiceDisplayModel? selectedInvoice;
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
        this.clock = clock;
        this.lessonTimeZone = lessonTimeZone ?? TimeZoneInfo.Local;
        displayMonth = new DateOnly(clock.Now.Year, clock.Now.Month, 1);

        RefreshCommand = new AsyncRelayCommand(() => RefreshAsync(), () => !IsBusy);
        PreviousMonthCommand = new AsyncRelayCommand(() => ChangeMonthAsync(-1), () => !IsBusy);
        NextMonthCommand = new AsyncRelayCommand(() => ChangeMonthAsync(1), () => !IsBusy);
        PreviousStudentCommand = new AsyncRelayCommand(() => SelectAdjacentStudentAsync(-1), CanNavigateStudents);
        NextStudentCommand = new AsyncRelayCommand(() => SelectAdjacentStudentAsync(1), CanNavigateStudents);
        SelectStudentCommand = new AsyncRelayCommand(
            parameter => SelectStudentFromDropdownAsync((string)parameter!),
            parameter =>
                parameter is string shortName &&
                !IsBusy &&
                !StringComparer.OrdinalIgnoreCase.Equals(shortName, CurrentStudentShortName));
        ToggleInvoicePaymentStatusCommand = new AsyncRelayCommand(
            parameter => ToggleInvoicePaymentStatusAsync((InvoiceDisplayModel)parameter!),
            parameter => parameter is InvoiceDisplayModel && !IsBusy);
        OpenCalendarCommand = new AsyncRelayCommand(
            () => calendarLauncher.OpenAsync(CancellationToken.None),
            () => !IsBusy);
        WriteInvoiceCommand = new AsyncRelayCommand(WriteInvoiceAsync, CanStartWriteInvoice);
        OpenInvoiceCommand = new AsyncRelayCommand(
            parameter => OpenInvoiceAsync((InvoiceDisplayModel)parameter!),
            parameter => parameter is InvoiceDisplayModel && !IsBusy);
        OpenInvoicePdfCommand = new AsyncRelayCommand(
            parameter => OpenInvoicePdfAsync((InvoiceDisplayModel)parameter!),
            parameter => parameter is InvoiceDisplayModel && !IsBusy);
        OpenManageStudentsCommand = new AsyncRelayCommand(OpenManageStudentsAsync, () => !IsBusy);
        OpenSettingsCommand = new AsyncRelayCommand(OpenSettingsAsync, () => !IsBusy);
        OpenManagePlansCommand = new AsyncRelayCommand(OpenManagePlansAsync, () => !IsBusy);
        AddLessonCommand = new AsyncRelayCommand(AddLessonAsync, () => !IsBusy && CurrentStudentShortName is not null);
        PopulateCalendarCommand = new AsyncRelayCommand(PopulateCalendarAsync, CanPopulateCalendar);
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

    public string DisplayMonthText => DisplayMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

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

    public AsyncRelayCommand RefreshCommand { get; }

    public AsyncRelayCommand PreviousMonthCommand { get; }

    public AsyncRelayCommand NextMonthCommand { get; }

    public AsyncRelayCommand PreviousStudentCommand { get; }

    public AsyncRelayCommand NextStudentCommand { get; }

    public AsyncRelayCommand SelectStudentCommand { get; }

    public AsyncRelayCommand ToggleInvoicePaymentStatusCommand { get; }

    public AsyncRelayCommand OpenCalendarCommand { get; }

    public AsyncRelayCommand WriteInvoiceCommand { get; }

    public AsyncRelayCommand OpenInvoiceCommand { get; }

    public AsyncRelayCommand OpenInvoicePdfCommand { get; }

    public AsyncRelayCommand OpenManageStudentsCommand { get; }

    public AsyncRelayCommand OpenSettingsCommand { get; }

    public AsyncRelayCommand OpenManagePlansCommand { get; }

    public AsyncRelayCommand AddLessonCommand { get; }

    public AsyncRelayCommand PopulateCalendarCommand { get; }

    public AsyncRelayCommand SaveLessonCommand { get; }

    public AsyncRelayCommand DeleteLessonCommand { get; }

    public AsyncRelayCommand ToggleLessonStateCommand { get; }

    public AsyncRelayCommand DismissErrorCommand { get; }

    public Task LoadAsync() => RefreshAsync();

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

    private async Task RefreshAsync(Func<bool>? shouldApply = null)
    {
        IsBusy = true;
        try
        {
            var result = await dashboardService.LoadAsync(DisplayMonth, CurrentStudentShortName, CancellationToken.None);
            if (!result.Succeeded)
            {
                ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Could not refresh dashboard.";
                return;
            }

            if (shouldApply is not null && !shouldApply())
            {
                return;
            }

            ApplyDashboard(result.Value!);
            ErrorMessage = null;
            lastRefreshAt = clock.Now;
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
            StringComparer.OrdinalIgnoreCase.Equals(
                studentNavigationEntries[index].StudentShortName,
                CurrentStudentShortName))
        {
            return index;
        }

        if (SelectedLesson is { } selectedLesson &&
            selectedLesson.TryGetStartsAt(out var selectedStartsAt))
        {
            var selectedLessonIndex = Array.FindIndex(
                studentNavigationEntries,
                entry =>
                    StringComparer.OrdinalIgnoreCase.Equals(entry.StudentShortName, selectedLesson.StudentShortName) &&
                    entry.StartsAt == selectedStartsAt);
            if (selectedLessonIndex >= 0)
            {
                return selectedLessonIndex;
            }
        }

        return Array.FindIndex(
            studentNavigationEntries,
            entry => StringComparer.OrdinalIgnoreCase.Equals(entry.StudentShortName, CurrentStudentShortName));
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
        CurrentStudentShortName = nextEntry.StudentShortName;
        DisplayMonth = new DateOnly(nextEntry.StartsAt.Year, nextEntry.StartsAt.Month, 1);
        await RefreshAsync();
    }

    private async Task SelectStudentFromDropdownAsync(string studentShortName)
    {
        selectedStudentNavigationIndex = null;
        suppressNavigationSelectionOnce = true;
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
        var selectedStudentShortName = await studentOverviewLauncher.ShowStudentOverviewAsync(
            CurrentStudentShortName,
            ApplyStudentOverviewSelectionAsync,
            CancellationToken.None);
        await ApplyStudentOverviewSelectionAsync(selectedStudentShortName, CancellationToken.None);
    }

    private async Task ApplyStudentOverviewSelectionAsync(string? selectedStudentShortName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(selectedStudentShortName))
        {
            return;
        }

        CurrentStudentShortName = selectedStudentShortName;
        var selectionVersion = Interlocked.Increment(ref studentOverviewSelectionVersion);
        await RefreshAsync(() => selectionVersion == Interlocked.Read(ref studentOverviewSelectionVersion));
    }

    private async Task OpenSettingsAsync()
    {
        var saved = await settingsLauncher.ShowSettingsAsync(CancellationToken.None);
        if (saved)
        {
            await RefreshAsync();
        }
    }

    private Task OpenManagePlansAsync() =>
        planCatalogLauncher.ShowPlanCatalogAsync(CancellationToken.None);

    private async Task WriteInvoiceAsync()
    {
        await WaitForActivationRefreshAsync();

        if (IsBusy)
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

            var issued = await invoiceWorkflowLauncher.ShowWriteInvoiceAsync(
                DisplayMonth,
                CurrentStudentShortName,
                CancellationToken.None);
            if (issued)
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

    private Task AddLessonAsync()
    {
        if (CurrentStudentShortName is null)
        {
            return Task.CompletedTask;
        }

        if (PlanOptions.Count == 0)
        {
            ErrorMessage = "Create or publish an active plan first.";
            return Task.CompletedTask;
        }

        DateTime? defaultDate = DisplayMonth == CurrentMonth()
            ? null
            : DisplayMonth.ToDateTime(TimeOnly.MinValue);
        var draft = LessonDisplayModel.CreateDraft(CurrentStudentShortName, lessonTimeZone, defaultDate);
        AddLessonRow(draft);
        SelectedLesson = draft;
        RaiseCommandAvailabilityChanged();
        return Task.CompletedTask;
    }

    private async Task PopulateCalendarAsync()
    {
        if (CurrentStudentShortName is null)
        {
            return;
        }

        var result = await lessonEditorService.PopulateCalendarAsync(
            CurrentStudentShortName,
            DisplayMonth,
            DateOnly.FromDateTime(clock.Now.DateTime),
            CancellationToken.None);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Google Calendar could not be populated.";
            return;
        }

        ErrorMessage = null;
        await RefreshAsync();
    }

    public void ClearError()
    {
        ErrorMessage = null;
    }

    private async Task SaveLessonAsync(LessonDisplayModel lesson)
    {
        await SaveLessonAsync(lesson, initiatedByAutoSave: false);
    }

    private async Task SaveLessonAsync(LessonDisplayModel lesson, bool initiatedByAutoSave)
    {
        if (!lesson.CanEdit)
        {
            ErrorMessage = lesson.State == LessonState.Completed
                ? "Completed lessons cannot be edited."
                : "Invoiced lessons cannot be edited.";
            return;
        }

        if (!initiatedByAutoSave && lesson.IsDraft && savingDraftLessons.Contains(lesson))
        {
            return;
        }

        if (!initiatedByAutoSave && lesson.IsDraft)
        {
            autoSaveDraftVersions[lesson] = autoSaveDraftVersions.TryGetValue(lesson, out var version)
                ? version + 1
                : 1;
        }

        if (lesson.IsDraft && lesson.PlanCatalogId <= 0)
        {
            ErrorMessage = null;
            return;
        }

        if (!lesson.TryGetStartsAt(out var startsAt, out var hasKnownStartTime))
        {
            ErrorMessage = null;
            return;
        }

        if (lesson.IsDraft)
        {
            await SaveDraftLessonAsync(lesson, startsAt, hasKnownStartTime);
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

    private async Task SaveDraftLessonAsync(LessonDisplayModel lesson, DateTimeOffset startsAt, bool hasKnownStartTime)
    {
        var requestedPlanCatalogId = lesson.PlanCatalogId;
        var result = await lessonEditorService.AddAsync(
            new LessonAddRequest(lesson.StudentShortName, startsAt, requestedPlanCatalogId, hasKnownStartTime),
            DisplayMonth,
            CancellationToken.None);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Google Calendar lesson creation failed.";
            return;
        }

        var createdLesson = result.Value!
            .FirstOrDefault(created =>
                StringComparer.OrdinalIgnoreCase.Equals(created.StudentShortName, lesson.StudentShortName) &&
                created.StartsAt == startsAt &&
                created.HasKnownStartTime == hasKnownStartTime);
        if (createdLesson is not null)
        {
            lesson.AcceptCreatedLesson(createdLesson);
        }
        else
        {
            lesson.AcceptSubmittedDraft();
        }

        ErrorMessage = null;
        RaiseCommandAvailabilityChanged();
        await RefreshAsync();
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
        if (!result.Succeeded)
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Google Calendar lesson deletion failed.";
            return;
        }

        ErrorMessage = null;
        RemoveLessonRow(lesson);
        if (ReferenceEquals(SelectedLesson, lesson))
        {
            SelectedLesson = null;
        }

        RaiseCommandAvailabilityChanged();
    }

    private async Task ToggleLessonStateAsync(LessonDisplayModel lesson)
    {
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

    private void ApplyDashboard(StartupDashboardResult dashboard)
    {
        var selectedLesson = SelectedLesson;
        var selectedLessonId = selectedLesson?.Id;
        var selectedInvoiceNumber = SelectedInvoice?.InvoiceNumber;
        CurrentStudentShortName = dashboard.CurrentStudentShortName;
        currentStudentHasSchedule = dashboard.CurrentStudentHasSchedule;
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
                selectedStudentNavigationIndex,
                dashboard.CurrentStudentShortName,
                selectedLesson);

        var draftLessons = Lessons
            .Where(lesson => lesson.IsDraft)
            .ToArray();
        ClearLessonRows();
        foreach (var lesson in dashboard.Lessons
                     .Where(lesson => IsInDisplayMonth(lesson.StartsAt))
                     .OrderBy(lesson => lesson.StartsAt))
        {
            AddLessonRow(new LessonDisplayModel(lesson, lessonTimeZone));
        }

        foreach (var draft in draftLessons.Where(lesson => lesson.IsDraft && IsVisibleDraftForDisplayMonth(lesson)))
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
        if (selectedLesson is { IsDraft: true } && Lessons.Contains(selectedLesson))
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

        Invoices.Clear();
        foreach (var invoice in dashboard.Invoices.OrderByDescending(invoice => invoice.InvoiceDate))
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
            StringComparer.OrdinalIgnoreCase.Equals(lesson.StudentShortName, selectedLesson.StudentShortName) &&
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

        return Lessons.Count == 0 ? SelectedInvoice : null;
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
        int? preferredIndex,
        string? currentStudent,
        LessonDisplayModel? selectedLesson)
    {
        if (studentNavigationEntries.Length == 0)
        {
            return null;
        }

        if (preferredIndex is { } index &&
            index >= 0 &&
            index < studentNavigationEntries.Length &&
            StringComparer.OrdinalIgnoreCase.Equals(
                studentNavigationEntries[index].StudentShortName,
                currentStudent))
        {
            return index;
        }

        if (selectedLesson is not null &&
            selectedLesson.TryGetStartsAt(out var selectedStartsAt))
        {
            var matchingLessonIndex = Array.FindIndex(
                studentNavigationEntries,
                entry =>
                    StringComparer.OrdinalIgnoreCase.Equals(entry.StudentShortName, selectedLesson.StudentShortName) &&
                    entry.StartsAt == selectedStartsAt);
            if (matchingLessonIndex >= 0)
            {
                return matchingLessonIndex;
            }
        }

        var matchingCalendarStudentIndex = Array.FindIndex(
            studentNavigationEntries,
            entry =>
                entry.HasCalendarLesson &&
                StringComparer.OrdinalIgnoreCase.Equals(entry.StudentShortName, currentStudent));
        if (matchingCalendarStudentIndex >= 0)
        {
            return matchingCalendarStudentIndex;
        }

        var matchingStudentIndex = Array.FindIndex(
            studentNavigationEntries,
            entry => StringComparer.OrdinalIgnoreCase.Equals(entry.StudentShortName, currentStudent));
        return matchingStudentIndex >= 0 ? matchingStudentIndex : null;
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
        StringComparer.OrdinalIgnoreCase.Equals(entry.StudentShortName, lesson.StudentShortName) &&
        lesson.TryGetStartsAt(out var startsAt) &&
        startsAt == entry.StartsAt;

    private void AddLessonRow(LessonDisplayModel lesson)
    {
        lesson.PropertyChanged += OnLessonPropertyChanged;
        Lessons.Add(lesson);
    }

    private bool IsInDisplayMonth(DateTimeOffset startsAt) =>
        startsAt.Year == DisplayMonth.Year &&
        startsAt.Month == DisplayMonth.Month;

    private bool IsVisibleDraftForDisplayMonth(LessonDisplayModel lesson) =>
        lesson.StartsOnDate is null ||
        (lesson.StartsOnDate.Value.Year == DisplayMonth.Year &&
         lesson.StartsOnDate.Value.Month == DisplayMonth.Month);

    private void RemoveLessonRow(LessonDisplayModel lesson)
    {
        lesson.PropertyChanged -= OnLessonPropertyChanged;
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
                TryAutoSaveCompleteDraftLesson(lesson);
                return;
            }

            TrySaveLessonPlanChange(lesson);
            return;
        }

        SortLessonsPreservingSelection(lesson);
        TryAutoSaveCompleteDraftLesson(lesson);
    }

    private void TrySaveLessonPlanChange(LessonDisplayModel lesson)
    {
        if (lesson.IsDraft ||
            !lesson.CanEditPlan ||
            lesson.LocalLessonId <= 0 ||
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
                var result = await lessonEditorService.UpdatePlanAsync(
                    lesson.LocalLessonId,
                    requestedPlanCatalogId,
                    CancellationToken.None);
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

    private void TryAutoSaveCompleteDraftLesson(LessonDisplayModel lesson)
    {
        if (!lesson.IsDraft ||
            lesson.PlanCatalogId <= 0 ||
            !lesson.TryGetStartsAt(out _))
        {
            return;
        }

        var version = autoSaveDraftVersions.TryGetValue(lesson, out var currentVersion)
            ? currentVersion + 1
            : 1;
        autoSaveDraftVersions[lesson] = version;
        _ = AutoSaveCompleteDraftLessonAsync(lesson, version);
    }

    private async Task AutoSaveCompleteDraftLessonAsync(LessonDisplayModel lesson, int version)
    {
        await Task.Delay(250);
        if (!lesson.IsDraft ||
            !autoSaveDraftVersions.TryGetValue(lesson, out var latestVersion) ||
            latestVersion != version ||
            savingDraftLessons.Contains(lesson))
        {
            return;
        }

        savingDraftLessons.Add(lesson);
        try
        {
            await SaveLessonAsync(lesson, initiatedByAutoSave: true);
        }
        finally
        {
            savingDraftLessons.Remove(lesson);
            if (autoSaveDraftVersions.TryGetValue(lesson, out latestVersion) &&
                latestVersion == version)
            {
                autoSaveDraftVersions.Remove(lesson);
            }
        }
    }

    private bool CanNavigateStudents() =>
        !IsBusy && studentNavigationEntries.Length > 1;

    private bool CanPopulateCalendar() =>
        !IsBusy &&
        CurrentStudentShortName is not null;

    private bool CanStartWriteInvoice() =>
        !IsBusy || IsActivationRefreshRunning();

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

    private DateOnly CurrentMonth()
    {
        var now = clock.Now;
        return new DateOnly(now.Year, now.Month, 1);
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
        OpenInvoiceCommand.RaiseCanExecuteChanged();
        OpenInvoicePdfCommand.RaiseCanExecuteChanged();
        OpenManageStudentsCommand.RaiseCanExecuteChanged();
        OpenSettingsCommand.RaiseCanExecuteChanged();
        OpenManagePlansCommand.RaiseCanExecuteChanged();
        AddLessonCommand.RaiseCanExecuteChanged();
        PopulateCalendarCommand.RaiseCanExecuteChanged();
        SaveLessonCommand.RaiseCanExecuteChanged();
        DeleteLessonCommand.RaiseCanExecuteChanged();
        ToggleLessonStateCommand.RaiseCanExecuteChanged();
    }
}
