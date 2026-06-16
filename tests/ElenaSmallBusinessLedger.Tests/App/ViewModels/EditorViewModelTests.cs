using ElenaSmallBusinessLedger.App.Services;
using ElenaSmallBusinessLedger.App.ViewModels;
using ElenaSmallBusinessLedger.Core.Calendar;
using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Plans;
using ElenaSmallBusinessLedger.Core.Students;
using ElenaSmallBusinessLedger.Core.UseCases.Lessons;
using ElenaSmallBusinessLedger.Core.UseCases.Students;

namespace ElenaSmallBusinessLedger.Tests.App.ViewModels;

public sealed class EditorViewModelTests
{
    [Fact]
    public async Task StudentEditorPreservesInputWhenValidationFails()
    {
        var service = new FakeStudentEditorService
        {
            SaveResult = OperationResult<string>.Failure(
                new ValidationError("Student.BirthdateRequiredForYoung", "Birthdate is required."))
        };
        var viewModel = StudentEditorViewModel.CreateNew(service);
        viewModel.ShortName = "Anna";
        viewModel.FullName = "Anna Andersen";
        viewModel.AgeBracket = AgeBracket.Young;
        viewModel.PlanPriceWithoutVatText = "400";

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
        var viewModel = StudentEditorViewModel.CreateNew(service);
        viewModel.ShortName = "Anna";
        viewModel.FullName = "Anna Andersen";
        viewModel.Address = "Testvej 1";
        viewModel.Phone = "12345678";
        viewModel.Email = "anna@example.test";
        viewModel.PlanName = "Standard";
        viewModel.PlanPriceWithoutVatText = "400";
        viewModel.PlanEffectiveDateText = "01-01-2026";
        viewModel.PlanStartsOnText = "01-01-2026";
        viewModel.NewScheduleWeekday = DayOfWeek.Tuesday;
        viewModel.NewScheduleTimeText = "15:00";
        await viewModel.AddScheduleCommand.ExecuteAsync();

        await viewModel.SaveCommand.ExecuteAsync();

        Assert.NotNull(service.LastRequest);
        Assert.Equal("Anna", service.LastRequest.ShortName);
        Assert.Single(service.LastRequest.PlanPeriods);
        Assert.Single(service.LastRequest.Schedule);
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

        public OperationResult<IReadOnlyList<CalendarLesson>> EditResult { get; init; } =
            OperationResult<IReadOnlyList<CalendarLesson>>.Success([]);

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> AddAsync(
            LessonAddRequest request,
            DateOnly refreshMonth,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IReadOnlyList<CalendarLesson>>.Success([]));

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> EditAsync(
            LessonEditRequest request,
            DateOnly refreshMonth,
            CancellationToken cancellationToken)
        {
            EditCalled = true;
            return Task.FromResult(EditResult);
        }

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> DeleteAsync(
            string lessonId,
            DateOnly refreshMonth,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IReadOnlyList<CalendarLesson>>.Success([]));
    }
}
