using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.App.ViewModels;
using ElenasBusinessLedger.App.Views;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Students;
using ElenasBusinessLedger.Core.UseCases.Plans;
using ElenasBusinessLedger.Core.UseCases.Students;

namespace ElenasBusinessLedger.Tests.App.Services;

public sealed class StudentOverviewWindowLauncherTests
{
    [Fact]
    public void SelectingStudentReopensDetailsWindowWhenUserClosedIt()
    {
        RunOnStaThread(() =>
        {
            Exception? dispatcherException = null;
            var launcher = new StudentOverviewWindowLauncher(
                new FakeStudentOverviewService(
                    [
                        CreateSnapshot("Anna"),
                        CreateSnapshot("Bo")
                    ]),
                new FakeStudentEditorService(),
                new FakePlanCatalogService(),
                new FakeInvoiceWorkflowLauncher(),
                new FakeUiClock(new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero)));

            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    var overviewWindow = Assert.Single(VisibleWindows<StudentOverviewView>());
                    var viewModel = Assert.IsType<StudentOverviewViewModel>(overviewWindow.DataContext);
                    var firstDetailsWindow = Assert.Single(VisibleDetailsWindowsFor(viewModel));

                    firstDetailsWindow.Close();
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Assert.Empty(VisibleDetailsWindowsFor(viewModel));

                    await viewModel.SelectStudentAsync(viewModel.Students.Single(student => student.ShortName == "Bo"));
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

                    var reopenedDetailsWindow = Assert.Single(VisibleDetailsWindowsFor(viewModel));
                    Assert.NotSame(firstDetailsWindow, reopenedDetailsWindow);
                    Assert.Same(viewModel, reopenedDetailsWindow.DataContext);
                    Assert.Equal("Bo", viewModel.SelectedStudent?.ShortName);

                    overviewWindow.Close();
                }
                catch (Exception ex)
                {
                    dispatcherException = ex;
                    foreach (var window in VisibleWindows<Window>())
                    {
                        window.Close();
                    }
                }
            }), DispatcherPriority.ApplicationIdle);

            launcher.ShowStudentOverviewAsync(
                    "Anna",
                    static (_, _) => Task.CompletedTask,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            if (dispatcherException is not null)
            {
                ExceptionDispatchInfo.Capture(dispatcherException).Throw();
            }
        });
    }

    private static IReadOnlyList<TWindow> VisibleWindows<TWindow>()
        where TWindow : Window
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        return PresentationSource.CurrentSources
            .OfType<HwndSource>()
            .Select(source => source.RootVisual)
            .OfType<TWindow>()
            .Where(window => ReferenceEquals(window.Dispatcher, dispatcher))
            .Where(window => window.IsVisible)
            .ToArray();
    }

    private static IReadOnlyList<StudentDetailsView> VisibleDetailsWindowsFor(StudentOverviewViewModel viewModel) =>
        VisibleWindows<StudentDetailsView>()
            .Where(window => ReferenceEquals(window.DataContext, viewModel))
            .ToArray();

    private static StudentOverviewSnapshot CreateSnapshot(string shortName)
    {
        var student = Student.Create(
            shortName,
            $"{shortName} Andersen",
            "Testvej 1" + Environment.NewLine + "1234 Testby",
            "12345678",
            $"{shortName.ToLowerInvariant()}@example.test",
            AgeBracket.Standard,
            birthdate: null,
            isActive: true).Value!;

        var record = new StudentRecord(
            0,
            student,
            [
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

        return new StudentOverviewSnapshot(record, []);
    }

    private static PlanCatalogItem CreateCatalogPlan() =>
        new(
            Id: 1,
            Name: "Standard",
            PriceWithoutVat: 400m,
            LessonsPerWeek: LessonsPerWeek.One,
            DurationMinutes: 60,
            Flexibility: PlanFlexibility.MoveOrCancelByAgreement,
            NumberOfStudents: 1,
            EffectiveFrom: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            PaymentTerms: "Paid monthly in advance",
            RetireOn: null,
            State: PlanCatalogState.Active);

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

    private sealed class FakeStudentOverviewService : IStudentOverviewService
    {
        private readonly IReadOnlyList<StudentOverviewSnapshot> snapshots;

        public FakeStudentOverviewService(IReadOnlyList<StudentOverviewSnapshot> snapshots)
        {
            this.snapshots = snapshots;
        }

        public Task<IReadOnlyList<StudentOverviewSnapshot>> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(snapshots);

        public Task<OperationResult<StudentOverviewSnapshot>> SetStudentActiveAsync(
            string studentShortName,
            bool isActive,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeStudentEditorService : IStudentEditorService
    {
        public Task<StudentEditorSnapshot?> LoadAsync(string shortName, CancellationToken cancellationToken) =>
            Task.FromResult<StudentEditorSnapshot?>(null);

        public Task<OperationResult<string>> SaveAsync(SaveStudentRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<string>.Success(request.ShortName));
    }

    private sealed class FakePlanCatalogService : IPlanCatalogService
    {
        public Task<IReadOnlyList<PlanCatalogItem>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlanCatalogItem>>([CreateCatalogPlan()]);

        public Task<IReadOnlyList<PlanCatalogItem>> ListAssignableAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlanCatalogItem>>([CreateCatalogPlan()]);

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

        public Task<OperationResult<PlanCatalogItem>> UpdateRetireOnAsync(
            int planId,
            DateTimeOffset? retireOn,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<int>> RetireDueAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeInvoiceWorkflowLauncher : IInvoiceWorkflowLauncher
    {
        public Task<bool> ShowWriteInvoiceAsync(
            DateOnly displayMonth,
            string? initialStudentShortName,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task ShowInvoiceViewerAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken) =>
            Task.CompletedTask;
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
