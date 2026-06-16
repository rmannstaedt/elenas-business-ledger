using System.Collections.ObjectModel;
using System.Globalization;
using ElenaSmallBusinessLedger.App.Commands;
using ElenaSmallBusinessLedger.App.Services;
using ElenaSmallBusinessLedger.Core.Invoices;
using ElenaSmallBusinessLedger.Core.UseCases.Startup;

namespace ElenaSmallBusinessLedger.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private static readonly TimeSpan ActivationRefreshMinimumInterval = TimeSpan.FromSeconds(10);
    private readonly IStartupDashboardService dashboardService;
    private readonly IInvoicePaymentStatusService paymentStatusService;
    private readonly IConfirmationService confirmationService;
    private readonly ICalendarLauncher calendarLauncher;
    private readonly IUiClock clock;
    private DateTimeOffset? lastRefreshAt;
    private string? currentStudentShortName;
    private string[] studentShortNames = [];
    private DateOnly displayMonth;
    private bool isBusy;
    private string? errorMessage;

    public MainWindowViewModel(
        IStartupDashboardService dashboardService,
        IInvoicePaymentStatusService paymentStatusService,
        IConfirmationService confirmationService,
        ICalendarLauncher calendarLauncher,
        IUiClock clock)
    {
        this.dashboardService = dashboardService;
        this.paymentStatusService = paymentStatusService;
        this.confirmationService = confirmationService;
        this.calendarLauncher = calendarLauncher;
        this.clock = clock;
        displayMonth = new DateOnly(clock.Now.Year, clock.Now.Month, 1);

        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
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
        WriteInvoiceCommand = new AsyncRelayCommand(() => Task.CompletedTask, () => !IsBusy);
        OpenInvoiceCommand = new AsyncRelayCommand(_ => Task.CompletedTask, parameter => parameter is InvoiceDisplayModel);
        AddStudentCommand = new AsyncRelayCommand(() => Task.CompletedTask, () => !IsBusy);
        EditStudentCommand = new AsyncRelayCommand(() => Task.CompletedTask, () => !IsBusy);
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

    public AsyncRelayCommand AddStudentCommand { get; }

    public AsyncRelayCommand EditStudentCommand { get; }

    public Task LoadAsync() => RefreshAsync();

    public async Task RefreshAfterActivationAsync()
    {
        if (lastRefreshAt is not null && clock.Now - lastRefreshAt.Value < ActivationRefreshMinimumInterval)
        {
            return;
        }

        await RefreshAsync();
    }

    private async Task RefreshAsync()
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
            confirmMarkOutstanding = await confirmationService.ConfirmMarkInvoiceOutstandingAsync(invoice, CancellationToken.None);
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
            Lessons.Add(new LessonDisplayModel(lesson));
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
        AddStudentCommand.RaiseCanExecuteChanged();
        EditStudentCommand.RaiseCanExecuteChanged();
    }
}
