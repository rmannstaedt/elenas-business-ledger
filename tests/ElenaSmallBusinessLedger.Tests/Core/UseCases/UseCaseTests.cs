using ElenaSmallBusinessLedger.Core.Calendar;
using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Company;
using ElenaSmallBusinessLedger.Core.Invoices;
using ElenaSmallBusinessLedger.Core.Lessons;
using ElenaSmallBusinessLedger.Core.Money;
using ElenaSmallBusinessLedger.Core.Plans;
using ElenaSmallBusinessLedger.Core.Ports;
using ElenaSmallBusinessLedger.Core.Students;
using ElenaSmallBusinessLedger.Core.Time;
using ElenaSmallBusinessLedger.Core.UseCases.Invoices;
using ElenaSmallBusinessLedger.Core.UseCases.Lessons;
using ElenaSmallBusinessLedger.Core.UseCases.Startup;

namespace ElenaSmallBusinessLedger.Tests.Core.UseCases;

public sealed class UseCaseTests
{
    [Fact]
    public async Task StartupDashboardShowsSelectedStudentLessonsAcrossPreviousCurrentAndNextMonth()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero));
        var students = new FakeStudentRepository([CreateStudentRecord("Anna")]);
        var calendar = new FakeCalendarGateway(
        [
            new CalendarLesson("past", "Anna", new DateTimeOffset(2026, 5, 20, 15, 0, 0, TimeSpan.Zero)),
            new CalendarLesson("current", "Anna", new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero)),
            new CalendarLesson("next", "Anna", new DateTimeOffset(2026, 7, 7, 15, 0, 0, TimeSpan.Zero)),
            new CalendarLesson("outside", "Anna", new DateTimeOffset(2026, 8, 4, 15, 0, 0, TimeSpan.Zero))
        ]);
        var invoices = new FakeInvoiceRepository
        {
            LessonStatuses =
            [
                new InvoiceLessonStatus("Anna", new DateTimeOffset(2026, 5, 20, 15, 0, 0, TimeSpan.Zero), PaymentStatus.Paid)
            ],
            StudentInvoices =
            [
                new IssuedInvoiceSummary(new InvoiceNumber(1), "Anna", new DateOnly(2026, 5, 20), PaymentStatus.Paid, "invoice.pdf")
            ]
        };
        var query = new StartupDashboardQuery(students, calendar, invoices, clock);

        var result = await query.ExecuteAsync(new DateOnly(2026, 6, 1), "Anna", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Anna", result.Value!.CurrentStudentShortName);
        Assert.Equal(["Anna"], result.Value.StudentShortNames);
        Assert.Equal(3, result.Value.Lessons.Count);
        Assert.Contains(result.Value.Lessons, lesson =>
            lesson.Id == "past" &&
            lesson.State == LessonState.Completed &&
            lesson.IsInvoiced &&
            lesson.PaymentStatus == PaymentStatus.Paid);
        Assert.Contains(result.Value.Invoices, invoice => invoice.InvoiceNumber.Value == 1);
    }

    [Fact]
    public async Task EditingUninvoicedLessonRefreshesFromCalendarAfterCalendarUpdateSucceeds()
    {
        var updatedLesson = new CalendarLesson("lesson-1", "Anna", new DateTimeOffset(2026, 6, 17, 15, 0, 0, TimeSpan.Zero));
        var calendar = new FakeCalendarGateway([updatedLesson]);
        var useCase = new EditLessonUseCase(calendar);

        var result = await useCase.ExecuteAsync(
            new LessonEditRequest(updatedLesson.Id, updatedLesson.StudentShortName, updatedLesson.StartsAt),
            new DateOnly(2026, 6, 1),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(calendar.UpdateCalled);
        Assert.Contains(result.Value!, lesson => lesson.Id == updatedLesson.Id && lesson.StartsAt == updatedLesson.StartsAt);
    }

    [Fact]
    public async Task EditingUninvoicedLessonReportsFailureWhenCalendarUpdateFails()
    {
        var calendar = new FakeCalendarGateway([])
        {
            UpdateResult = OperationResult<IReadOnlyList<CalendarLesson>>.Failure(
                new ValidationError("Calendar.UpdateFailed", "Google Calendar rejected the update."))
        };
        var useCase = new EditLessonUseCase(calendar);

        var result = await useCase.ExecuteAsync(
            new LessonEditRequest("lesson-1", "Anna", new DateTimeOffset(2026, 6, 17, 15, 0, 0, TimeSpan.Zero)),
            new DateOnly(2026, 6, 1),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Calendar.UpdateFailed");
    }

    [Fact]
    public async Task AddingUninvoicedLessonRefreshesFromCalendarAfterCalendarAddSucceeds()
    {
        var addedLesson = new CalendarLesson("lesson-2", "Anna", new DateTimeOffset(2026, 6, 24, 15, 0, 0, TimeSpan.Zero));
        var calendar = new FakeCalendarGateway([addedLesson]);
        var useCase = new AddLessonUseCase(calendar);

        var result = await useCase.ExecuteAsync(
            new LessonAddRequest(addedLesson.StudentShortName, addedLesson.StartsAt),
            new DateOnly(2026, 6, 1),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Contains(result.Value!, lesson => lesson.Id == addedLesson.Id && lesson.StartsAt == addedLesson.StartsAt);
    }

    [Fact]
    public async Task DeletingUninvoicedLessonRefreshesFromCalendarAfterCalendarDeleteSucceeds()
    {
        var remainingLesson = new CalendarLesson("lesson-2", "Anna", new DateTimeOffset(2026, 6, 24, 15, 0, 0, TimeSpan.Zero));
        var calendar = new FakeCalendarGateway([remainingLesson]);
        var useCase = new DeleteLessonUseCase(calendar);

        var result = await useCase.ExecuteAsync("lesson-1", new DateOnly(2026, 6, 1), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Value!, lesson => lesson.Id == "lesson-1");
    }

    [Fact]
    public async Task InvoiceCandidateSelectionListsTodayStudentsWithoutInvoiceAndPreselectsCurrentTimeStudent()
    {
        var students = new FakeStudentRepository([CreateStudentRecord("Anna"), CreateStudentRecord("Bo")]);
        var calendar = new FakeCalendarGateway(
        [
            new CalendarLesson("anna-today", "Anna", new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero)),
            new CalendarLesson("bo-today", "Bo", new DateTimeOffset(2026, 6, 16, 17, 0, 0, TimeSpan.Zero))
        ]);
        var invoices = new FakeInvoiceRepository { StudentsWithInvoiceThisMonth = ["Bo"] };
        var useCase = new InvoiceCandidateSelectionUseCase(students, calendar, invoices);

        var result = await useCase.ExecuteAsync(
            new DateOnly(2026, 6, 16),
            new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(["Anna"], result.Value!.CandidateStudentShortNames);
        Assert.Equal(["Anna", "Bo"], result.Value.AllStudentShortNames);
        Assert.Equal("Anna", result.Value.PreselectedStudentShortName);
    }

    [Fact]
    public async Task InvoicePreviewCreatesDraftFromCalendarLessonsAndActivePlan()
    {
        var students = new FakeStudentRepository([CreateStudentRecord("Anna")]);
        var company = new FakeCompanyProfileRepository();
        var calendar = new FakeCalendarGateway(
        [
            new CalendarLesson("lesson-1", "Anna", new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero))
        ]);
        var useCase = new InvoicePreviewUseCase(students, company, calendar);

        var result = await useCase.CreateAsync(
            "Anna",
            new DateOnly(2026, 6, 16),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 30),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Anna", result.Value!.Draft.Student.ShortName);
        Assert.Single(result.Value.Draft.Lines);
        Assert.Equal("Standard", result.Value.Draft.Lines[0].PlanName);
        Assert.Equal(Money.Dkk(400m), result.Value.Draft.Lines[0].PriceWithoutVat);
    }

    [Fact]
    public async Task InvoicePreviewFailsWhenCalendarLessonHasNoActivePlan()
    {
        var student = CreateStudentRecord("Anna");
        var studentWithoutActivePlan = student with
        {
            PlanPeriods =
            [
                new StudentPlanPeriod(
                    student.PlanPeriods[0].Plan,
                    new DateOnly(2026, 1, 1),
                    new DateOnly(2026, 5, 31))
            ]
        };
        var students = new FakeStudentRepository([studentWithoutActivePlan]);
        var company = new FakeCompanyProfileRepository();
        var calendar = new FakeCalendarGateway(
        [
            new CalendarLesson("lesson-1", "Anna", new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero))
        ]);
        var useCase = new InvoicePreviewUseCase(students, company, calendar);

        var result = await useCase.CreateAsync(
            "Anna",
            new DateOnly(2026, 6, 16),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 30),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Invoice.NoActivePlanForLesson");
    }

    [Fact]
    public async Task IssueInvoiceDoesNotSaveInvoiceOrPrintWhenPdfWriteFails()
    {
        var company = new FakeCompanyProfileRepository();
        var invoices = new FakeInvoiceRepository();
        var fileStore = new FakeInvoiceFileStore();
        var pdfWriter = new FakeInvoicePdfWriter
        {
            WriteResult = OperationResult<string>.Failure(
                new ValidationError("InvoicePdf.WriteFailed", "The PDF could not be written."))
        };
        var printer = new FakeInvoicePrinter();
        var useCase = new IssueInvoiceUseCase(company, invoices, fileStore, pdfWriter, printer);

        var result = await useCase.ExecuteAsync(CreateInvoiceDraft(), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.False(invoices.SaveIssuedCalled);
        Assert.False(printer.PrintCalled);
    }

    [Fact]
    public async Task IssueInvoiceStagesVerifiesPublishesSavesAndPrintsInOrder()
    {
        var company = new FakeCompanyProfileRepository();
        var invoices = new FakeInvoiceRepository();
        var fileStore = new FakeInvoiceFileStore();
        var pdfWriter = new FakeInvoicePdfWriter();
        var printer = new FakeInvoicePrinter();
        var useCase = new IssueInvoiceUseCase(company, invoices, fileStore, pdfWriter, printer);

        var result = await useCase.ExecuteAsync(CreateInvoiceDraft(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("stage:000001", fileStore.Calls[0]);
        Assert.Equal("write:C:\\Invoices\\.staging\\000001.pdf", pdfWriter.Calls[0]);
        Assert.Equal("verify:C:\\Invoices\\.staging\\000001.pdf", fileStore.Calls[1]);
        Assert.Equal("publish:C:\\Invoices\\.staging\\000001.pdf", fileStore.Calls[2]);
        Assert.True(invoices.SaveIssuedCalled);
        Assert.True(printer.PrintCalled);
        Assert.Equal("C:\\Invoices\\000001.pdf", result.Value!.Invoice.PdfFilePath);
    }

    [Fact]
    public async Task PaymentStatusUseCaseSavesOnlyConfirmedStatusChanges()
    {
        var invoices = new FakeInvoiceRepository { CurrentPaymentStatus = PaymentStatus.Paid };
        var useCase = new ToggleInvoicePaymentStatusUseCase(invoices);

        var unconfirmed = await useCase.ExecuteAsync(new InvoiceNumber(1), confirmMarkOutstanding: false, CancellationToken.None);
        var confirmed = await useCase.ExecuteAsync(new InvoiceNumber(1), confirmMarkOutstanding: true, CancellationToken.None);

        Assert.True(unconfirmed.RequiresConfirmation);
        Assert.False(invoices.SavePaymentStatusCalledBeforeConfirmation);
        Assert.True(confirmed.Succeeded);
        Assert.Equal(PaymentStatus.Outstanding, invoices.SavedPaymentStatus);
    }

    private static InvoiceDraft CreateInvoiceDraft() =>
        InvoiceDraft.Create(
            invoiceDate: new DateOnly(2026, 6, 16),
            companySnapshot: new CompanySnapshot("Maple Key Studio", "Musikvej 1", null),
            studentSnapshot: new StudentSnapshot("Anna", "Anna Andersen", "Testvej 1"),
            lines:
            [
                new InvoiceLineDraft(
                    lessonDateTime: new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
                    planName: "Standard",
                    priceWithoutVat: Money.Dkk(400m))
            ],
            priceAdjustment: null);

    private static StudentRecord CreateStudentRecord(string shortName)
    {
        var student = Student.Create(
            shortName,
            $"{shortName} Andersen",
            "Testvej 1",
            "12345678",
            $"{shortName.ToLowerInvariant()}@example.test",
            AgeBracket.Standard,
            birthdate: null,
            isActive: true).Value!;

        var plan = new Plan(
            "Standard",
            priceWithoutVat: 400m,
            LessonsPerWeek.One,
            durationMinutes: 60,
            PlanFlexibility.MoveOrCancelByAgreement,
            numberOfStudents: 1,
            effectiveFrom: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        return new StudentRecord(
            student,
            [new StudentPlanPeriod(plan, new DateOnly(2026, 1, 1), EndsOn: null)],
            [new StudentScheduleEntry(DayOfWeek.Tuesday, new TimeOnly(15, 0))]);
    }

    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now => now;
    }

    private sealed class FakeStudentRepository(IReadOnlyList<StudentRecord> students) : IStudentRepository
    {
        public Task<IReadOnlyList<StudentRecord>> ListActiveStudentsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(students);

        public Task<StudentRecord?> GetByShortNameAsync(string shortName, CancellationToken cancellationToken) =>
            Task.FromResult(students.FirstOrDefault(student => student.Student.ShortName == shortName));

        public Task<OperationResult<StudentRecord>> SaveAsync(StudentRecord student, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<StudentRecord>.Success(student));
    }

    private sealed class FakeCompanyProfileRepository : ICompanyProfileRepository
    {
        public Task<CompanyProfile> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(CompanyProfile.Create(
                "Maple Key Studio",
                "Musikvej 1",
                "12345678",
                "elena@example.test",
                cvr: null,
                vatRegistrationDate: null,
                VatStatus.NotRegistered,
                "C:\\Invoices"));
    }

    private sealed class FakeCalendarGateway(IReadOnlyList<CalendarLesson> lessons) : IGoogleCalendarGateway
    {
        public OperationResult<IReadOnlyList<CalendarLesson>> UpdateResult { get; init; } =
            OperationResult<IReadOnlyList<CalendarLesson>>.Success(lessons);

        public bool UpdateCalled { get; private set; }

        public Task<IReadOnlyList<CalendarLesson>> ListLessonsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
        {
            var matchingLessons = lessons
                .Where(lesson => DateOnly.FromDateTime(lesson.StartsAt.DateTime) >= from)
                .Where(lesson => DateOnly.FromDateTime(lesson.StartsAt.DateTime) <= to)
                .ToArray();

            return Task.FromResult<IReadOnlyList<CalendarLesson>>(matchingLessons);
        }

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> AddLessonAsync(LessonAddRequest request, DateOnly refreshMonth, CancellationToken cancellationToken) =>
            Task.FromResult(UpdateResult);

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> UpdateLessonAsync(LessonEditRequest request, DateOnly refreshMonth, CancellationToken cancellationToken)
        {
            UpdateCalled = true;
            return Task.FromResult(UpdateResult);
        }

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> DeleteLessonAsync(string lessonId, DateOnly refreshMonth, CancellationToken cancellationToken) =>
            Task.FromResult(UpdateResult);
    }

    private sealed class FakeInvoiceRepository : IInvoiceRepository
    {
        public IReadOnlyList<InvoiceLessonStatus> LessonStatuses { get; init; } = [];

        public IReadOnlyList<IssuedInvoiceSummary> StudentInvoices { get; init; } = [];

        public HashSet<string> StudentsWithInvoiceThisMonth { get; init; } = [];

        public PaymentStatus CurrentPaymentStatus { get; init; } = PaymentStatus.Outstanding;

        public bool SaveIssuedCalled { get; private set; }

        public bool SavePaymentStatusCalledBeforeConfirmation { get; private set; }

        public PaymentStatus? SavedPaymentStatus { get; private set; }

        public Task<IReadOnlyList<InvoiceLessonStatus>> ListLessonStatusesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
            Task.FromResult(LessonStatuses);

        public Task<IReadOnlyList<IssuedInvoiceSummary>> ListInvoicesForStudentAsync(string studentShortName, CancellationToken cancellationToken) =>
            Task.FromResult(StudentInvoices);

        public Task<bool> HasInvoiceForStudentInMonthAsync(string studentShortName, DateOnly month, CancellationToken cancellationToken) =>
            Task.FromResult(StudentsWithInvoiceThisMonth.Contains(studentShortName));

        public Task<InvoiceNumber> GetNextInvoiceNumberAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new InvoiceNumber(1));

        public Task<OperationResult<IssuedInvoice>> SaveIssuedInvoiceAsync(IssuedInvoice invoice, CancellationToken cancellationToken)
        {
            SaveIssuedCalled = true;
            return Task.FromResult(OperationResult<IssuedInvoice>.Success(invoice));
        }

        public Task<PaymentStatus> GetPaymentStatusAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken) =>
            Task.FromResult(CurrentPaymentStatus);

        public Task SavePaymentStatusAsync(InvoiceNumber invoiceNumber, PaymentStatus paymentStatus, CancellationToken cancellationToken)
        {
            if (paymentStatus == PaymentStatus.Paid)
            {
                SavePaymentStatusCalledBeforeConfirmation = true;
            }

            SavedPaymentStatus = paymentStatus;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInvoiceFileStore : IInvoiceFileStore
    {
        public List<string> Calls { get; } = [];

        public Task<OperationResult<string>> CreateStagingPathAsync(InvoiceDraft draft, InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
        {
            Calls.Add($"stage:{invoiceNumber}");
            return Task.FromResult(OperationResult<string>.Success("C:\\Invoices\\.staging\\000001.pdf"));
        }

        public Task<OperationResult<string>> VerifyStagedPdfAsync(string stagingPath, CancellationToken cancellationToken)
        {
            Calls.Add($"verify:{stagingPath}");
            return Task.FromResult(OperationResult<string>.Success(stagingPath));
        }

        public Task<OperationResult<string>> PublishAsync(string stagingPath, InvoiceDraft draft, InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
        {
            Calls.Add($"publish:{stagingPath}");
            return Task.FromResult(OperationResult<string>.Success("C:\\Invoices\\000001.pdf"));
        }
    }

    private sealed class FakeInvoicePdfWriter : IInvoicePdfWriter
    {
        public OperationResult<string> WriteResult { get; init; } =
            OperationResult<string>.Success("C:\\Invoices\\.staging\\000001.pdf");

        public List<string> Calls { get; } = [];

        public Task<OperationResult<string>> WriteAsync(
            InvoiceDraft draft,
            InvoiceTotals totals,
            InvoiceNumber invoiceNumber,
            string stagingPath,
            CancellationToken cancellationToken)
        {
            Calls.Add($"write:{stagingPath}");
            return Task.FromResult(WriteResult);
        }
    }

    private sealed class FakeInvoicePrinter : IInvoicePrinter
    {
        public bool PrintCalled { get; private set; }

        public Task<OperationResult<string>> PrintAsync(string pdfFilePath, CancellationToken cancellationToken)
        {
            PrintCalled = true;
            return Task.FromResult(OperationResult<string>.Success(pdfFilePath));
        }
    }
}
