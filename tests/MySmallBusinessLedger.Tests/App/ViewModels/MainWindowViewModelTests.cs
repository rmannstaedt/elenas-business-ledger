using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.UseCases.Startup;
using System.Windows;
using WpfMenuItem = System.Windows.Controls.MenuItem;
using AppMainWindow = MySmallBusinessLedger.App.MainWindow;

namespace MySmallBusinessLedger.Tests.App.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void MainWindowInitializesWithDashboardSurface()
    {
        RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel();
            var window = new AppMainWindow(viewModel);

            Assert.Equal("My Small Business Ledger", window.Title);
            Assert.Same(viewModel, window.DataContext);
            Assert.NotNull(window.Content);

            window.Close();
        });
    }

    [Fact]
    public void StudentMenuOnlyShowsManageStudents()
    {
        RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel();
            var window = new AppMainWindow(viewModel);

            var studentMenu = Assert.Single(FindChildren<WpfMenuItem>(window), item => item.Header?.ToString() == "_Student");
            var studentActions = studentMenu.Items
                .OfType<WpfMenuItem>()
                .Select(item => item.Header?.ToString())
                .Where(header => header is not null)
                .Cast<string>()
                .ToArray();

            Assert.Equal(["_Manage Students"], studentActions);

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
    public async Task WriteInvoiceLaunchesWorkflowAndRefreshesAfterIssue()
    {
        var dashboard = new FakeDashboardService();
        var invoices = new FakeInvoiceWorkflowLauncher { NextWriteResult = true };
        var viewModel = CreateViewModel(dashboard: dashboard, invoiceWorkflowLauncher: invoices);
        await viewModel.LoadAsync();

        await viewModel.WriteInvoiceCommand.ExecuteAsync();

        Assert.True(invoices.WriteWasShown);
        Assert.Equal(2, dashboard.LoadCount);
    }

    [Fact]
    public async Task SettingsRefreshesDashboardAfterSave()
    {
        var dashboard = new FakeDashboardService();
        var settings = new FakeSettingsLauncher { NextResult = true };
        var viewModel = CreateViewModel(dashboard: dashboard, settingsLauncher: settings);
        await viewModel.LoadAsync();

        await viewModel.OpenSettingsCommand.ExecuteAsync();

        Assert.True(settings.WasShown);
        Assert.Equal(2, dashboard.LoadCount);
    }

    [Fact]
    public async Task ManagePlansCommandOpensPlanCatalog()
    {
        var planCatalog = new FakePlanCatalogLauncher();
        var viewModel = CreateViewModel(planCatalogLauncher: planCatalog);

        await viewModel.OpenManagePlansCommand.ExecuteAsync();

        Assert.True(planCatalog.WasShown);
    }

    [Fact]
    public async Task ManageStudentsCommandRefreshesDashboardWithSelectedOverviewStudent()
    {
        var dashboard = new FakeDashboardService { StudentShortNames = ["Anna", "Bo"] };
        var studentOverview = new FakeStudentOverviewLauncher { SelectedStudentShortName = "Bo" };
        var viewModel = CreateViewModel(dashboard: dashboard, studentOverviewLauncher: studentOverview);
        await viewModel.LoadAsync();

        await viewModel.OpenManageStudentsCommand.ExecuteAsync();

        Assert.True(studentOverview.WasShown);
        Assert.Equal("Anna", studentOverview.InitialSelectedStudentShortName);
        Assert.Equal("Bo", dashboard.LastSelectedStudentShortName);
        Assert.Equal("Bo", viewModel.CurrentStudentShortName);
        Assert.Equal(2, dashboard.LoadCount);
    }

    [Fact]
    public async Task ManageStudentsSelectionImmediatelyRefreshesDashboard()
    {
        var dashboard = new FakeDashboardService { StudentShortNames = ["Anna", "Erik"] };
        var selectionRefreshWasSeenBeforeClose = false;
        var studentOverview = new FakeStudentOverviewLauncher
        {
            SelectionChangedStudentShortName = "Erik",
            OnAfterSelectionChanged = () =>
            {
                selectionRefreshWasSeenBeforeClose =
                    dashboard.LastSelectedStudentShortName == "Erik" &&
                    dashboard.LoadCount == 2;
            },
            SelectedStudentShortName = "Erik"
        };
        var viewModel = CreateViewModel(dashboard: dashboard, studentOverviewLauncher: studentOverview);
        await viewModel.LoadAsync();

        await viewModel.OpenManageStudentsCommand.ExecuteAsync();

        Assert.True(selectionRefreshWasSeenBeforeClose);
        Assert.Equal("Erik", viewModel.CurrentStudentShortName);
    }

    [Fact]
    public async Task OpenInvoiceLaunchesViewerForSelectedInvoice()
    {
        var invoices = new FakeInvoiceWorkflowLauncher();
        var viewModel = CreateViewModel(invoiceWorkflowLauncher: invoices);
        await viewModel.LoadAsync();

        await viewModel.OpenInvoiceCommand.ExecuteAsync(viewModel.Invoices[0]);

        Assert.Equal(new InvoiceNumber(1), invoices.LastViewedInvoiceNumber);
    }

    [Fact]
    public async Task AddLessonAddsEditableDraftRowWithoutOpeningDialog()
    {
        var lessonEditorLauncher = new FakeLessonEditorLauncher();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
            lessonEditorLauncher: lessonEditorLauncher);
        await viewModel.LoadAsync();

        await viewModel.AddLessonCommand.ExecuteAsync();

        Assert.False(lessonEditorLauncher.AddWasShown);
        Assert.Equal(2, viewModel.Lessons.Count);
        var draft = viewModel.Lessons[1];
        Assert.True(draft.IsDraft);
        Assert.Equal("Anna", draft.StudentShortName);
        Assert.Null(draft.StartsOnDate);
        Assert.Equal(string.Empty, draft.StartsAtTimeText);
        Assert.True(draft.CanEdit);
    }

    [Fact]
    public async Task SavingCompleteDraftLessonAddsCalendarLesson()
    {
        var lessonEditor = new FakeLessonEditorService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
            lessonEditorService: lessonEditor);
        await viewModel.LoadAsync();
        await viewModel.AddLessonCommand.ExecuteAsync();
        var draft = viewModel.Lessons[1];
        draft.StartsOnDate = new DateTime(2026, 6, 24);
        draft.StartsAtTimeText = "15:30";

        await viewModel.SaveLessonCommand.ExecuteAsync(draft);

        Assert.True(lessonEditor.AddWasCalled);
        Assert.Equal("Anna", lessonEditor.LastAddRequest?.StudentShortName);
        Assert.Equal(new DateTimeOffset(2026, 6, 24, 15, 30, 0, TimeSpan.Zero), lessonEditor.LastAddRequest?.StartsAt);
        Assert.False(draft.IsDraft);
        Assert.False(viewModel.HasError);
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
            new FakeStudentOverviewLauncher(),
            lessonEditor,
            new FakeLessonEditorService(),
            new FakeLessonStateService(),
            new FakeInvoiceWorkflowLauncher(),
            new FakeSettingsLauncher(),
            new FakePlanCatalogLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero)));
        await viewModel.LoadAsync();

        await viewModel.SaveLessonCommand.ExecuteAsync(viewModel.Lessons[0]);

        Assert.False(lessonEditor.EditWasShown);
        Assert.True(viewModel.HasError);
    }

    [Fact]
    public async Task PlannedLessonCanBeMarkedCompletedManually()
    {
        var lessonStates = new FakeLessonStateService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
            lessonStates: lessonStates);
        await viewModel.LoadAsync();

        await viewModel.ToggleLessonStateCommand.ExecuteAsync(viewModel.Lessons[0]);

        Assert.Equal("lesson-1", lessonStates.LastLessonId);
        Assert.Equal(LessonState.Planned, lessonStates.LastCurrentState);
        Assert.Equal("Completed", viewModel.Lessons[0].StateText);
        Assert.False(viewModel.Lessons[0].CanEdit);
    }

    [Fact]
    public async Task CompletedLessonRequiresConfirmationBeforeReturningToPlanned()
    {
        var confirmation = new FakeConfirmationService { NextAnswer = true };
        var lessonStates = new FakeLessonStateService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Completed, LessonIsInvoiced = false, LessonPaymentStatus = null },
            confirmation: confirmation,
            lessonStates: lessonStates);
        await viewModel.LoadAsync();

        await viewModel.ToggleLessonStateCommand.ExecuteAsync(viewModel.Lessons[0]);

        Assert.True(confirmation.MarkLessonPlannedWasAsked);
        Assert.Equal(LessonState.Completed, lessonStates.LastCurrentState);
        Assert.Equal("Planned", viewModel.Lessons[0].StateText);
        Assert.True(viewModel.Lessons[0].CanEdit);
    }

    [Fact]
    public async Task CompletedLessonReturnToPlannedIsNotLostWhenConfirmationReactivatesWindow()
    {
        var clock = new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));
        var confirmation = new FakeConfirmationService { NextAnswer = true };
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Completed, LessonIsInvoiced = false, LessonPaymentStatus = null },
            confirmation: confirmation,
            clock: clock);
        confirmation.OnMarkLessonPlannedAsync = () => viewModel.RefreshAfterActivationAsync();
        await viewModel.LoadAsync();
        clock.Now = clock.Now.AddSeconds(11);

        await viewModel.ToggleLessonStateCommand.ExecuteAsync(viewModel.Lessons[0]);

        Assert.Equal("Planned", viewModel.Lessons[0].StateText);
        Assert.True(viewModel.Lessons[0].CanEdit);
    }

    [Fact]
    public async Task CompletedLessonEditIsBlocked()
    {
        var lessonEditor = new FakeLessonEditorService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Completed, LessonIsInvoiced = false, LessonPaymentStatus = null },
            lessonEditorService: lessonEditor);
        await viewModel.LoadAsync();

        await viewModel.SaveLessonCommand.ExecuteAsync(viewModel.Lessons[0]);

        Assert.False(lessonEditor.EditWasCalled);
        Assert.True(viewModel.HasError);
        Assert.Equal("Completed lessons cannot be edited.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task SuccessfulLessonDeleteRemovesVisibleLesson()
    {
        var lessonEditor = new FakeLessonEditorService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
            lessonEditorService: lessonEditor);
        await viewModel.LoadAsync();

        await viewModel.DeleteLessonCommand.ExecuteAsync(viewModel.Lessons[0]);

        Assert.True(lessonEditor.DeleteWasCalled);
        Assert.Empty(viewModel.Lessons);
        Assert.False(viewModel.HasError);
    }

    private static MainWindowViewModel CreateViewModel(
        FakeDashboardService? dashboard = null,
        FakePaymentService? payments = null,
        FakeConfirmationService? confirmation = null,
        FakeCalendarLauncher? calendar = null,
        FakeStudentOverviewLauncher? studentOverviewLauncher = null,
        FakeLessonEditorLauncher? lessonEditorLauncher = null,
        FakeLessonEditorService? lessonEditorService = null,
        FakeLessonStateService? lessonStates = null,
        FakeInvoiceWorkflowLauncher? invoiceWorkflowLauncher = null,
        FakeSettingsLauncher? settingsLauncher = null,
        FakePlanCatalogLauncher? planCatalogLauncher = null,
        FakeUiClock? clock = null) =>
        new(
            dashboard ?? new FakeDashboardService(),
            payments ?? new FakePaymentService(),
            confirmation ?? new FakeConfirmationService(),
            calendar ?? new FakeCalendarLauncher(),
            studentOverviewLauncher ?? new FakeStudentOverviewLauncher(),
            lessonEditorLauncher ?? new FakeLessonEditorLauncher(),
            lessonEditorService ?? new FakeLessonEditorService(),
            lessonStates ?? new FakeLessonStateService(),
            invoiceWorkflowLauncher ?? new FakeInvoiceWorkflowLauncher(),
            settingsLauncher ?? new FakeSettingsLauncher(),
            planCatalogLauncher ?? new FakePlanCatalogLauncher(),
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

    private static IReadOnlyList<T> FindChildren<T>(DependencyObject root)
        where T : DependencyObject
    {
        var matches = new List<T>();
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is T match)
            {
                matches.Add(match);
            }

            if (child is DependencyObject dependencyObject)
            {
                matches.AddRange(FindChildren<T>(dependencyObject));
            }
        }

        return matches;
    }

    private sealed class FakeDashboardService : IStartupDashboardService
    {
        public int LoadCount { get; private set; }

        public DateOnly? LastMonth { get; private set; }

        public string? LastSelectedStudentShortName { get; private set; }

        public IReadOnlyList<string> StudentShortNames { get; init; } = ["Anna"];

        public OperationResult<StartupDashboardResult>? NextResult { get; set; }

        public LessonState LessonState { get; init; } = LessonState.Completed;

        public bool LessonIsInvoiced { get; init; } = true;

        public PaymentStatus? LessonPaymentStatus { get; init; } = PaymentStatus.Outstanding;

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
                            LessonState,
                            LessonIsInvoiced,
                            LessonPaymentStatus)
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

        public bool MarkLessonPlannedWasAsked { get; private set; }

        public Func<Task>? OnMarkLessonPlannedAsync { get; set; }

        public Task<bool> ConfirmMarkInvoiceOutstandingAsync(InvoiceDisplayModel invoice, CancellationToken cancellationToken)
        {
            WasAsked = true;
            return Task.FromResult(NextAnswer);
        }

        public Task<bool> ConfirmDeleteLessonAsync(LessonDisplayModel lesson, CancellationToken cancellationToken) =>
            Task.FromResult(NextAnswer);

        public Task<bool> ConfirmMarkLessonPlannedAsync(LessonDisplayModel lesson, CancellationToken cancellationToken)
        {
            MarkLessonPlannedWasAsked = true;
            return ConfirmMarkLessonPlannedCoreAsync();
        }

        private async Task<bool> ConfirmMarkLessonPlannedCoreAsync()
        {
            if (OnMarkLessonPlannedAsync is not null)
            {
                await OnMarkLessonPlannedAsync();
            }

            return NextAnswer;
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

    private sealed class FakeStudentOverviewLauncher : IStudentOverviewLauncher
    {
        public bool WasShown { get; private set; }

        public string? InitialSelectedStudentShortName { get; private set; }

        public string? SelectedStudentShortName { get; init; }

        public string? SelectionChangedStudentShortName { get; init; }

        public Action? OnAfterSelectionChanged { get; init; }

        public async Task<string?> ShowStudentOverviewAsync(
            string? selectedStudentShortName,
            Func<string?, CancellationToken, Task> selectedStudentChangedAsync,
            CancellationToken cancellationToken)
        {
            WasShown = true;
            InitialSelectedStudentShortName = selectedStudentShortName;
            if (SelectionChangedStudentShortName is not null)
            {
                await selectedStudentChangedAsync(SelectionChangedStudentShortName, cancellationToken);
                OnAfterSelectionChanged?.Invoke();
            }

            return SelectedStudentShortName;
        }
    }

    private sealed class FakeLessonEditorLauncher : ILessonEditorLauncher
    {
        public bool AddWasShown { get; private set; }

        public bool EditWasShown { get; private set; }

        public Task<bool> ShowAddLessonAsync(
            string studentShortName,
            DateTimeOffset startsAt,
            DateOnly refreshMonth,
            CancellationToken cancellationToken)
        {
            AddWasShown = true;
            return Task.FromResult(false);
        }

        public Task<bool> ShowEditLessonAsync(
            LessonDisplayModel lesson,
            DateOnly refreshMonth,
            CancellationToken cancellationToken)
        {
            EditWasShown = true;
            return Task.FromResult(false);
        }
    }

    private sealed class FakeLessonEditorService : ILessonEditorService
    {
        public bool AddWasCalled { get; private set; }

        public bool EditWasCalled { get; private set; }

        public bool DeleteWasCalled { get; private set; }

        public MySmallBusinessLedger.Core.UseCases.Lessons.LessonAddRequest? LastAddRequest { get; private set; }

        public Task<OperationResult<IReadOnlyList<MySmallBusinessLedger.Core.Calendar.CalendarLesson>>> AddAsync(
            MySmallBusinessLedger.Core.UseCases.Lessons.LessonAddRequest request,
            DateOnly refreshMonth,
            CancellationToken cancellationToken)
        {
            AddWasCalled = true;
            LastAddRequest = request;
            return Task.FromResult(OperationResult<IReadOnlyList<MySmallBusinessLedger.Core.Calendar.CalendarLesson>>.Success(
                [new MySmallBusinessLedger.Core.Calendar.CalendarLesson("new-lesson", request.StudentShortName, request.StartsAt)]));
        }

        public Task<OperationResult<IReadOnlyList<MySmallBusinessLedger.Core.Calendar.CalendarLesson>>> EditAsync(
            MySmallBusinessLedger.Core.UseCases.Lessons.LessonEditRequest request,
            DateOnly refreshMonth,
            CancellationToken cancellationToken)
        {
            EditWasCalled = true;
            return Task.FromResult(OperationResult<IReadOnlyList<MySmallBusinessLedger.Core.Calendar.CalendarLesson>>.Success([]));
        }

        public Task<OperationResult<IReadOnlyList<MySmallBusinessLedger.Core.Calendar.CalendarLesson>>> DeleteAsync(
            string lessonId,
            DateOnly refreshMonth,
            CancellationToken cancellationToken)
        {
            DeleteWasCalled = true;
            return Task.FromResult(OperationResult<IReadOnlyList<MySmallBusinessLedger.Core.Calendar.CalendarLesson>>.Success([]));
        }
    }

    private sealed class FakeLessonStateService : ILessonStateService
    {
        public string? LastLessonId { get; private set; }

        public LessonState? LastCurrentState { get; private set; }

        public Task<OperationResult<LessonState>> ToggleAsync(
            string lessonId,
            LessonState currentState,
            bool confirmReturnToPlanned,
            CancellationToken cancellationToken)
        {
            LastLessonId = lessonId;
            LastCurrentState = currentState;
            var nextState = currentState == LessonState.Planned ? LessonState.Completed : LessonState.Planned;
            return Task.FromResult(OperationResult<LessonState>.Success(nextState));
        }
    }

    private sealed class FakeInvoiceWorkflowLauncher : IInvoiceWorkflowLauncher
    {
        public bool NextWriteResult { get; init; }

        public bool WriteWasShown { get; private set; }

        public InvoiceNumber? LastViewedInvoiceNumber { get; private set; }

        public Task<bool> ShowWriteInvoiceAsync(DateOnly displayMonth, CancellationToken cancellationToken)
        {
            WriteWasShown = true;
            return Task.FromResult(NextWriteResult);
        }

        public Task ShowInvoiceViewerAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
        {
            LastViewedInvoiceNumber = invoiceNumber;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSettingsLauncher : ISettingsLauncher
    {
        public bool NextResult { get; init; }

        public bool WasShown { get; private set; }

        public Task<bool> ShowSettingsAsync(CancellationToken cancellationToken)
        {
            WasShown = true;
            return Task.FromResult(NextResult);
        }
    }

    private sealed class FakePlanCatalogLauncher : IPlanCatalogLauncher
    {
        public bool WasShown { get; private set; }

        public Task ShowPlanCatalogAsync(CancellationToken cancellationToken)
        {
            WasShown = true;
            return Task.CompletedTask;
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
