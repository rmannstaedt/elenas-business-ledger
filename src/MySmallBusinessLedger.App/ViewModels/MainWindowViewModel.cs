using System.Collections.ObjectModel;
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
    private readonly ISettingsLauncher settingsLauncher;
    private readonly IPlanCatalogLauncher planCatalogLauncher;
    private readonly IUiClock clock;
    private readonly TimeZoneInfo lessonTimeZone;
    private long studentOverviewSelectionVersion;
    private DateTimeOffset? lastRefreshAt;
    private string? currentStudentShortName;
    private string[] studentShortNames = [];
    private DateOnly displayMonth;
    private bool isBusy;
    private int activationRefreshSuspendCount;
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
        ToggleInvoicePaymentStatusCommand = new AsyncRelayCommand(
            parameter => ToggleInvoicePaymentStatusAsync((InvoiceDisplayModel)parameter!),
            parameter => parameter is InvoiceDisplayModel && !IsBusy);
        OpenCalendarCommand = new AsyncRelayCommand(
            () => calendarLauncher.OpenAsync(CancellationToken.None),
            () => !IsBusy);
        WriteInvoiceCommand = new AsyncRelayCommand(WriteInvoiceAsync, () => !IsBusy);
        OpenInvoiceCommand = new AsyncRelayCommand(
            parameter => OpenInvoiceAsync((InvoiceDisplayModel)parameter!),
            parameter => parameter is InvoiceDisplayModel && !IsBusy);
        OpenManageStudentsCommand = new AsyncRelayCommand(OpenManageStudentsAsync, () => !IsBusy);
        OpenSettingsCommand = new AsyncRelayCommand(OpenSettingsAsync, () => !IsBusy);
        OpenManagePlansCommand = new AsyncRelayCommand(OpenManagePlansAsync, () => !IsBusy);
        AddLessonCommand = new AsyncRelayCommand(AddLessonAsync, () => !IsBusy && CurrentStudentShortName is not null);
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

    public ObservableCollection<InvoiceDisplayModel> Invoices { get; } = [];

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

    public AsyncRelayCommand ToggleInvoicePaymentStatusCommand { get; }

    public AsyncRelayCommand OpenCalendarCommand { get; }

    public AsyncRelayCommand WriteInvoiceCommand { get; }

    public AsyncRelayCommand OpenInvoiceCommand { get; }

    public AsyncRelayCommand OpenManageStudentsCommand { get; }

    public AsyncRelayCommand OpenSettingsCommand { get; }

    public AsyncRelayCommand OpenManagePlansCommand { get; }

    public AsyncRelayCommand AddLessonCommand { get; }

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

        await RefreshAsync();
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

    private async Task SelectAdjacentStudentAsync(int delta)
    {
        if (studentShortNames.Length == 0)
        {
            return;
        }

        var currentIndex = Array.FindIndex(
            studentShortNames,
            student => StringComparer.OrdinalIgnoreCase.Equals(student, CurrentStudentShortName));
        var nextIndex = currentIndex < 0
            ? 0
            : (currentIndex + delta + studentShortNames.Length) % studentShortNames.Length;

        CurrentStudentShortName = studentShortNames[nextIndex];
        await RefreshAsync();
    }

    private async Task ToggleInvoicePaymentStatusAsync(InvoiceDisplayModel invoice)
    {
        var confirmMarkOutstanding = false;
        if (invoice.PaymentStatus == PaymentStatus.Paid)
        {
            confirmMarkOutstanding = await RunWithActivationRefreshSuspendedAsync(
                () => confirmationService.ConfirmMarkInvoiceOutstandingAsync(invoice, CancellationToken.None));
            if (!confirmMarkOutstanding)
            {
                return;
            }
        }

        var result = await paymentStatusService.ToggleAsync(invoice.InvoiceNumber, confirmMarkOutstanding, CancellationToken.None);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Could not update payment status.";
            return;
        }

        invoice.PaymentStatus = result.Value;
        ErrorMessage = null;
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
        var issued = await invoiceWorkflowLauncher.ShowWriteInvoiceAsync(DisplayMonth, CancellationToken.None);
        if (issued)
        {
            await RefreshAsync();
        }
    }

    private Task OpenInvoiceAsync(InvoiceDisplayModel invoice) =>
        invoiceWorkflowLauncher.ShowInvoiceViewerAsync(invoice.InvoiceNumber, CancellationToken.None);

    private Task AddLessonAsync()
    {
        if (CurrentStudentShortName is null)
        {
            return Task.CompletedTask;
        }

        Lessons.Add(LessonDisplayModel.CreateDraft(CurrentStudentShortName, lessonTimeZone));
        RaiseCommandAvailabilityChanged();
        return Task.CompletedTask;
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

        if (!lesson.TryGetStartsAt(out var startsAt))
        {
            ErrorMessage = null;
            return;
        }

        if (lesson.IsDraft)
        {
            await SaveDraftLessonAsync(lesson, startsAt);
            return;
        }

        if (!lesson.HasDateTimeChange)
        {
            ErrorMessage = null;
            return;
        }

        var result = await lessonEditorService.EditAsync(
            new LessonEditRequest(lesson.Id, lesson.StudentShortName, startsAt),
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

    private async Task SaveDraftLessonAsync(LessonDisplayModel lesson, DateTimeOffset startsAt)
    {
        var result = await lessonEditorService.AddAsync(
            new LessonAddRequest(lesson.StudentShortName, startsAt),
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
                created.StartsAt == startsAt);
        if (createdLesson is not null)
        {
            lesson.AcceptCreatedLesson(createdLesson);
        }
        else
        {
            lesson.AcceptDateTimeChange();
        }

        ErrorMessage = null;
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
            Lessons.Remove(lesson);
            ErrorMessage = null;
            RaiseCommandAvailabilityChanged();
            return;
        }

        var confirmed = await RunWithActivationRefreshSuspendedAsync(
            () => confirmationService.ConfirmDeleteLessonAsync(lesson, CancellationToken.None));
        if (!confirmed)
        {
            return;
        }

        var result = await lessonEditorService.DeleteAsync(lesson.Id, DisplayMonth, CancellationToken.None);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? "Google Calendar lesson deletion failed.";
            return;
        }

        ErrorMessage = null;
        Lessons.Remove(lesson);
        RaiseCommandAvailabilityChanged();
    }

    private async Task ToggleLessonStateAsync(LessonDisplayModel lesson)
    {
        if (lesson.IsInvoiced)
        {
            ErrorMessage = "Invoiced lessons cannot be changed.";
            return;
        }

        var confirmReturnToPlanned = false;
        if (lesson.State == LessonState.Completed)
        {
            confirmReturnToPlanned = await RunWithActivationRefreshSuspendedAsync(
                () => confirmationService.ConfirmMarkLessonPlannedAsync(lesson, CancellationToken.None));
            if (!confirmReturnToPlanned)
            {
                return;
            }
        }

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
        CurrentStudentShortName = dashboard.CurrentStudentShortName;
        studentShortNames = dashboard.StudentShortNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Lessons.Clear();
        foreach (var lesson in dashboard.Lessons.OrderBy(lesson => lesson.StartsAt))
        {
            Lessons.Add(new LessonDisplayModel(lesson, lessonTimeZone));
        }

        Invoices.Clear();
        foreach (var invoice in dashboard.Invoices.OrderByDescending(invoice => invoice.InvoiceDate))
        {
            Invoices.Add(new InvoiceDisplayModel(invoice));
        }

        RaiseCommandAvailabilityChanged();
    }

    private bool CanNavigateStudents() =>
        !IsBusy && studentShortNames.Length > 1;

    private void RaiseCommandAvailabilityChanged()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        PreviousMonthCommand.RaiseCanExecuteChanged();
        NextMonthCommand.RaiseCanExecuteChanged();
        PreviousStudentCommand.RaiseCanExecuteChanged();
        NextStudentCommand.RaiseCanExecuteChanged();
        ToggleInvoicePaymentStatusCommand.RaiseCanExecuteChanged();
        OpenCalendarCommand.RaiseCanExecuteChanged();
        WriteInvoiceCommand.RaiseCanExecuteChanged();
        OpenManageStudentsCommand.RaiseCanExecuteChanged();
        OpenSettingsCommand.RaiseCanExecuteChanged();
        OpenManagePlansCommand.RaiseCanExecuteChanged();
        AddLessonCommand.RaiseCanExecuteChanged();
        SaveLessonCommand.RaiseCanExecuteChanged();
        DeleteLessonCommand.RaiseCanExecuteChanged();
        ToggleLessonStateCommand.RaiseCanExecuteChanged();
    }
}
