using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Money;
using MySmallBusinessLedger.Core.UseCases.Invoices;

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
                    Money.Dkk(amount)))
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

    private static IssuedInvoice CreateIssuedInvoice()
    {
        var preview = CreatePreview();
        return preview.Draft.Issue(new InvoiceNumber(1), preview.Totals, @"C:\Invoices\000001-Anna.pdf");
    }

    private sealed class FakeInvoiceWorkflowService : IInvoiceWorkflowService
    {
        public OperationResult<InvoiceCandidateSelectionResult> Candidates { get; init; } =
            OperationResult<InvoiceCandidateSelectionResult>.Success(new InvoiceCandidateSelectionResult([], [], null));

        public OperationResult<IssueInvoiceResult> IssueResult { get; init; } =
            OperationResult<IssueInvoiceResult>.Failure(new ValidationError("Test.NotConfigured", "Issue not configured."));

        public bool IssueCalled { get; private set; }

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

        public Task<IssuedInvoice?> GetIssuedInvoiceAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken) =>
            Task.FromResult<IssuedInvoice?>(CreateIssuedInvoice());
    }
}
