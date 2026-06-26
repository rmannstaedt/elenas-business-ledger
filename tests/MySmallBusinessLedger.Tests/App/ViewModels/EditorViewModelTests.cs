using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Students;
using MySmallBusinessLedger.Core.UseCases.Lessons;
using MySmallBusinessLedger.Core.UseCases.Students;

namespace MySmallBusinessLedger.Tests.App.ViewModels;

public sealed class EditorViewModelTests
{
    [Fact]
    public void StudentEditorSplitsSavedMultilineAddressAcrossAddressFields()
    {
        var snapshot = new StudentEditorSnapshot(
            7,
            "Anna",
            "Anna Andersen",
            "Testvej 1" + Environment.NewLine + "1234 Testby",
            "12345678",
            "anna@example.test",
            AgeBracket.Standard,
            null,
            true,
            [],
            []);
        var viewModel = StudentEditorViewModel.FromSnapshot(new FakeStudentEditorService(), snapshot);

        Assert.Equal("Testvej 1", viewModel.Address);
        Assert.Equal("1234 Testby", viewModel.AddressLine2);
    }

    [Fact]
    public async Task StudentEditorCombinesAddressLinesWhenSaving()
    {
        var service = new FakeStudentEditorService();
        var viewModel = CreateNewStudentEditorWithActivePlan(service);
        viewModel.ShortName = "Anna";
        viewModel.FullName = "Anna Andersen";
        viewModel.Address = "Testvej 1";
        viewModel.AddressLine2 = "1234 Testby";
        viewModel.Phone = "12345678";
        viewModel.Email = "anna@example.test";

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.NotNull(service.LastRequest);
        Assert.Equal("Testvej 1" + Environment.NewLine + "1234 Testby", service.LastRequest.Address);
    }

    [Fact]
    public async Task StudentEditorPreservesInputWhenValidationFails()
    {
        var service = new FakeStudentEditorService
        {
            SaveResult = OperationResult<string>.Failure(
                new ValidationError("Student.BirthdateRequiredForYoung", "Birthdate is required."))
        };
        var viewModel = CreateNewStudentEditorWithActivePlan(service);
        viewModel.ShortName = "Anna";
        viewModel.FullName = "Anna Andersen";
        viewModel.AgeBracket = AgeBracket.Young;

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal("Birthdate is required.", viewModel.ErrorMessage);
        Assert.Equal("Anna", viewModel.ShortName);
        Assert.False(viewModel.WasSaved);
    }

    [Fact]
    public async Task StudentEditorSendsPlanAndScheduleToService()
    {
        var service = new FakeStudentEditorService();
        var viewModel = CreateNewStudentEditorWithActivePlan(service);
        viewModel.ShortName = "Anna";
        viewModel.FullName = "Anna Andersen";
        viewModel.Address = "Testvej 1";
        viewModel.Phone = "12345678";
        viewModel.Email = "anna@example.test";
        viewModel.PlanStartsOnDate = new DateTime(2026, 1, 1);
        viewModel.SchedulePlacements[0].SelectedWeekday =
            viewModel.WeekdayOptions.Single(option => option.Weekday == DayOfWeek.Tuesday);
        viewModel.SchedulePlacements[0].TimeText = "15:00";

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.NotNull(service.LastRequest);
        Assert.Equal("Anna", service.LastRequest.ShortName);
        Assert.Single(service.LastRequest.PlanPeriods);
        Assert.Single(service.LastRequest.Schedule);
        Assert.True(viewModel.WasSaved);
    }

    [Fact]
    public async Task EmbeddedStudentEditorDoesNotRequestHostCloseAfterSave()
    {
        var service = new FakeStudentEditorService();
        var viewModel = StudentEditorViewModel.CreateNew(
            service,
            [CreateCatalogPlan("Standard", PlanCatalogState.Active)],
            requestCloseOnSave: false);
        var closeWasRequested = false;
        viewModel.CloseRequested += (_, _) => closeWasRequested = true;
        viewModel.ShortName = "Anna";
        viewModel.FullName = "Anna Andersen";
        viewModel.Address = "Testvej 1";
        viewModel.Phone = "12345678";
        viewModel.Email = "anna@example.test";

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.True(viewModel.WasSaved);
        Assert.False(closeWasRequested);
    }

