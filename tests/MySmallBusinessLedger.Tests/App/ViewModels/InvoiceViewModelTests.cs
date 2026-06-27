using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Money;
using MySmallBusinessLedger.Core.UseCases.Invoices;
using MySmallBusinessLedger.App.Views;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfCheckBox = System.Windows.Controls.CheckBox;

namespace MySmallBusinessLedger.Tests.App.ViewModels;

public sealed class InvoiceViewModelTests
{
    [Fact]
    public async Task CandidateDialogLoadsStudentsAndPreselectsCurrentStudent()
    {
        var service = new FakeInvoiceWorkflowService
        {
            Candidates = OperationResult<InvoiceCandidateSelectionResult>.Success(
                new InvoiceCandidateSelectionResult(["Anna"], ["Anna", "Bo"], "Anna"))
        };
        var viewModel = new InvoiceCandidateDialogViewModel(
            service,
            new DateOnly(2026, 6, 16),
            new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero));

        await viewModel.LoadAsync();

        Assert.Equal(["Anna"], viewModel.CandidateStudentShortNames);
        Assert.Equal(["Anna", "Bo"], viewModel.AllStudentShortNames);
        Assert.Equal("Anna", viewModel.SelectedStudentShortName);
        Assert.True(viewModel.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public async Task CandidateDialogPrefersInitialStudentOverTimeBasedPreselection()
    {
        var service = new FakeInvoiceWorkflowService
        {
            Candidates = OperationResult<InvoiceCandidateSelectionResult>.Success(
                new InvoiceCandidateSelectionResult(["Karina"], ["Mira", "Karina"], "Karina"))
        };
        var viewModel = new InvoiceCandidateDialogViewModel(
            service,
            new DateOnly(2026, 6, 23),
            new DateTimeOffset(2026, 6, 23, 15, 0, 0, TimeSpan.Zero),
            initialStudentShortName: "Mira");

        await viewModel.LoadAsync();

        Assert.Equal("Mira", viewModel.SelectedStudentShortName);
        Assert.True(viewModel.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public async Task PreviewRequiresReasonWhenPriceAdjustmentIsEntered()
    {
        var service = new FakeInvoiceWorkflowService();
        var preview = CreatePreview();
        var viewModel = new InvoicePreviewViewModel(service, preview);
        viewModel.PriceAdjustmentAmountText = "-50";
        viewModel.PriceAdjustmentReason = "";

        await viewModel.PrintInvoiceCommand.ExecuteAsync();

        Assert.False(service.IssueCalled);
        Assert.Equal("A price adjustment requires a reason.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task PreviewIssuesEditedDraftAndRequestsCloseAfterSuccessfulPrint()
    {
        var service = new FakeInvoiceWorkflowService
        {
            IssueResult = OperationResult<IssueInvoiceResult>.Success(
                new IssueInvoiceResult(CreateIssuedInvoice(), PrintSucceeded: true, []))
        };
        var preview = CreatePreview();
        var viewModel = new InvoicePreviewViewModel(service, preview);
        var closed = false;
        viewModel.CloseRequested += (_, result) => closed = result == true;

        await viewModel.PrintInvoiceCommand.ExecuteAsync();

        Assert.True(service.IssueCalled);
        Assert.True(viewModel.WasIssued);
        Assert.True(closed);
    }

    [Fact]
    public async Task PreviewDoesNotIssueWhenAllLessonLinesAreExcluded()
    {
        var service = new FakeInvoiceWorkflowService();
        var viewModel = new InvoicePreviewViewModel(service, CreatePreview());
        viewModel.Lines[0].IsIncluded = false;

        await viewModel.PrintInvoiceCommand.ExecuteAsync();

        Assert.False(service.IssueCalled);
        Assert.Equal("An invoice requires at least one lesson.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task PreviewRefreshesTotalsFromIncludedLinesAndPriceAdjustment()
    {
        var service = new FakeInvoiceWorkflowService();
        var viewModel = new InvoicePreviewViewModel(service, CreatePreview(400m, 400m));
        viewModel.Lines[1].IsIncluded = false;
        viewModel.PriceAdjustmentAmountText = "-50";
        viewModel.PriceAdjustmentReason = "Rabat";

        await viewModel.RefreshTotalsAsync();

        Assert.Equal("350,00 DKK", viewModel.TotalWithoutVatText);
        Assert.Equal("350,00 DKK", viewModel.TotalText);
    }

    [Fact]
    public async Task PreviewStaysOpenWhenInvoiceIsSavedButPrintRequestFails()
    {
        var service = new FakeInvoiceWorkflowService
        {
            IssueResult = OperationResult<IssueInvoiceResult>.Success(
                new IssueInvoiceResult(
                    CreateIssuedInvoice(),
                    PrintSucceeded: false,
                    [new ValidationError("InvoicePrinter.PrintRequestFailed", "The invoice PDF was saved, but the print request failed.")]))
        };
        var viewModel = new InvoicePreviewViewModel(service, CreatePreview());
        var closeRequested = false;
        viewModel.CloseRequested += (_, _) => closeRequested = true;

        await viewModel.PrintInvoiceCommand.ExecuteAsync();

        Assert.True(service.IssueCalled);
        Assert.True(viewModel.WasIssued);
        Assert.False(closeRequested);
        Assert.Equal("The invoice PDF was saved, but the print request failed.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task PreviewDoesNotIssueWhenPrinterSetupIsDeclined()
    {
        var service = new FakeInvoiceWorkflowService
        {
            IssueResult = OperationResult<IssueInvoiceResult>.Success(
                new IssueInvoiceResult(CreateIssuedInvoice(), PrintSucceeded: true, []))
        };
        var viewModel = new InvoicePreviewViewModel(
            service,
            CreatePreview(),
            _ => Task.FromResult(false));

        await viewModel.PrintInvoiceCommand.ExecuteAsync();

        Assert.False(service.IssueCalled);
        Assert.Equal("Choose an invoice printer before printing the invoice.", viewModel.ErrorMessage);
    }

    [Fact]
    public void PreviewIncludeCheckboxPreviewClickTogglesLine()
    {
        RunOnStaThread(() =>
        {
            var viewModel = new InvoicePreviewViewModel(new FakeInvoiceWorkflowService(), CreatePreview());
            var window = new InvoicePreviewView(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => FindVisualChildren<WpfCheckBox>(window).Count == 1);
                var includeCheckBox = Assert.Single(FindVisualChildren<WpfCheckBox>(window));

                Assert.True(viewModel.Lines[0].IsIncluded);

                RaisePreviewLeftClick(includeCheckBox);
                PumpUntil(() => !viewModel.Lines[0].IsIncluded);

                Assert.False(viewModel.Lines[0].IsIncluded);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public async Task ViewerReprintUsesExistingInvoiceNumberAndRefreshesPrintStatus()
    {
        var updatedInvoice = CreateIssuedInvoice(
            printStatus: InvoicePrintStatus.Printed,
            printedAt: new DateTimeOffset(2026, 6, 16, 18, 0, 0, TimeSpan.Zero),
            lastPrintError: null);
        var service = new FakeInvoiceWorkflowService
        {
            ReprintResult = OperationResult<ReprintInvoiceResult>.Success(
                new ReprintInvoiceResult(updatedInvoice, PrintSucceeded: true, []))
        };
        var viewModel = new InvoiceViewerViewModel(
            CreateIssuedInvoice(printStatus: InvoicePrintStatus.PrintFailed, lastPrintError: "Paper jam."),
            service);

        await viewModel.ReprintInvoiceCommand.ExecuteAsync();

        Assert.Equal(new InvoiceNumber(1), service.ReprintInvoiceNumber);
        Assert.Equal("Print request sent 16-06-2026 18:00", viewModel.PrintStatusText);
        Assert.Null(viewModel.LastPrintError);
        Assert.False(viewModel.HasLastPrintError);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task ViewerFailedReprintKeepsInvoiceOpenAndShowsPrinterError()
    {
        var failedInvoice = CreateIssuedInvoice(
            printStatus: InvoicePrintStatus.PrintFailed,
            printedAt: null,
            lastPrintError: "The configured invoice printer is not available.");
        var service = new FakeInvoiceWorkflowService
        {
            ReprintResult = OperationResult<ReprintInvoiceResult>.Success(
                new ReprintInvoiceResult(
                    failedInvoice,
                    PrintSucceeded: false,
                    [new ValidationError("InvoicePrinter.Unavailable", "The configured invoice printer is not available.")]))
        };
        var viewModel = new InvoiceViewerViewModel(CreateIssuedInvoice(), service);

        await viewModel.ReprintInvoiceCommand.ExecuteAsync();

        Assert.Equal("Print failed", viewModel.PrintStatusText);
        Assert.Equal("The configured invoice printer is not available.", viewModel.LastPrintError);
        Assert.Equal("The configured invoice printer is not available.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task ViewerDoesNotReprintWhenPrinterSetupIsDeclined()
    {
        var service = new FakeInvoiceWorkflowService
        {
            ReprintResult = OperationResult<ReprintInvoiceResult>.Success(
                new ReprintInvoiceResult(CreateIssuedInvoice(InvoicePrintStatus.Printed), PrintSucceeded: true, []))
        };
        var viewModel = new InvoiceViewerViewModel(
            CreateIssuedInvoice(),
            service,
            _ => Task.FromResult(false));

        await viewModel.ReprintInvoiceCommand.ExecuteAsync();

        Assert.Null(service.ReprintInvoiceNumber);
        Assert.Equal("Choose an invoice printer before reprinting the invoice.", viewModel.ErrorMessage);
    }

    [Fact]
    public void ViewerDoesNotOfferReprintWhenNoPublishedPdfExists()
    {
        var viewModel = new InvoiceViewerViewModel(CreateIssuedInvoice(pdfFilePath: null), new FakeInvoiceWorkflowService());

        Assert.False(viewModel.ReprintInvoiceCommand.CanExecute(null));
        Assert.Equal("Not printed", viewModel.PrintStatusText);
    }

    private static InvoicePreviewResult CreatePreview(params decimal[] lineAmounts)
    {
        if (lineAmounts.Length == 0)
        {
            lineAmounts = [400m];
        }

        var draft = InvoiceDraft.Create(
            new DateOnly(2026, 6, 16),
            new CompanySnapshot("Maple Key Studio", "Musikvej 1", null),
            new StudentSnapshot("Anna", "Anna Andersen", "Testvej 1"),
            lineAmounts
                .Select((amount, index) => new InvoiceLineDraft(
                    new DateTimeOffset(2026, 6, 16 + index, 15, 0, 0, TimeSpan.Zero),
                    "Standard",
                    Money.Dkk(amount),
                    planCatalogId: 7,
                    lessonId: 100 + index))
                .ToArray(),
            priceAdjustment: null);

        return new InvoicePreviewResult(
            draft,
            draft.CalculateTotals(CreateCompanyProfile()));
    }

    private static CompanyProfile CreateCompanyProfile() =>
        CompanyProfile.Create(
            "Maple Key Studio",
            "Musikvej 1",
            phone: string.Empty,
            email: string.Empty,
            cvr: null,
            vatRegistrationDate: null,
            vatStatus: VatStatus.NotRegistered,
            invoiceFolderPath: @"C:\Invoices");

    private static IssuedInvoice CreateIssuedInvoice(
        InvoicePrintStatus printStatus) =>
        CreateIssuedInvoice(
            pdfFilePath: @"C:\Invoices\000001-Anna.pdf",
            printStatus,
            printedAt: null,
            lastPrintError: null);

    private static IssuedInvoice CreateIssuedInvoice(
        string? pdfFilePath = @"C:\Invoices\000001-Anna.pdf",
        InvoicePrintStatus printStatus = InvoicePrintStatus.NotPrinted,
        DateTimeOffset? printedAt = null,
        string? lastPrintError = null)
    {
        var preview = CreatePreview();
        return preview.Draft.Issue(
            new InvoiceNumber(1),
            preview.Totals,
            pdfFilePath,
            InvoiceIssueStatus.Issued,
            printStatus,
            printedAt,
            lastPrintError);
    }

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

    private static IReadOnlyList<T> FindVisualChildren<T>(DependencyObject root)
        where T : DependencyObject
    {
        var matches = new List<T>();
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                matches.Add(match);
            }

            matches.AddRange(FindVisualChildren<T>(child));
        }

        return matches;
    }

    private static void RaisePreviewLeftClick(UIElement element)
    {
        element.RaiseEvent(new MouseButtonEventArgs(
            Mouse.PrimaryDevice,
            Environment.TickCount,
            MouseButton.Left)
        {
            RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
            Source = element
        });
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (!condition() && DateTimeOffset.UtcNow < deadline)
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        Assert.True(condition());
    }

    private sealed class FakeInvoiceWorkflowService : IInvoiceWorkflowService
    {
        public OperationResult<InvoiceCandidateSelectionResult> Candidates { get; init; } =
            OperationResult<InvoiceCandidateSelectionResult>.Success(new InvoiceCandidateSelectionResult([], [], null));

        public OperationResult<IssueInvoiceResult> IssueResult { get; init; } =
            OperationResult<IssueInvoiceResult>.Failure(new ValidationError("Test.NotConfigured", "Issue not configured."));

        public OperationResult<ReprintInvoiceResult> ReprintResult { get; init; } =
            OperationResult<ReprintInvoiceResult>.Failure(new ValidationError("Test.NotConfigured", "Reprint not configured."));

        public bool IssueCalled { get; private set; }

        public InvoiceNumber? ReprintInvoiceNumber { get; private set; }

        public Task<OperationResult<InvoiceCandidateSelectionResult>> LoadCandidatesAsync(DateOnly date, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(Candidates);

        public Task<OperationResult<InvoicePreviewResult>> CreatePreviewAsync(string studentShortName, DateOnly invoiceDate, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<InvoicePreviewResult>.Success(CreatePreview()));

        public Task<OperationResult<InvoiceTotals>> CalculateTotalsAsync(InvoiceDraft draft, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<InvoiceTotals>.Success(draft.CalculateTotals(CreateCompanyProfile())));

        public Task<OperationResult<IssueInvoiceResult>> IssueAsync(InvoiceDraft draft, CancellationToken cancellationToken)
        {
            IssueCalled = true;
            return Task.FromResult(IssueResult);
        }

        public Task<OperationResult<ReprintInvoiceResult>> ReprintAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
        {
            ReprintInvoiceNumber = invoiceNumber;
            return Task.FromResult(ReprintResult);
        }

        public Task<IssuedInvoice?> GetIssuedInvoiceAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken) =>
            Task.FromResult<IssuedInvoice?>(CreateIssuedInvoice());
    }
}
