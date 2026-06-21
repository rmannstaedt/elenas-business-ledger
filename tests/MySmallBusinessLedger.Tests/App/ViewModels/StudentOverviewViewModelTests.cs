using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.App.Views;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.Students;
using MySmallBusinessLedger.Core.UseCases.Plans;
using MySmallBusinessLedger.Core.UseCases.Students;
using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using WpfGrid = System.Windows.Controls.Grid;
using WpfGridSplitter = System.Windows.Controls.GridSplitter;

namespace MySmallBusinessLedger.Tests.App.ViewModels;

public sealed class StudentOverviewViewModelTests
{
    [Fact]
    public void ViewInitializesWithOverviewSurface()
    {
        RunOnStaThread(() =>
        {
            var viewModel = new StudentOverviewViewModel(
                new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
                new FakeStudentEditorService(),
                new FakePlanCatalogService(),
                new FakeInvoiceWorkflowLauncher(),
                new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
            var window = new StudentOverviewView(viewModel);

            Assert.Equal("Manage Students", window.Title);
            Assert.Same(viewModel, window.DataContext);
            Assert.NotNull(window.Content);

            window.Close();
        });
    }

    [Fact]
    public void CloseButtonIsCompactAndRightAligned()
    {
        RunOnStaThread(() =>
        {
            var viewModel = new StudentOverviewViewModel(
                new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
                new FakeStudentEditorService(),
                new FakePlanCatalogService(),
                new FakeInvoiceWorkflowLauncher(),
                new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
            var window = new StudentOverviewView(viewModel);

            var closeButton = Assert.Single(FindChildren<WpfButton>(window), button => button.Content?.ToString() == "Close");

            Assert.Equal(HorizontalAlignment.Right, closeButton.HorizontalAlignment);
            Assert.Equal(84, closeButton.MinWidth);

            window.Close();
        });
    }

    [Fact]
    public void StudentOverviewLayoutKeepsEditorPanesFixedAndInvoicesResizable()
    {
        RunOnStaThread(() =>
        {
            var viewModel = new StudentOverviewViewModel(
                new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
                new FakeStudentEditorService(),
                new FakePlanCatalogService(),
                new FakeInvoiceWorkflowLauncher(),
                new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
            var window = new StudentOverviewView(viewModel);

            var outerColumns = Assert.Single(FindChildren<WpfGrid>(window), grid => grid.Name == "StudentOverviewColumns");
            Assert.Equal(GridUnitType.Star, outerColumns.ColumnDefinitions[0].Width.GridUnitType);
            Assert.Equal(8, outerColumns.ColumnDefinitions[1].Width.Value);
            Assert.Equal(360, outerColumns.ColumnDefinitions[2].Width.Value);

            var detailColumns = Assert.Single(FindChildren<WpfGrid>(window), grid => grid.Name == "StudentDetailColumns");
            Assert.Equal(GridUnitType.Pixel, detailColumns.ColumnDefinitions[2].Width.GridUnitType);
            Assert.Equal(GridUnitType.Pixel, detailColumns.ColumnDefinitions[4].Width.GridUnitType);
            Assert.Equal(detailColumns.ColumnDefinitions[2].Width.Value, detailColumns.ColumnDefinitions[4].Width.Value);

            var splitter = Assert.Single(FindChildren<WpfGridSplitter>(window), gridSplitter => gridSplitter.Name == "InvoicesPaneSplitter");
            Assert.Equal(System.Windows.Controls.GridResizeDirection.Columns, splitter.ResizeDirection);
            Assert.Equal(System.Windows.Controls.GridResizeBehavior.PreviousAndNext, splitter.ResizeBehavior);

            window.Close();
        });
    }

    [Fact]
    public async Task LoadListsStudentsAndShowsSelectedStudentDetailsPlanAndInvoices()
    {
        var service = new FakeStudentOverviewService(
            [
                CreateSnapshot("Anna", isActive: true),
                CreateSnapshot("Bo", isActive: false)
            ]);
        var viewModel = new StudentOverviewViewModel(
            service,
            new FakeStudentEditorService(),
            new FakePlanCatalogService(),
            new FakeInvoiceWorkflowLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));

        await viewModel.LoadAsync();

        Assert.Equal(["Anna", "Bo"], viewModel.Students.Select(student => student.ShortName).ToArray());
        Assert.Equal("Anna", viewModel.SelectedStudent?.ShortName);
        Assert.Equal("Anna Andersen", viewModel.SelectedStudentEditor?.FullName);
        Assert.True(viewModel.SelectedStudentEditor?.IsActive);
        Assert.Equal("Standard", viewModel.SelectedStudentEditor?.PlanName);
        Assert.Equal("400 kr.", viewModel.SelectedStudentEditor?.PlanPriceWithoutVatText);
        Assert.Equal("Paid monthly in advance", viewModel.SelectedStudentEditor?.PlanPaymentTerms);
        var invoice = Assert.Single(viewModel.Invoices);
        Assert.Equal("000001", invoice.InvoiceNumberText);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task OpenInvoiceCommandUsesExistingInvoiceViewerLauncher()
    {
        var invoices = new FakeInvoiceWorkflowLauncher();
        var viewModel = new StudentOverviewViewModel(
            new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
            new FakeStudentEditorService(),
            new FakePlanCatalogService(),
            invoices,
            new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
        await viewModel.LoadAsync();

        await viewModel.OpenInvoiceCommand.ExecuteAsync(viewModel.Invoices[0]);

        Assert.Equal(new InvoiceNumber(1), invoices.LastViewedInvoiceNumber);
    }

    [Fact]
    public async Task SaveStudentChangesThroughEmbeddedEditorAndRefreshesSelectedStudent()
    {
        var service = new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]);
        var editor = new FakeStudentEditorService();
        var viewModel = new StudentOverviewViewModel(
            service,
            editor,
            new FakePlanCatalogService(),
            new FakeInvoiceWorkflowLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
        await viewModel.LoadAsync();

        viewModel.SelectedStudentEditor!.FullName = "Anna A.";
        viewModel.SelectedStudentEditor.IsActive = false;

        await viewModel.SaveStudentCommand.ExecuteAsync();

        Assert.NotNull(editor.LastRequest);
        Assert.Equal("Anna", editor.LastRequest.ShortName);
        Assert.Equal("Anna A.", editor.LastRequest.FullName);
        Assert.False(editor.LastRequest.IsActive);
        Assert.Equal("Anna", viewModel.SelectedStudent?.ShortName);
        Assert.Equal(2, service.LoadCount);
    }

    [Fact]
    public async Task CancelStudentEditDiscardsUnsavedChanges()
    {
        var viewModel = new StudentOverviewViewModel(
            new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
            new FakeStudentEditorService(),
            new FakePlanCatalogService(),
            new FakeInvoiceWorkflowLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
        await viewModel.LoadAsync();

        viewModel.SelectedStudentEditor!.FullName = "Unsaved";

        await viewModel.CancelStudentEditCommand.ExecuteAsync();

        Assert.Equal("Anna Andersen", viewModel.SelectedStudentEditor.FullName);
    }

    [Fact]
    public async Task AddStudentCreatesEmbeddedDraftAndSaveSelectsSavedStudent()
    {
        var service = new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]);
        var editor = new FakeStudentEditorService { SavedShortName = "Bo" };
        var viewModel = new StudentOverviewViewModel(
            service,
            editor,
            new FakePlanCatalogService(),
            new FakeInvoiceWorkflowLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
        await viewModel.LoadAsync();

        await viewModel.AddStudentCommand.ExecuteAsync();
        viewModel.SelectedStudentEditor!.ShortName = "Bo";
        viewModel.SelectedStudentEditor.FullName = "Bo Berg";
        viewModel.SelectedStudentEditor.Address = "Testvej 2";
        viewModel.SelectedStudentEditor.Phone = "87654321";
        viewModel.SelectedStudentEditor.Email = "bo@example.test";
        service.AddSnapshot(CreateSnapshot("Bo", isActive: true));

        await viewModel.SaveStudentCommand.ExecuteAsync();

        Assert.NotNull(editor.LastRequest);
        Assert.Equal("Bo", editor.LastRequest.ShortName);
        Assert.Equal("Bo", viewModel.SelectedStudent?.ShortName);
    }

    [Fact]
    public async Task SelectingAssignablePlanInEmbeddedEditorCopiesPlanSnapshotIntoSaveRequest()
    {
        var editor = new FakeStudentEditorService();
        var planCatalog = new FakePlanCatalogService(
            [
                CreateCatalogPlan(
                    "Duo",
                    priceWithoutVat: 650m,
                    lessonsPerWeek: LessonsPerWeek.Two,
                    durationMinutes: 45,
                    flexibility: PlanFlexibility.None,
                    numberOfStudents: 2)
            ]);
        var viewModel = new StudentOverviewViewModel(
            new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
            editor,
            planCatalog,
            new FakeInvoiceWorkflowLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
        await viewModel.LoadAsync();

        viewModel.SelectedStudentEditor!.SelectedAssignablePlan =
            viewModel.SelectedStudentEditor.AssignablePlans.Single(plan => plan.Name == "Duo");
        await viewModel.SaveStudentCommand.ExecuteAsync();

        Assert.NotNull(editor.LastRequest);
        var savedPlan = Assert.Single(editor.LastRequest.PlanPeriods);
        Assert.Equal("Duo", savedPlan.PlanName);
        Assert.Equal(650m, savedPlan.PriceWithoutVat);
        Assert.Equal(LessonsPerWeek.Two, savedPlan.LessonsPerWeek);
        Assert.Equal(45, savedPlan.DurationMinutes);
        Assert.Equal(PlanFlexibility.None, savedPlan.Flexibility);
        Assert.Equal(2, savedPlan.NumberOfStudents);
    }

    private static StudentOverviewSnapshot CreateSnapshot(string shortName, bool isActive)
    {
        var student = Student.Create(
            shortName,
            $"{shortName} Andersen",
            "Testvej 1" + Environment.NewLine + "1234 Testby",
            "12345678",
            $"{shortName.ToLowerInvariant()}@example.test",
            AgeBracket.Standard,
            birthdate: null,
            isActive).Value!;

        var record = new StudentRecord(
            student,
            [
                new StudentPlanPeriod(
                    new Plan(
                        "Old",
                        priceWithoutVat: 300m,
                        LessonsPerWeek.One,
                        durationMinutes: 30,
                        PlanFlexibility.MoveOrCancelByAgreement,
                        numberOfStudents: 1,
                        effectiveFrom: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                        paymentTerms: "Paid monthly in advance"),
                    new DateOnly(2026, 1, 1),
                    new DateOnly(2026, 5, 31)),
                new StudentPlanPeriod(
                    new Plan(
                        "Standard",
                        priceWithoutVat: 400m,
                        LessonsPerWeek.One,
                        durationMinutes: 60,
                        PlanFlexibility.MoveOrCancelByAgreement,
                        numberOfStudents: 1,
                        effectiveFrom: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                        paymentTerms: "Paid monthly in advance"),
                    new DateOnly(2026, 6, 1),
                    EndsOn: null)
            ],
            []);

        return new StudentOverviewSnapshot(
            record,
            [
                new IssuedInvoiceSummary(
                    new InvoiceNumber(1),
                    shortName,
                    new DateOnly(2026, 6, 16),
                    PaymentStatus.Outstanding,
                    "C:\\Invoices\\000001.pdf")
            ]);
    }

    private static PlanCatalogItem CreateCatalogPlan(
        string name,
        decimal priceWithoutVat = 400m,
        LessonsPerWeek lessonsPerWeek = LessonsPerWeek.One,
        int durationMinutes = 60,
        PlanFlexibility flexibility = PlanFlexibility.MoveOrCancelByAgreement,
        int numberOfStudents = 1) =>
        new(
            Id: 1,
            name,
            priceWithoutVat,
            lessonsPerWeek,
            durationMinutes,
            flexibility,
            numberOfStudents,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "Paid monthly in advance",
            RetireOn: null,
            PlanCatalogState.Active);

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

    private sealed class FakeStudentOverviewService : IStudentOverviewService
    {
        private readonly List<StudentOverviewSnapshot> snapshots;

        public FakeStudentOverviewService(IReadOnlyList<StudentOverviewSnapshot> snapshots)
        {
            this.snapshots = snapshots.ToList();
        }

        public int LoadCount { get; private set; }

        public string? LastSetActiveStudentShortName { get; private set; }

        public bool? LastSetActiveValue { get; private set; }

        public Task<IReadOnlyList<StudentOverviewSnapshot>> LoadAsync(CancellationToken cancellationToken)
        {
            LoadCount++;
            return Task.FromResult<IReadOnlyList<StudentOverviewSnapshot>>(snapshots.ToArray());
        }

        public void AddSnapshot(StudentOverviewSnapshot snapshot) =>
            snapshots.Add(snapshot);

        public Task<OperationResult<StudentOverviewSnapshot>> SetStudentActiveAsync(
            string studentShortName,
            bool isActive,
            CancellationToken cancellationToken)
        {
            LastSetActiveStudentShortName = studentShortName;
            LastSetActiveValue = isActive;
            var index = snapshots.FindIndex(snapshot => snapshot.Student.ShortName == studentShortName);
            var snapshot = snapshots[index];
            var updatedStudent = snapshot.Student with { IsActive = isActive };
            var updated = snapshot with
            {
                Record = snapshot.Record with { Student = updatedStudent }
            };
            snapshots[index] = updated;
            return Task.FromResult(OperationResult<StudentOverviewSnapshot>.Success(updated));
        }
    }

    private sealed class FakeStudentEditorService : IStudentEditorService
    {
        public string SavedShortName { get; init; } = "Anna";

        public SaveStudentRequest? LastRequest { get; private set; }

        public Task<StudentEditorSnapshot?> LoadAsync(string shortName, CancellationToken cancellationToken) =>
            Task.FromResult<StudentEditorSnapshot?>(null);

        public Task<OperationResult<string>> SaveAsync(SaveStudentRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(OperationResult<string>.Success(SavedShortName));
        }
    }

    private sealed class FakePlanCatalogService : IPlanCatalogService
    {
        private readonly IReadOnlyList<PlanCatalogItem> assignablePlans;

        public FakePlanCatalogService(IReadOnlyList<PlanCatalogItem>? assignablePlans = null)
        {
            this.assignablePlans = assignablePlans ?? [CreateCatalogPlan("Standard")];
        }

        public Task<IReadOnlyList<PlanCatalogItem>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(assignablePlans);

        public Task<IReadOnlyList<PlanCatalogItem>> ListAssignableAsync(CancellationToken cancellationToken) =>
            Task.FromResult(assignablePlans);

        public Task<OperationResult<PlanCatalogItem>> CreateDraftAsync(PlanDraftInput input, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<PlanCatalogItem>> UpdateDraftAsync(int planId, PlanDraftInput input, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<PlanCatalogItem>> RenameAsync(int planId, string newName, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<bool>> DeleteDraftAsync(int planId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<PlanCatalogItem>> PublishDraftAsync(int planId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<PlanCatalogItem>> RetireAsync(int planId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<PlanCatalogItem>> ReactivateAsync(int planId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<PlanCatalogItem>> UpdateRetireOnAsync(int planId, DateTimeOffset? retireOn, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<int>> RetireDueAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeInvoiceWorkflowLauncher : IInvoiceWorkflowLauncher
    {
        public InvoiceNumber? LastViewedInvoiceNumber { get; private set; }

        public Task<bool> ShowWriteInvoiceAsync(DateOnly displayMonth, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task ShowInvoiceViewerAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
        {
            LastViewedInvoiceNumber = invoiceNumber;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUiClock : IUiClock
    {
        public FakeUiClock(DateTimeOffset now)
        {
            Now = now;
        }

        public DateTimeOffset Now { get; }
    }
}