    [Fact]
    public async Task StudentEditorCopiesSelectedActiveCatalogPlanIntoSavedSnapshot()
    {
        var service = new FakeStudentEditorService();
        var plan = CreateCatalogPlan(
            "Duo",
            PlanCatalogState.Active,
            PriceWithoutVat: 650m,
            LessonsPerWeek.Two,
            DurationMinutes: 45,
            PlanFlexibility.None,
            NumberOfStudents: 2,
            EffectiveFrom: new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));
        var viewModel = StudentEditorViewModel.CreateNew(service, [plan]);
        viewModel.ShortName = "Anna";
        viewModel.FullName = "Anna Andersen";
        viewModel.Address = "Testvej 1";
        viewModel.Phone = "12345678";
        viewModel.Email = "anna@example.test";

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.NotNull(service.LastRequest);
        var savedPlan = Assert.Single(service.LastRequest.PlanPeriods);
        Assert.Equal("Duo", savedPlan.PlanName);
        Assert.Equal(650m, savedPlan.PriceWithoutVat);
        Assert.Equal(LessonsPerWeek.Two, savedPlan.LessonsPerWeek);
        Assert.Equal(45, savedPlan.DurationMinutes);
        Assert.Equal(PlanFlexibility.None, savedPlan.Flexibility);
        Assert.Equal(2, savedPlan.NumberOfStudents);
        Assert.Equal(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero), savedPlan.PlanEffectiveFrom);
        Assert.Equal("Paid monthly in advance", savedPlan.PaymentTerms);
    }

    [Fact]
    public void StudentEditorFormatsPlanDurationWithMinutes()
    {
        var viewModel = StudentEditorViewModel.CreateNew(
            new FakeStudentEditorService(),
            [CreateCatalogPlan("Standard", PlanCatalogState.Active, DurationMinutes: 60)]);

        Assert.Equal("60 minutes", viewModel.PlanDurationMinutesText);
    }

    [Fact]
    public void StudentEditorFormatsPlanPriceWithKronerSuffix()
    {
        var viewModel = StudentEditorViewModel.CreateNew(
            new FakeStudentEditorService(),
            [CreateCatalogPlan("Standard", PlanCatalogState.Active, PriceWithoutVat: 400m)]);

        Assert.Equal("400 kr.", viewModel.PlanPriceWithoutVatText);
    }

    [Fact]
    public void StudentEditorProvidesScheduleTimeSelectorOptions()
    {
        var viewModel = CreateNewStudentEditorWithActivePlan(new FakeStudentEditorService());
        var placement = Assert.Single(viewModel.SchedulePlacements);

        Assert.Contains("08:00", placement.TimeOptions);
        Assert.Contains("15:00", placement.TimeOptions);
        Assert.Contains("21:30", placement.TimeOptions);
    }

    [Fact]
    public void StudentEditorOffersOnlyActiveCatalogPlansForAssignment()
    {
        var viewModel = StudentEditorViewModel.CreateNew(
            new FakeStudentEditorService(),
            [
                CreateCatalogPlan("Draft", PlanCatalogState.Draft),
                CreateCatalogPlan("Active", PlanCatalogState.Active),
                CreateCatalogPlan("Retired", PlanCatalogState.Retired)
            ]);

        var plan = Assert.Single(viewModel.AssignablePlans);
        Assert.Equal("Active", plan.Name);
        Assert.Equal("Active", viewModel.SelectedAssignablePlan?.Name);
    }

    [Fact]
    public async Task NewStudentRequiresActiveCatalogPlanBeforeSave()
    {
        var service = new FakeStudentEditorService();
        var viewModel = StudentEditorViewModel.CreateNew(service);
        viewModel.ShortName = "Anna";
        viewModel.FullName = "Anna Andersen";
        viewModel.Address = "Testvej 1";
        viewModel.Phone = "12345678";
        viewModel.Email = "anna@example.test";

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.Null(service.LastRequest);
        Assert.True(viewModel.HasError);
        Assert.Equal("An active plan is required before this student can be saved.", viewModel.ErrorMessage);
        Assert.False(viewModel.WasSaved);
    }

    [Fact]
    public async Task StudentEditorDoesNotRewriteExistingSnapshotWhenOpenedWithMatchingCatalogPlan()
    {
        var service = new FakeStudentEditorService();
        var snapshot = new StudentEditorSnapshot(
            7,
            "Anna",
            "Anna Andersen",
            "Testvej 1",
            "12345678",
            "anna@example.test",
            AgeBracket.Standard,
            null,
            true,
            [
                new StudentPlanPeriodInput(
                    "Standard",
                    400m,
                    LessonsPerWeek.One,
                    DurationMinutes: 60,
                    PlanFlexibility.MoveOrCancelByAgreement,
                    NumberOfStudents: 1,
                    PlanEffectiveFrom: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    StartsOn: new DateOnly(2026, 1, 1),
                    EndsOn: null,
                    PaymentTerms: "Paid monthly in advance")
            ],
            []);
        var viewModel = StudentEditorViewModel.FromSnapshot(
            service,
            snapshot,
            [CreateCatalogPlan("Standard", PlanCatalogState.Active, PriceWithoutVat: 900m)]);

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.NotNull(service.LastRequest);
        var savedPlan = Assert.Single(service.LastRequest.PlanPeriods);
        Assert.Equal("Standard", savedPlan.PlanName);
        Assert.Equal(400m, savedPlan.PriceWithoutVat);
        Assert.Equal("Paid monthly in advance", savedPlan.PaymentTerms);
        Assert.Equal("Standard", viewModel.SelectedAssignablePlan?.Name);
    }

    [Fact]
    public async Task StudentEditorAllowsUnspecifiedSchedulePlacement()
    {
        var service = new FakeStudentEditorService();
        var viewModel = CreateNewStudentEditorWithActivePlan(service);
        viewModel.ShortName = "Anna";
        viewModel.FullName = "Anna Andersen";
        viewModel.Address = "Testvej 1";
        viewModel.Phone = "12345678";
        viewModel.Email = "anna@example.test";

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.NotNull(service.LastRequest);
        Assert.Empty(service.LastRequest.Schedule);
        Assert.True(viewModel.WasSaved);
    }

    [Fact]
    public async Task LessonEditorDoesNotSaveInvoicedLesson()
    {
        var service = new FakeLessonEditorService();
        var viewModel = LessonEditorViewModel.EditExisting(
            service,
            lessonId: "lesson-1",
            studentShortName: "Anna",
            startsAt: new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
            isInvoiced: true,
            refreshMonth: new DateOnly(2026, 6, 1));

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.False(service.EditCalled);
        Assert.True(viewModel.HasError);
        Assert.False(viewModel.WasSaved);
    }

    [Fact]
    public async Task LessonEditorShowsCalendarFailureWithoutClearingInput()
    {
        var service = new FakeLessonEditorService
        {
            EditResult = OperationResult<IReadOnlyList<CalendarLesson>>.Failure(
                new ValidationError("Calendar.UpdateFailed", "Google Calendar rejected the update."))
        };
        var viewModel = LessonEditorViewModel.EditExisting(
            service,
            lessonId: "lesson-1",
            studentShortName: "Anna",
            startsAt: new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
            isInvoiced: false,
            refreshMonth: new DateOnly(2026, 6, 1));
        viewModel.DateText = "17-06-2026";

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.True(service.EditCalled);
        Assert.True(viewModel.HasError);
        Assert.Equal("Google Calendar rejected the update.", viewModel.ErrorMessage);
        Assert.Equal("17-06-2026", viewModel.DateText);
        Assert.False(viewModel.WasSaved);
    }

    [Fact]
    public async Task LessonEditorUsesEditedDateOffsetForLocalTimeZone()
    {
        var service = new FakeLessonEditorService();
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        var viewModel = LessonEditorViewModel.EditExisting(
            service,
            lessonId: "lesson-1",
            studentShortName: "Anna",
            startsAt: new DateTimeOffset(2026, 7, 1, 15, 0, 0, TimeSpan.FromHours(2)),
            isInvoiced: false,
            refreshMonth: new DateOnly(2026, 6, 1),
            lessonTimeZone: timeZone);
        viewModel.DateText = "15-01-2026";
        viewModel.TimeText = "15:30";

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.Equal(
            new DateTimeOffset(2026, 1, 15, 15, 30, 0, TimeSpan.FromHours(1)),
            service.LastEditRequest?.StartsAt);
    }

    private static PlanCatalogItem CreateCatalogPlan(
        string name,
        PlanCatalogState state,
        decimal PriceWithoutVat = 400m,
        LessonsPerWeek LessonsPerWeek = LessonsPerWeek.One,
        int DurationMinutes = 60,
        PlanFlexibility Flexibility = PlanFlexibility.MoveOrCancelByAgreement,
        int NumberOfStudents = 1,
        DateTimeOffset? EffectiveFrom = null) =>
        new(
            Id: 1,
            name,
            PriceWithoutVat,
            LessonsPerWeek,
            DurationMinutes,
            Flexibility,
            NumberOfStudents,
            EffectiveFrom ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            PaymentTerms: "Paid monthly in advance",
            RetireOn: null,
            state);

    private static StudentEditorViewModel CreateNewStudentEditorWithActivePlan(FakeStudentEditorService service) =>
        StudentEditorViewModel.CreateNew(service, [CreateCatalogPlan("Standard", PlanCatalogState.Active)]);

    private sealed class FakeStudentEditorService : IStudentEditorService
    {
        public OperationResult<string> SaveResult { get; init; } =
            OperationResult<string>.Success("Anna");

        public SaveStudentRequest? LastRequest { get; private set; }

        public Task<StudentEditorSnapshot?> LoadAsync(string shortName, CancellationToken cancellationToken) =>
            Task.FromResult<StudentEditorSnapshot?>(null);

        public Task<OperationResult<string>> SaveAsync(SaveStudentRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(SaveResult);
        }
    }

    private sealed class FakeLessonEditorService : ILessonEditorService
    {
        public bool EditCalled { get; private set; }

        public LessonEditRequest? LastEditRequest { get; private set; }

        public OperationResult<IReadOnlyList<CalendarLesson>> EditResult { get; init; } =
            OperationResult<IReadOnlyList<CalendarLesson>>.Success([]);

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> AddAsync(
            LessonAddRequest request,
            DateOnly refreshMonth,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IReadOnlyList<CalendarLesson>>.Success([]));

        public Task<OperationResult<PopulateCalendarResult>> PopulateCalendarAsync(
            string studentShortName,
            DateOnly displayMonth,
            DateOnly today,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<PopulateCalendarResult>.Success(new PopulateCalendarResult(0, 0, [])));

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> EditAsync(
            LessonEditRequest request,
            DateOnly refreshMonth,
            CancellationToken cancellationToken)
        {
            EditCalled = true;
            LastEditRequest = request;
            return Task.FromResult(EditResult);
        }

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> DeleteAsync(
            string lessonId,
            DateOnly refreshMonth,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IReadOnlyList<CalendarLesson>>.Success([]));

        public Task<OperationResult<LessonRecord>> UpdatePlanAsync(
            int lessonId,
            int planCatalogId,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<LessonRecord>.Failure(
                new ValidationError("Test.NotImplemented", "This fake does not update lesson plans.")));
    }

}
