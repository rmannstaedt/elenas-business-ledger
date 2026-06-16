using ElenaSmallBusinessLedger.App.Services;
using ElenaSmallBusinessLedger.App.ViewModels;
using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Invoices;
using ElenaSmallBusinessLedger.Core.Lessons;
using ElenaSmallBusinessLedger.Core.UseCases.Startup;
using AppMainWindow = ElenaSmallBusinessLedger.App.MainWindow;

namespace ElenaSmallBusinessLedger.Tests.App.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void MainWindowInitializesWithDashboardSurface()
    {
        RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel();
            var window = new AppMainWindow(viewModel);

            Assert.Equal("Elena's Small Business Ledger", window.Title);
            Assert.Same(viewModel, window.DataContext);
            Assert.NotNull(window.Content);

            window.Close();
        });
    }

    [Fact]
    public async Task LoadPopulatesCurrentStudentLessonsAndInvoices()
    {
        var dashboard = new FakeDashboardService();
        var viewModel = CreateViewModel(dashboard: dashboard);

        await viewModel.LoadAsync();

        Assert.Equal("Anna", viewModel.CurrentStudentShortName);
        Assert.Single(viewModel.Lessons);
        Assert.Equal("16-06-2026 15:00", viewModel.Lessons[0].StartsAtText);
        Assert.Equal("Completed", viewModel.Lessons[0].StateText);
        Assert.Equal("Yes", viewModel.Lessons[0].InvoicedText);
        Assert.Equal("Outstanding", viewModel.Lessons[0].PaymentStatusText);
        Assert.Single(viewModel.Invoices);
        Assert.Equal("000001", viewModel.Invoices[0].InvoiceNumberText);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task MonthNavigationRefreshesDashboardForAdjacentMonth()
    {
        var dashboard = new FakeDashboardService();
        var viewModel = CreateViewModel(dashboard: dashboard);
        await viewModel.LoadAsync();

        await viewModel.NextMonthCommand.ExecuteAsync();

        Assert.Equal(new DateOnly(2026, 7, 1), dashboard.LastMonth);

        await viewModel.PreviousMonthCommand.ExecuteAsync();

        Assert.Equal(new DateOnly(2026, 6, 1), dashboard.LastMonth);
    }

    [Fact]
    public async Task StudentNavigationUsesDashboardStudentList()
    {
        var dashboard = new FakeDashboardService { StudentShortNames = ["Anna", "Bo"] };
        var viewModel = CreateViewModel(dashboard: dashboard);
        await viewModel.LoadAsync();

        await viewModel.NextStudentCommand.ExecuteAsync();

        Assert.Equal("Bo", dashboard.LastSelectedStudentShortName);
        Assert.Equal("Bo", viewModel.CurrentStudentShortName);
    }

    [Fact]
    public async Task RefreshOnActivationIsDebounced()
    {
        var clock = new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));
        var dashboard = new FakeDashboardService();
        var viewModel = CreateViewModel(dashboard: dashboard, clock: clock);
        await viewModel.LoadAsync();

        await viewModel.RefreshAfterActivationAsync();
        clock.Now = clock.Now.AddSeconds(4);
        await viewModel.RefreshAfterActivationAsync();
        clock.Now = clock.Now.AddSeconds(6);
        await viewModel.RefreshAfterActivationAsync();

        Assert.Equal(2, dashboard.LoadCount);
    }

    [Fact]
    public async Task ManualRefreshShowsCalendarErrorWithoutClearingExistingRows()
    {
        var dashboard = new FakeDashboardService();
        var viewModel = CreateViewModel(dashboard: dashboard);
        await viewModel.LoadAsync();
        dashboard.NextResult = OperationResult<StartupDashboardResult>.Failure(
            new ValidationError("Calendar.RefreshFailed", "Could not refresh Google Calendar."));

        await viewModel.RefreshCommand.ExecuteAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal("Could not refresh Google Calendar.", viewModel.ErrorMessage);
        Assert.Single(viewModel.Lessons);
    }

    [Fact]
    public async Task OutstandingInvoiceTogglesToPaidWithoutConfirmation()
    {
        var payments = new FakePaymentService();
        var viewModel = CreateViewModel(payments: payments);
        await viewModel.LoadAsync();

        await viewModel.ToggleInvoicePaymentStatusCommand.ExecuteAsync(viewModel.Invoices[0]);

        Assert.Equal(new InvoiceNumber(1), payments.LastInvoiceNumber);
        Assert.False(payments.LastConfirmMarkOutstanding);
        Assert.Equal("Paid", viewModel.Invoices[0].PaymentStatusText);
    }

    [Fact]
    public async Task PaidInvoiceRequiresConfirmationBeforeMarkingOutstanding()
    {
        var payments = new FakePaymentService();
        var confirmation = new FakeConfirmationService { NextAnswer = true };
        var viewModel = CreateViewModel(payments: payments, confirmation: confirmation);
        await viewModel.LoadAsync();
        await viewModel.ToggleInvoicePaymentStatusCommand.ExecuteAsync(viewModel.Invoices[0]);

        await viewModel.ToggleInvoicePaymentStatusCommand.ExecuteAsync(viewModel.Invoices[0]);

        Assert.True(confirmation.WasAsked);
        Assert.True(payments.LastConfirmMarkOutstanding);
        Assert.Equal("Outstanding", viewModel.Invoices[0].PaymentStatusText);
    }

    [Fact]
    public async Task OpenCalendarCommandUsesExternalBrowserService()
    {
        var calendar = new FakeCalendarLauncher();
        var viewModel = CreateViewModel(calendar: calendar);

        await viewModel.OpenCalendarCommand.ExecuteAsync();

        Assert.True(calendar.WasOpened);
    }

    [Fact]
    public async Task AddStudentRefreshesDashboardAfterDialogSave()
    {
        var dashboard = new FakeDashboardService();
        var studentEditor = new FakeStudentEditorLauncher { NextResult = true };
        var viewModel = new MainWindowViewModel(
            dashboard,
            new FakePaymentService(),
            new FakeConfirmationService(),
            new FakeCalendarLauncher(),
            studentEditor,
            new FakeLessonEditorLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero)));
        await viewModel.LoadAsync();

        await viewModel.AddStudentCommand.ExecuteAsync();

        Assert.True(studentEditor.AddWasShown);
        Assert.Equal(2, dashboard.LoadCount);
    }

    [Fact]
    public async Task InvoicedLessonEditIsBlockedWithoutOpeningDialog()
    {
        var lessonEditor = new FakeLessonEditorLauncher();
        var viewModel = new MainWindowViewModel(
            new FakeDashboardService(),
            new FakePaymentService(),
            new FakeConfirmationService(),
            new FakeCalendarLauncher(),
            new FakeStudentEditorLauncher(),
            lessonEditor,
            new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero)));
        await viewModel.LoadAsync();

        await viewModel.EditLessonCommand.ExecuteAsync(viewModel.Lessons[0]);

        Assert.False(lessonEditor.EditWasShown);
        Assert.True(viewModel.HasError);
    }

    private static MainWindowViewModel CreateViewModel(
        FakeDashboardService? dashboard = null,
        FakePaymentService? payments = null,
        FakeConfirmationService? confirmation = null,
        FakeCalendarLauncher? calendar = null,
        FakeUiClock? clock = null) =>
        new(
            dashboard ?? new FakeDashboardService(),
            payments ?? new FakePaymentService(),
            confirmation ?? new FakeConfirmationService(),
            calendar ?? new FakeCalendarLauncher(),
            new FakeStudentEditorLauncher(),
            new FakeLessonEditorLauncher(),
            clock ?? new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero)));

    private static void RunOnStaThread(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw exception;
        }
    }

    private sealed class FakeDashboardService : IStartupDashboardService
    {
        public int LoadCount { get; private set; }

        public DateOnly? LastMonth { get; private set; }

        public string? LastSelectedStudentShortName { get; private set; }

        public IReadOnlyList<string> StudentShortNames { get; init; } = ["Anna"];

        public OperationResult<StartupDashboardResult>? NextResult { get; set; }

        public Task<OperationResult<StartupDashboardResult>> LoadAsync(
            DateOnly month,
            string? selectedStudentShortName,
            CancellationToken cancellationToken)
        {
            LoadCount++;
            LastMonth = month;
            LastSelectedStudentShortName = selectedStudentShortName;
            var currentStudent = selectedStudentShortName ?? "Anna";

            var result = NextResult ?? OperationResult<StartupDashboardResult>.Success(
                new StartupDashboardResult(
                    currentStudent,
                    StudentShortNames,
                    [
                        new DashboardLesson(
                            "lesson-1",
                            currentStudent,
                            new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.FromHours(2)),
                            LessonState.Completed,
                            IsInvoiced: true,
                            PaymentStatus.Outstanding)
                    ],
                    [
                        new IssuedInvoiceSummary(
                            new InvoiceNumber(1),
                            currentStudent,
                            new DateOnly(2026, 6, 16),
                            PaymentStatus.Outstanding,
                            "C:\\Invoices\\000001.pdf")
                    ]));

            NextResult = null;
            return Task.FromResult(result);
        }
    }

    private sealed class FakePaymentService : IInvoicePaymentStatusService
    {
        public InvoiceNumber LastInvoiceNumber { get; private set; }

        public bool LastConfirmMarkOutstanding { get; private set; }

        public Task<OperationResult<PaymentStatus>> ToggleAsync(
            InvoiceNumber invoiceNumber,
            bool confirmMarkOutstanding,
            CancellationToken cancellationToken)
        {
            LastInvoiceNumber = invoiceNumber;
            LastConfirmMarkOutstanding = confirmMarkOutstanding;
            var next = confirmMarkOutstanding ? PaymentStatus.Outstanding : PaymentStatus.Paid;
            return Task.FromResult(OperationResult<PaymentStatus>.Success(next));
        }
    }

    private sealed class FakeConfirmationService : IConfirmationService
    {
        public bool NextAnswer { get; init; } = true;

        public bool WasAsked { get; private set; }

        public Task<bool> ConfirmMarkInvoiceOutstandingAsync(InvoiceDisplayModel invoice, CancellationToken cancellationToken)
        {
            WasAsked = true;
            return Task.FromResult(NextAnswer);
        }
    }

    private sealed class FakeCalendarLauncher : ICalendarLauncher
    {
        public bool WasOpened { get; private set; }

        public Task OpenAsync(CancellationToken cancellationToken)
        {
            WasOpened = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStudentEditorLauncher : IStudentEditorLauncher
    {
        public bool NextResult { get; init; }

        public bool AddWasShown { get; private set; }

        public Task<bool> ShowAddStudentAsync(CancellationToken cancellationToken)
        {
            AddWasShown = true;
            return Task.FromResult(NextResult);
        }

        public Task<bool> ShowEditStudentAsync(string studentShortName, CancellationToken cancellationToken) =>
            Task.FromResult(NextResult);
    }

    private sealed class FakeLessonEditorLauncher : ILessonEditorLauncher
    {
        public bool EditWasShown { get; private set; }

        public Task<bool> ShowAddLessonAsync(
            string studentShortName,
            DateTimeOffset startsAt,
            DateOnly refreshMonth,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> ShowEditLessonAsync(
            LessonDisplayModel lesson,
            DateOnly refreshMonth,
            CancellationToken cancellationToken)
        {
            EditWasShown = true;
            return Task.FromResult(false);
        }
    }

    private sealed class FakeUiClock : IUiClock
    {
        public FakeUiClock(DateTimeOffset now)
        {
            Now = now;
        }

        public DateTimeOffset Now { get; set; }
    }
}
