using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Company;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.Money;
using MySmallBusinessLedger.Core.Plans;
using MySmallBusinessLedger.Core.Ports;
using MySmallBusinessLedger.Core.Students;
using MySmallBusinessLedger.Core.Time;
using MySmallBusinessLedger.Core.UseCases.Invoices;
using MySmallBusinessLedger.Core.UseCases.Lessons;
using MySmallBusinessLedger.Core.UseCases.Startup;

namespace MySmallBusinessLedger.Tests.Core.UseCases;

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
    public async Task StartupDashboardDoesNotCompleteUninvoicedLessonsByDate()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero));
        var students = new FakeStudentRepository([CreateStudentRecord("Anna")]);
        var calendar = new FakeCalendarGateway(
        [
            new CalendarLesson("past-uninvoiced", "Anna", new DateTimeOffset(2026, 6, 1, 15, 0, 0, TimeSpan.Zero))
        ]);
        var invoices = new FakeInvoiceRepository();
        var query = new StartupDashboardQuery(students, calendar, invoices, clock);

        var result = await query.ExecuteAsync(new DateOnly(2026, 6, 1), "Anna", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Contains(result.Value!.Lessons, lesson =>
            lesson.Id == "past-uninvoiced" &&
            lesson.State == LessonState.Planned &&
            !lesson.IsInvoiced);
    }

    [Fact]
    public async Task StartupDashboardCreatesLocalLessonForUninvoicedCalendarLessonWithActivePlan()
    {
        var startsAt = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var clock = new FakeClock(startsAt);
        var students = new FakeStudentRepository([CreateStudentRecord("Anna")]);
        var calendar = new FakeCalendarGateway([new CalendarLesson("calendar-1", "Anna", startsAt)]);
        var lessons = new FakeLessonRepository();
        var query = new StartupDashboardQuery(
            students,
            calendar,
            new FakeInvoiceRepository(),
            clock,
            lessonRepository: lessons);

        var result = await query.ExecuteAsync(new DateOnly(2026, 6, 1), "Anna", CancellationToken.None);

        Assert.True(result.Succeeded);
        var dashboardLesson = Assert.Single(result.Value!.Lessons);
        Assert.Equal("calendar-1", dashboardLesson.Id);
        Assert.Equal(42, dashboardLesson.PlanCatalogId);
        Assert.Equal("Standard", dashboardLesson.PlanName);
        var savedLesson = Assert.Single(lessons.SavedLessons);
        Assert.Equal("calendar-1", savedLesson.GoogleCalendarEventId);
        Assert.Equal(42, savedLesson.PlanCatalogId);
        Assert.Equal("Standard", savedLesson.PlanName);
    }

    [Fact]
    public async Task StartupDashboardIgnoresCalendarLessonsWithoutKnownActiveStudentWhenUsingLocalLessonLedger()
    {
        var startsAt = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var students = new FakeStudentRepository([]);
        var calendar = new FakeCalendarGateway([new CalendarLesson("calendar-unknown", "Unknown", startsAt)]);
        var lessons = new FakeLessonRepository();
        var query = new StartupDashboardQuery(
            students,
            calendar,
            new FakeInvoiceRepository(),
            new FakeClock(startsAt),
            lessonRepository: lessons);

        var result = await query.ExecuteAsync(new DateOnly(2026, 6, 1), selectedStudentShortName: null, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Null(result.Value!.CurrentStudentShortName);
        Assert.Empty(result.Value.Lessons);
        Assert.Empty(lessons.SavedLessons);
    }

    [Fact]
    public async Task StartupDashboardKeepsInvoicedLocalLessonWhenCalendarEventWasDeleted()
    {
        var startsAt = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var clock = new FakeClock(startsAt);
        var students = new FakeStudentRepository([CreateStudentRecord("Anna")]);
        var lessons = new FakeLessonRepository();
        await lessons.SaveAsync(new LessonRecord(
            Id: 0,
            StudentId: 1,
            StudentShortName: "Anna",
            PlanCatalogId: 42,
            PlanName: "Standard",
            StartsAt: startsAt,
            DurationMinutes: 60,
            State: LessonState.Planned,
            GoogleCalendarEventId: "calendar-deleted",
            InvoiceNumber: new InvoiceNumber(7)),
            CancellationToken.None);
        var query = new StartupDashboardQuery(
            students,
            new FakeCalendarGateway([]),
            new FakeInvoiceRepository
            {
                LessonStatuses =
                [
                    new InvoiceLessonStatus("Anna", startsAt, PaymentStatus.Outstanding, new InvoiceNumber(7), 1)
                ]
            },
            clock,
            lessonRepository: lessons);

        var result = await query.ExecuteAsync(new DateOnly(2026, 6, 1), "Anna", CancellationToken.None);

        Assert.True(result.Succeeded);
        var dashboardLesson = Assert.Single(result.Value!.Lessons);
        Assert.Equal("calendar-deleted", dashboardLesson.Id);
        Assert.True(dashboardLesson.IsInvoiced);
        Assert.Equal(LessonState.Completed, dashboardLesson.State);
        Assert.Equal(new InvoiceNumber(7), dashboardLesson.InvoiceNumber);
        Assert.Equal(PaymentStatus.Outstanding, dashboardLesson.PaymentStatus);
    }

    [Fact]
    public async Task StartupDashboardDoesNotApplyInvoiceStatusToSeparateLessonOnSameDate()
    {
        var startsAt = new DateTimeOffset(2026, 5, 1, 14, 0, 0, TimeSpan.Zero);
        var clock = new FakeClock(startsAt);
        var students = new FakeStudentRepository([CreateStudentRecord("Anna")]);
        var lessons = new FakeLessonRepository();
        await lessons.SaveAsync(new LessonRecord(
            Id: 0,
            StudentId: 1,
            StudentShortName: "Anna",
            PlanCatalogId: 1,
            PlanName: "Prøvetime",
            StartsAt: startsAt,
            DurationMinutes: 30,
            State: LessonState.Completed,
            GoogleCalendarEventId: "calendar-trial",
            InvoiceNumber: new InvoiceNumber(1)),
            CancellationToken.None);
        var calendar = new FakeCalendarGateway(
        [
            new CalendarLesson("calendar-standard", "Anna", startsAt, LessonState.Planned)
        ]);
        var invoices = new FakeInvoiceRepository
        {
            LessonStatuses =
            [
                new InvoiceLessonStatus("Anna", startsAt, PaymentStatus.Paid, new InvoiceNumber(1), 1)
            ]
        };
        var query = new StartupDashboardQuery(
            students,
            calendar,
            invoices,
            clock,
            lessonRepository: lessons);

        var result = await query.ExecuteAsync(new DateOnly(2026, 5, 1), "Anna", CancellationToken.None);

        Assert.True(result.Succeeded);
        var newLesson = Assert.Single(result.Value!.Lessons, lesson => lesson.Id == "calendar-standard");
        Assert.Equal(LessonState.Planned, newLesson.State);
        Assert.False(newLesson.IsInvoiced);
        Assert.Null(newLesson.InvoiceNumber);
        Assert.Null(newLesson.PaymentStatus);
        Assert.Null(lessons.SavedLessons.Single(lesson => lesson.GoogleCalendarEventId == "calendar-standard").InvoiceNumber);
    }

    [Fact]
    public async Task StartupDashboardDoesNotTreatLocalInvoiceNumberAsIssuedWithoutInvoiceLineStatus()
    {
        var startsAt = new DateTimeOffset(2026, 5, 1, 14, 0, 0, TimeSpan.Zero);
        var clock = new FakeClock(startsAt);
        var students = new FakeStudentRepository([CreateStudentRecord("Anna")]);
        var lessons = new FakeLessonRepository();
        await lessons.SaveAsync(new LessonRecord(
            Id: 0,
            StudentId: 1,
            StudentShortName: "Anna",
            PlanCatalogId: 2,
            PlanName: "Standard",
            StartsAt: startsAt,
            DurationMinutes: 60,
            State: LessonState.Planned,
            GoogleCalendarEventId: "calendar-standard",
            InvoiceNumber: new InvoiceNumber(1)),
            CancellationToken.None);
        var calendar = new FakeCalendarGateway(
        [
            new CalendarLesson("calendar-standard", "Anna", startsAt, LessonState.Planned)
        ]);
        var query = new StartupDashboardQuery(
            students,
            calendar,
            new FakeInvoiceRepository(),
            clock,
            lessonRepository: lessons);

        var result = await query.ExecuteAsync(new DateOnly(2026, 5, 1), "Anna", CancellationToken.None);

        Assert.True(result.Succeeded);
        var lesson = Assert.Single(result.Value!.Lessons);
        Assert.Equal(LessonState.Planned, lesson.State);
        Assert.False(lesson.IsInvoiced);
        Assert.Null(lesson.InvoiceNumber);
        Assert.Null(lesson.PaymentStatus);
        Assert.Null(Assert.Single(lessons.SavedLessons).InvoiceNumber);
    }

    [Fact]
    public async Task StartupDashboardIgnoresCalendarEditsForInvoicedLocalLesson()
    {
        var invoicedStartsAt = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var editedCalendarStartsAt = new DateTimeOffset(2026, 6, 19, 12, 0, 0, TimeSpan.Zero);
        var clock = new FakeClock(invoicedStartsAt);
        var students = new FakeStudentRepository([CreateStudentRecord("Anna")]);
        var lessons = new FakeLessonRepository();
        await lessons.SaveAsync(new LessonRecord(
            Id: 0,
            StudentId: 1,
            StudentShortName: "Anna",
            PlanCatalogId: 42,
            PlanName: "Standard",
            StartsAt: invoicedStartsAt,
            DurationMinutes: 60,
            State: LessonState.Completed,
            GoogleCalendarEventId: "calendar-1",
            InvoiceNumber: new InvoiceNumber(7)),
            CancellationToken.None);
        var calendar = new FakeCalendarGateway(
        [
            new CalendarLesson("calendar-1", "Anna", editedCalendarStartsAt, LessonState.Planned)
        ]);
        var query = new StartupDashboardQuery(
            students,
            calendar,
            new FakeInvoiceRepository
            {
                LessonStatuses =
                [
                    new InvoiceLessonStatus("Anna", invoicedStartsAt, PaymentStatus.Outstanding, new InvoiceNumber(7), 1)
                ]
            },
            clock,
            lessonRepository: lessons);

        var result = await query.ExecuteAsync(new DateOnly(2026, 6, 1), "Anna", CancellationToken.None);

        Assert.True(result.Succeeded);
        var dashboardLesson = Assert.Single(result.Value!.Lessons);
        Assert.Equal(invoicedStartsAt, dashboardLesson.StartsAt);
        Assert.True(dashboardLesson.IsInvoiced);
        Assert.Equal(LessonState.Completed, dashboardLesson.State);
        Assert.Equal(invoicedStartsAt, Assert.Single(lessons.SavedLessons).StartsAt);
    }

    [Fact]
    public async Task StartupDashboardRemovesUninvoicedLocalLessonWhenCalendarEventWasDeleted()
    {
        var startsAt = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var clock = new FakeClock(startsAt);
        var students = new FakeStudentRepository([CreateStudentRecord("Anna")]);
        var lessons = new FakeLessonRepository();
        var saved = await lessons.SaveAsync(CreateLessonRecord("Anna", startsAt), CancellationToken.None);
        var query = new StartupDashboardQuery(
            students,
            new FakeCalendarGateway([]),
            new FakeInvoiceRepository(),
            clock,
            lessonRepository: lessons);

        var result = await query.ExecuteAsync(new DateOnly(2026, 6, 1), "Anna", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!.Lessons);
        Assert.Equal([saved.Id], lessons.DeletedLessonIds);
        Assert.Empty(lessons.SavedLessons);
    }

    [Fact]
    public async Task StartupDashboardNavigationFollowsCalendarLessonOrderAndRepeatsStudents()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));
        var students = new FakeStudentRepository(
        [
            CreateStudentRecord("Anna", []),
            CreateStudentRecord("Bo", [])
        ]);
        var calendar = new FakeCalendarGateway(
        [
            new CalendarLesson("anna-2", "Anna", new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero)),
            new CalendarLesson("bo-1", "Bo", new DateTimeOffset(2026, 6, 8, 14, 0, 0, TimeSpan.Zero)),
            new CalendarLesson("anna-1", "Anna", new DateTimeOffset(2026, 6, 9, 15, 0, 0, TimeSpan.Zero))
        ]);
        var query = new StartupDashboardQuery(students, calendar, new FakeInvoiceRepository(), clock, TimeZoneInfo.Utc);

        var result = await query.ExecuteAsync(new DateOnly(2026, 6, 1), "Anna", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["Bo", "Anna", "Anna"],
            result.Value!.StudentNavigationEntries.Select(entry => entry.StudentShortName).ToArray());
        Assert.Equal(
            [
                new DateTimeOffset(2026, 6, 8, 14, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 9, 15, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero)
            ],
            result.Value.StudentNavigationEntries.Select(entry => entry.StartsAt).ToArray());
    }

    [Fact]
    public async Task StartupDashboardNavigationIncludesAdjacentMonthCalendarLessons()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));
        var students = new FakeStudentRepository(
        [
            CreateStudentRecord("Anna", []),
            CreateStudentRecord("Bo", [])
        ]);
        var calendar = new FakeCalendarGateway(
        [
            new CalendarLesson("anna-june", "Anna", new DateTimeOffset(2026, 6, 29, 15, 0, 0, TimeSpan.Zero)),
            new CalendarLesson("bo-july", "Bo", new DateTimeOffset(2026, 7, 6, 14, 0, 0, TimeSpan.Zero))
        ]);
        var query = new StartupDashboardQuery(students, calendar, new FakeInvoiceRepository(), clock);

        var result = await query.ExecuteAsync(new DateOnly(2026, 6, 1), "Anna", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["Anna", "Bo"],
            result.Value!.StudentNavigationEntries.Select(entry => entry.StudentShortName).ToArray());
        Assert.Equal(
            [
                new DateTimeOffset(2026, 6, 29, 15, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 6, 14, 0, 0, TimeSpan.Zero)
            ],
            result.Value.StudentNavigationEntries.Select(entry => entry.StartsAt).ToArray());
    }

    [Fact]
    public async Task StartupDashboardNavigationFallsBackToDefaultSchedulesWhenMonthHasNoCalendarLessons()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));
        var students = new FakeStudentRepository(
        [
            CreateStudentRecord("Anna", [new StudentScheduleEntry(DayOfWeek.Tuesday, new TimeOnly(15, 0))]),
            CreateStudentRecord("Bo", [new StudentScheduleEntry(DayOfWeek.Monday, new TimeOnly(14, 0))])
        ]);
        var query = new StartupDashboardQuery(students, new FakeCalendarGateway([]), new FakeInvoiceRepository(), clock, TimeZoneInfo.Utc);

        var result = await query.ExecuteAsync(new DateOnly(2026, 6, 1), "Anna", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["Bo", "Anna", "Bo", "Anna"],
            result.Value!.StudentNavigationEntries.Take(4).Select(entry => entry.StudentShortName).ToArray());
        Assert.Equal(
            [
                new DateTimeOffset(2026, 6, 1, 14, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 2, 15, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 8, 14, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 9, 15, 0, 0, TimeSpan.Zero)
            ],
            result.Value.StudentNavigationEntries.Take(4).Select(entry => entry.StartsAt).ToArray());
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
    public async Task PopulateCalendarCurrentMonthStartsTodayAndSkipsExactDuplicates()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        var existingDuplicate = new CalendarLesson(
            "existing",
            "Anna",
            new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.FromHours(2)));
        var students = new FakeStudentRepository([CreateStudentRecord("Anna")]);
        var calendar = new FakeCalendarGateway([existingDuplicate]);
        var useCase = new PopulateCalendarUseCase(students, calendar, timeZone);

        var result = await useCase.ExecuteAsync(
            new PopulateCalendarRequest("Anna", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 16)),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Value!.CreatedCount);
        Assert.Equal(1, result.Value.SkippedDuplicateCount);
        Assert.Equal(
            [
                new DateTimeOffset(2026, 6, 23, 15, 0, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2026, 6, 30, 15, 0, 0, TimeSpan.FromHours(2))
            ],
            calendar.PopulatedRequests.Select(request => request.StartsAt).ToArray());
    }

    [Fact]
    public async Task PopulateCalendarFutureMonthSeedsWholeMonth()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        var students = new FakeStudentRepository([CreateStudentRecord("Anna")]);
        var calendar = new FakeCalendarGateway([]);
        var useCase = new PopulateCalendarUseCase(students, calendar, timeZone);

        var result = await useCase.ExecuteAsync(
            new PopulateCalendarRequest("Anna", new DateOnly(2026, 7, 1), new DateOnly(2026, 6, 16)),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(4, result.Value!.CreatedCount);
        Assert.Equal(
            [
                new DateTimeOffset(2026, 7, 7, 15, 0, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2026, 7, 14, 15, 0, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2026, 7, 21, 15, 0, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2026, 7, 28, 15, 0, 0, TimeSpan.FromHours(2))
            ],
            calendar.PopulatedRequests.Select(request => request.StartsAt).ToArray());
    }

    [Fact]
    public async Task PopulateCalendarPastMonthSeedsWholeMonth()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        var students = new FakeStudentRepository([CreateStudentRecord("Anna")]);
        var calendar = new FakeCalendarGateway([]);
        var useCase = new PopulateCalendarUseCase(students, calendar, timeZone);

        var result = await useCase.ExecuteAsync(
            new PopulateCalendarRequest("Anna", new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 16)),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                new DateTimeOffset(2026, 5, 5, 15, 0, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2026, 5, 12, 15, 0, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2026, 5, 19, 15, 0, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2026, 5, 26, 15, 0, 0, TimeSpan.FromHours(2))
            ],
            calendar.PopulatedRequests.Select(request => request.StartsAt).ToArray());
    }

    [Fact]
    public async Task PopulateCalendarRequiresSavedStudentSchedule()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        var studentWithoutSchedule = CreateStudentRecord("Anna") with { Schedule = [] };
        var students = new FakeStudentRepository([studentWithoutSchedule]);
        var calendar = new FakeCalendarGateway([]);
        var useCase = new PopulateCalendarUseCase(students, calendar, timeZone);

        var result = await useCase.ExecuteAsync(
            new PopulateCalendarRequest("Anna", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 16)),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "CalendarPopulate.NoSchedule");
        Assert.Empty(calendar.PopulatedRequests);
    }

    [Fact]
    public async Task PopulateCalendarReportsGoogleCalendarWriteFailure()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        var students = new FakeStudentRepository([CreateStudentRecord("Anna")]);
        var calendar = new FakeCalendarGateway([])
        {
            UpdateResult = OperationResult<IReadOnlyList<CalendarLesson>>.Failure(
                new ValidationError("Calendar.UpdateFailed", "Google Calendar could not be updated."))
        };
        var useCase = new PopulateCalendarUseCase(students, calendar, timeZone);

        var result = await useCase.ExecuteAsync(
            new PopulateCalendarRequest("Anna", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 16)),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Calendar.UpdateFailed");
        Assert.NotEmpty(calendar.PopulatedRequests);
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
    public async Task LessonStateToggleUpdatesCalendarAndRequiresConfirmationWhenReturningCompletedToPlanned()
    {
        var calendar = new FakeCalendarGateway([]);
        var useCase = new ToggleLessonStateUseCase(calendar);

        var completed = await useCase.ExecuteAsync(
            "lesson-1",
            LessonState.Planned,
            confirmReturnToPlanned: false,
            CancellationToken.None);
        var unconfirmedPlanned = await useCase.ExecuteAsync(
            "lesson-1",
            LessonState.Completed,
            confirmReturnToPlanned: false,
            CancellationToken.None);
        var confirmedPlanned = await useCase.ExecuteAsync(
            "lesson-1",
            LessonState.Completed,
            confirmReturnToPlanned: true,
            CancellationToken.None);

        Assert.True(completed.Succeeded);
        Assert.Equal(LessonState.Completed, completed.Value);
        Assert.True(unconfirmedPlanned.RequiresConfirmation);
        Assert.True(confirmedPlanned.Succeeded);
        Assert.Equal(LessonState.Planned, confirmedPlanned.Value);
        Assert.Equal([LessonState.Completed, LessonState.Planned], calendar.SavedLessonStates);
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
    public async Task InvoiceCandidateSelectionPreselectsLessonActiveAtCurrentTime()
    {
        var students = new FakeStudentRepository([CreateStudentRecord("Anna")]);
        var calendar = new FakeCalendarGateway(
        [
            new CalendarLesson("anna-today", "Anna", new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero))
        ]);
        var invoices = new FakeInvoiceRepository();
        var useCase = new InvoiceCandidateSelectionUseCase(
            students,
            calendar,
            invoices,
            lessonDuration: TimeSpan.FromMinutes(60));

        var result = await useCase.ExecuteAsync(
            new DateOnly(2026, 6, 16),
            new DateTimeOffset(2026, 6, 16, 15, 25, 0, TimeSpan.Zero),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Anna", result.Value!.PreselectedStudentShortName);
    }

    [Fact]
    public async Task InvoiceCandidateSelectionFallsBackToNextLessonWithinThirtyMinutesWhenCurrentStudentIsAlreadyInvoiced()
    {
        var students = new FakeStudentRepository([CreateStudentRecord("Anna"), CreateStudentRecord("Bo")]);
        var calendar = new FakeCalendarGateway(
        [
            new CalendarLesson("anna-today", "Anna", new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero)),
            new CalendarLesson("bo-today", "Bo", new DateTimeOffset(2026, 6, 16, 15, 25, 0, TimeSpan.Zero)),
            new CalendarLesson("cara-today", "Cara", new DateTimeOffset(2026, 6, 16, 16, 0, 0, TimeSpan.Zero))
        ]);
        var invoices = new FakeInvoiceRepository { StudentsWithInvoiceThisMonth = ["Anna"] };
        var useCase = new InvoiceCandidateSelectionUseCase(
            students,
            calendar,
            invoices,
            lessonDuration: TimeSpan.FromMinutes(60));

        var result = await useCase.ExecuteAsync(
            new DateOnly(2026, 6, 16),
            new DateTimeOffset(2026, 6, 16, 15, 10, 0, TimeSpan.Zero),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Bo", result.Value!.PreselectedStudentShortName);
    }

    [Fact]
    public async Task InvoiceCandidateSelectionUsesLocalUninvoicedLessonsEvenWhenStudentAlreadyHasSameMonthInvoice()
    {
        var startsAt = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var students = new FakeStudentRepository([CreateStudentRecord("Anna"), CreateStudentRecord("Bo")]);
        var lessons = new FakeLessonRepository();
        await lessons.SaveAsync(CreateLessonRecord("Anna", startsAt), CancellationToken.None);
        await lessons.SaveAsync(CreateLessonRecord("Bo", startsAt, invoiceNumber: new InvoiceNumber(3)), CancellationToken.None);
        var invoices = new FakeInvoiceRepository { StudentsWithInvoiceThisMonth = ["Anna", "Bo"] };
        var useCase = new InvoiceCandidateSelectionUseCase(
            students,
            new FakeCalendarGateway([]),
            invoices,
            lessonRepository: lessons);

        var result = await useCase.ExecuteAsync(
            new DateOnly(2026, 6, 16),
            startsAt,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(["Anna"], result.Value!.CandidateStudentShortNames);
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
        Assert.Equal(42, result.Value.Draft.Lines[0].PlanCatalogId);
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
    public async Task InvoicePreviewCreatesDraftFromLocalUninvoicedLessonsAndSelectedPlan()
    {
        var startsAt = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var students = new FakeStudentRepository([CreateStudentRecord("Anna")]);
        var company = new FakeCompanyProfileRepository();
        var lessons = new FakeLessonRepository();
        await lessons.SaveAsync(CreateLessonRecord("Anna", startsAt, planCatalogId: 77, planName: "Proevetime"), CancellationToken.None);
        await lessons.SaveAsync(CreateLessonRecord("Anna", startsAt.AddDays(7), invoiceNumber: new InvoiceNumber(4)), CancellationToken.None);
        var plans = new FakePlanCatalogRepository(
        [
            CreatePlanCatalogItem(77, "Proevetime", 250m)
        ]);
        var useCase = new InvoicePreviewUseCase(
            students,
            company,
            new FakeCalendarGateway([]),
            lessonRepository: lessons,
            planCatalogRepository: plans);

        var result = await useCase.CreateAsync(
            "Anna",
            new DateOnly(2026, 6, 16),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 30),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        var line = Assert.Single(result.Value!.Draft.Lines);
        Assert.Equal(1, line.LessonId);
        Assert.Equal("Proevetime", line.PlanName);
        Assert.Equal(77, line.PlanCatalogId);
        Assert.Equal(Money.Dkk(250m), line.PriceWithoutVat);
    }

    [Fact]
    public async Task IssueInvoiceDoesNotFinalizeInvoiceOrPrintWhenPdfWriteFails()
    {
        var calls = new List<string>();
        var company = new FakeCompanyProfileRepository();
        var invoices = new FakeInvoiceRepository(calls);
        var fileStore = new FakeInvoiceFileStore(calls);
        var pdfWriter = new FakeInvoicePdfWriter
        {
            Calls = calls,
            WriteResult = OperationResult<string>.Failure(
                new ValidationError("InvoicePdf.WriteFailed", "The PDF could not be written."))
        };
        var printer = new FakeInvoicePrinter(calls);
        var useCase = new IssueInvoiceUseCase(
            company,
            invoices,
            fileStore,
            pdfWriter,
            printer,
            new FakeClock(new DateTimeOffset(2026, 6, 16, 17, 30, 0, TimeSpan.Zero)));

        var result = await useCase.ExecuteAsync(CreateInvoiceDraft(), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.True(invoices.ReserveCalled);
        Assert.Equal(
            [
                "reserve",
                "stage:001",
                "write:C:\\Invoices\\.staging\\001.pdf",
                "discard:C:\\Invoices\\.staging\\001.pdf",
                "release:001"
            ],
            calls);
        Assert.True(invoices.ReleaseReservationCalled);
        Assert.False(invoices.FinalizeCalled);
        Assert.False(invoices.UpdatePrintStatusCalled);
        Assert.False(printer.PrintCalled);
    }

    [Fact]
    public async Task IssueInvoiceReservesFinalizesPrintsAndRecordsPrintedStatusInOrder()
    {
        var calls = new List<string>();
        var company = new FakeCompanyProfileRepository();
        var invoices = new FakeInvoiceRepository(calls);
        var fileStore = new FakeInvoiceFileStore(calls);
        var pdfWriter = new FakeInvoicePdfWriter { Calls = calls };
        var printer = new FakeInvoicePrinter(calls);
        var useCase = new IssueInvoiceUseCase(
            company,
            invoices,
            fileStore,
            pdfWriter,
            printer,
            new FakeClock(new DateTimeOffset(2026, 6, 16, 17, 30, 0, TimeSpan.Zero)));

        var result = await useCase.ExecuteAsync(CreateInvoiceDraft(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                "reserve",
                "stage:001",
                "write:C:\\Invoices\\.staging\\001.pdf",
                "verify:C:\\Invoices\\.staging\\001.pdf",
                "publish:C:\\Invoices\\.staging\\001.pdf",
                "finalize:C:\\Invoices\\001.pdf",
                "print:C:\\Invoices\\001.pdf:Office Printer",
                "print-status:Printed:2026-06-16T17:30:00.0000000+00:00:"
            ],
            calls);
        Assert.Equal(InvoiceIssueStatus.Issued, result.Value!.Invoice.IssueStatus);
        Assert.Equal(InvoicePrintStatus.Printed, result.Value.Invoice.PrintStatus);
        Assert.Equal(new DateTimeOffset(2026, 6, 16, 17, 30, 0, TimeSpan.Zero), result.Value.Invoice.PrintedAt);
        Assert.Equal("C:\\Invoices\\001.pdf", result.Value!.Invoice.PdfFilePath);
    }

    [Fact]
    public async Task IssueInvoiceLocksLocalLessonsAfterFinalPdfPublish()
    {
        var calls = new List<string>();
        var lessons = new FakeLessonRepository();
        var company = new FakeCompanyProfileRepository();
        var invoices = new FakeInvoiceRepository(calls);
        var fileStore = new FakeInvoiceFileStore(calls);
        var pdfWriter = new FakeInvoicePdfWriter { Calls = calls };
        var printer = new FakeInvoicePrinter(calls);
        var useCase = new IssueInvoiceUseCase(
            company,
            invoices,
            fileStore,
            pdfWriter,
            printer,
            new FakeClock(new DateTimeOffset(2026, 6, 16, 17, 30, 0, TimeSpan.Zero)),
            lessons);

        var result = await useCase.ExecuteAsync(CreateInvoiceDraft(lessonId: 12), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal([12], lessons.LockedLessonIds);
        Assert.Equal(new InvoiceNumber(1), lessons.LockedInvoiceNumber);
        Assert.Contains(calls, call => call == "finalize:C:\\Invoices\\001.pdf");
    }

    [Fact]
    public async Task UpdateLessonPlanChangesOnlyUninvoicedLocalLesson()
    {
        var lessonRepository = new FakeLessonRepository();
        var savedLesson = await lessonRepository.SaveAsync(
            CreateLessonRecord("Anna", new DateTimeOffset(2026, 6, 4, 16, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);
        var planRepository = new FakePlanCatalogRepository(
        [
            CreatePlanCatalogItem(77, "Proevetime", 250m)
        ]);
        var useCase = new UpdateLessonPlanUseCase(lessonRepository, planRepository);

        var result = await useCase.ExecuteAsync(savedLesson.Id, 77, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(77, result.Value!.PlanCatalogId);
        Assert.Equal("Proevetime", result.Value.PlanName);
        Assert.Equal(77, lessonRepository.SavedLessons.Single().PlanCatalogId);
    }

    [Fact]
    public async Task UpdateLessonPlanRejectsInvoicedLocalLesson()
    {
        var lessonRepository = new FakeLessonRepository();
        var savedLesson = await lessonRepository.SaveAsync(
            CreateLessonRecord(
                "Anna",
                new DateTimeOffset(2026, 6, 4, 16, 0, 0, TimeSpan.Zero),
                invoiceNumber: new InvoiceNumber(5)),
            CancellationToken.None);
        var planRepository = new FakePlanCatalogRepository(
        [
            CreatePlanCatalogItem(77, "Proevetime", 250m)
        ]);
        var useCase = new UpdateLessonPlanUseCase(lessonRepository, planRepository);

        var result = await useCase.ExecuteAsync(savedLesson.Id, 77, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Lesson.PlanLocked", Assert.Single(result.Errors).Code);
        Assert.Equal(42, lessonRepository.SavedLessons.Single().PlanCatalogId);
    }

    [Fact]
    public async Task IssueInvoiceReturnsIssuedInvoiceWithPrintFailedStatusWhenPrintingFails()
    {
        var calls = new List<string>();
        var company = new FakeCompanyProfileRepository();
        var invoices = new FakeInvoiceRepository(calls);
        var fileStore = new FakeInvoiceFileStore(calls);
        var pdfWriter = new FakeInvoicePdfWriter { Calls = calls };
        var printer = new FakeInvoicePrinter(calls)
        {
            PrintResult = OperationResult<string>.Failure(new ValidationError(
                "InvoicePrint.Failed",
                "The invoice could not be printed."))
        };
        var useCase = new IssueInvoiceUseCase(
            company,
            invoices,
            fileStore,
            pdfWriter,
            printer,
            new FakeClock(new DateTimeOffset(2026, 6, 16, 17, 30, 0, TimeSpan.Zero)));

        var result = await useCase.ExecuteAsync(CreateInvoiceDraft(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(InvoiceIssueStatus.Issued, result.Value!.Invoice.IssueStatus);
        Assert.Equal(InvoicePrintStatus.PrintFailed, result.Value.Invoice.PrintStatus);
        Assert.Null(result.Value.Invoice.PrintedAt);
        Assert.Equal("The invoice could not be printed.", result.Value.Invoice.LastPrintError);
        Assert.False(result.Value.PrintSucceeded);
        Assert.Contains(result.Value.PrintErrors, error => error.Code == "InvoicePrint.Failed");
    }

    [Fact]
    public async Task IssueInvoiceReleasesReservedInvoiceNumberWhenPdfPublishFails()
    {
        var calls = new List<string>();
        var company = new FakeCompanyProfileRepository();
        var invoices = new FakeInvoiceRepository(calls);
        var fileStore = new FakeInvoiceFileStore(calls)
        {
            PublishResult = OperationResult<string>.Failure(new ValidationError(
                "InvoicePdf.FinalFileAlreadyExists",
                "An invoice PDF already exists for this invoice number."))
        };
        var pdfWriter = new FakeInvoicePdfWriter { Calls = calls };
        var printer = new FakeInvoicePrinter(calls);
        var useCase = new IssueInvoiceUseCase(
            company,
            invoices,
            fileStore,
            pdfWriter,
            printer,
            new FakeClock(new DateTimeOffset(2026, 6, 16, 17, 30, 0, TimeSpan.Zero)));

        var result = await useCase.ExecuteAsync(CreateInvoiceDraft(), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "InvoicePdf.FinalFileAlreadyExists");
        Assert.Equal(
            [
                "reserve",
                "stage:001",
                "write:C:\\Invoices\\.staging\\001.pdf",
                "verify:C:\\Invoices\\.staging\\001.pdf",
                "publish:C:\\Invoices\\.staging\\001.pdf",
                "discard:C:\\Invoices\\.staging\\001.pdf",
                "release:001"
            ],
            calls);
        Assert.True(invoices.ReleaseReservationCalled);
        Assert.False(invoices.FinalizeCalled);
        Assert.False(printer.PrintCalled);
    }

    [Fact]
    public async Task ReprintIssuedInvoiceUsesStoredPdfAndUpdatesOnlyPrintStatus()
    {
        var calls = new List<string>();
        var invoice = CreateInvoiceDraft().Issue(
            new InvoiceNumber(7),
            CreateInvoiceDraft().CalculateTotals(CreateCompanyProfile()),
            "C:\\Invoices\\007.pdf",
            InvoiceIssueStatus.Issued,
            InvoicePrintStatus.PrintFailed,
            printedAt: null,
            lastPrintError: "Previous printer error.");
        var company = new FakeCompanyProfileRepository();
        var invoices = new FakeInvoiceRepository(calls)
        {
            IssuedInvoiceToReturn = invoice
        };
        var printer = new FakeInvoicePrinter(calls);
        var useCase = new ReprintIssuedInvoiceUseCase(
            company,
            invoices,
            printer,
            new FakeClock(new DateTimeOffset(2026, 6, 16, 18, 0, 0, TimeSpan.Zero)));

        var result = await useCase.ExecuteAsync(new InvoiceNumber(7), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(invoices.ReserveCalled);
        Assert.False(invoices.FinalizeCalled);
        Assert.Equal(
            [
                "print:C:\\Invoices\\007.pdf:Office Printer",
                "print-status:Printed:2026-06-16T18:00:00.0000000+00:00:"
            ],
            calls);
        Assert.Equal(new InvoiceNumber(7), result.Value!.Invoice.InvoiceNumber);
        Assert.Equal("C:\\Invoices\\007.pdf", result.Value.Invoice.PdfFilePath);
        Assert.Equal(InvoicePrintStatus.Printed, result.Value.Invoice.PrintStatus);
        Assert.Null(result.Value.Invoice.LastPrintError);
    }

    [Fact]
    public async Task ReprintIssuedInvoiceRecordsPrintFailureWithoutChangingIssueState()
    {
        var invoice = CreateInvoiceDraft().Issue(
            new InvoiceNumber(7),
            CreateInvoiceDraft().CalculateTotals(CreateCompanyProfile()),
            "C:\\Invoices\\007.pdf",
            InvoiceIssueStatus.Issued,
            InvoicePrintStatus.Printed,
            printedAt: new DateTimeOffset(2026, 6, 15, 18, 0, 0, TimeSpan.Zero),
            lastPrintError: null);
        var company = new FakeCompanyProfileRepository();
        var invoices = new FakeInvoiceRepository
        {
            IssuedInvoiceToReturn = invoice
        };
        var printer = new FakeInvoicePrinter
        {
            PrintResult = OperationResult<string>.Failure(new ValidationError(
                "InvoicePrinter.Unavailable",
                "The configured invoice printer is not available."))
        };
        var useCase = new ReprintIssuedInvoiceUseCase(
            company,
            invoices,
            printer,
            new FakeClock(new DateTimeOffset(2026, 6, 16, 18, 0, 0, TimeSpan.Zero)));

        var result = await useCase.ExecuteAsync(new InvoiceNumber(7), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(InvoiceIssueStatus.Issued, result.Value!.Invoice.IssueStatus);
        Assert.Equal(InvoicePrintStatus.PrintFailed, result.Value.Invoice.PrintStatus);
        Assert.Null(result.Value.Invoice.PrintedAt);
        Assert.Equal("The configured invoice printer is not available.", result.Value.Invoice.LastPrintError);
        Assert.False(result.Value.PrintSucceeded);
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

    private static InvoiceDraft CreateInvoiceDraft(int lessonId = 0) =>
        InvoiceDraft.Create(
            invoiceDate: new DateOnly(2026, 6, 16),
            companySnapshot: new CompanySnapshot("Maple Key Studio", "Musikvej 1", null),
            studentSnapshot: new StudentSnapshot("Anna", "Anna Andersen", "Testvej 1"),
            lines:
            [
                new InvoiceLineDraft(
                    lessonDateTime: new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
                    planName: "Standard",
                    priceWithoutVat: Money.Dkk(400m),
                    lessonId: lessonId)
            ],
            priceAdjustment: null);

    private static LessonRecord CreateLessonRecord(
        string studentShortName,
        DateTimeOffset startsAt,
        int studentId = 1,
        int planCatalogId = 42,
        string planName = "Standard",
        InvoiceNumber? invoiceNumber = null) =>
        new(
            Id: 0,
            StudentId: studentId,
            StudentShortName: studentShortName,
            PlanCatalogId: planCatalogId,
            PlanName: planName,
            StartsAt: startsAt,
            DurationMinutes: 60,
            State: LessonState.Planned,
            GoogleCalendarEventId: $"{studentShortName}-{startsAt:yyyyMMddHHmm}",
            InvoiceNumber: invoiceNumber);

    private static PlanCatalogItem CreatePlanCatalogItem(int id, string name, decimal priceWithoutVat) =>
        new(
            id,
            name,
            priceWithoutVat,
            LessonsPerWeek.One,
            60,
            PlanFlexibility.MoveOrCancelByAgreement,
            1,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            "Paid per lesson",
            RetireOn: null,
            PlanCatalogState.Active);

    private static CompanyProfile CreateCompanyProfile() =>
        CompanyProfile.Create(
            "Maple Key Studio",
            "Musikvej 1",
            "12345678",
            "elena@example.test",
            cvr: null,
            vatRegistrationDate: null,
            VatStatus.NotRegistered,
            "C:\\Invoices",
            configuredInvoicePrinterName: "Office Printer");

    private static StudentRecord CreateStudentRecord(
        string shortName,
        IReadOnlyList<StudentScheduleEntry>? schedule = null)
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
            effectiveFrom: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            planCatalogId: 42);

        return new StudentRecord(
            0,
            student,
            [new StudentPlanPeriod(plan, new DateOnly(2026, 1, 1), EndsOn: null)],
            schedule ?? [new StudentScheduleEntry(DayOfWeek.Tuesday, new TimeOnly(15, 0))]);
    }

    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now => now;
    }

    private sealed class FakeStudentRepository(IReadOnlyList<StudentRecord> students) : IStudentRepository
    {
        public Task<IReadOnlyList<StudentRecord>> ListStudentsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(students);

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
                "C:\\Invoices",
                configuredInvoicePrinterName: "Office Printer"));

        public Task SaveAsync(CompanyProfile profile, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeLessonRepository : ILessonRepository
    {
        private readonly List<LessonRecord> lessons = [];

        public IReadOnlyList<LessonRecord> SavedLessons => lessons;

        public IReadOnlyList<int> LockedLessonIds { get; private set; } = [];

        public InvoiceNumber? LockedInvoiceNumber { get; private set; }

        public IReadOnlyList<int> DeletedLessonIds { get; private set; } = [];

        public Task<LessonRecord> SaveAsync(LessonRecord lesson, CancellationToken cancellationToken)
        {
            var savedLesson = lesson.Id > 0
                ? lesson
                : lesson with { Id = lessons.Count + 1 };
            var existingIndex = lessons.FindIndex(storedLesson => storedLesson.Id == savedLesson.Id);
            if (existingIndex >= 0)
            {
                lessons[existingIndex] = savedLesson;
            }
            else
            {
                lessons.Add(savedLesson);
            }

            return Task.FromResult(savedLesson);
        }

        public Task<IReadOnlyList<LessonRecord>> ListForStudentMonthAsync(
            string studentShortName,
            DateOnly month,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LessonRecord>>(
                lessons
                    .Where(lesson => lesson.StudentShortName == studentShortName)
                    .Where(lesson => lesson.StartsAt.Year == month.Year && lesson.StartsAt.Month == month.Month)
                    .ToArray());

        public Task<IReadOnlyList<LessonRecord>> ListForWindowAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LessonRecord>>(
                lessons
                    .Where(lesson =>
                    {
                        var date = DateOnly.FromDateTime(lesson.StartsAt.DateTime);
                        return date >= from && date <= to;
                    })
                    .ToArray());

        public Task MarkInvoicedAsync(
            IReadOnlyList<int> lessonIds,
            InvoiceNumber invoiceNumber,
            CancellationToken cancellationToken)
        {
            LockedLessonIds = lessonIds.ToArray();
            LockedInvoiceNumber = invoiceNumber;

            for (var index = 0; index < lessons.Count; index++)
            {
                if (lessonIds.Contains(lessons[index].Id))
                {
                    lessons[index] = lessons[index] with { InvoiceNumber = invoiceNumber };
                }
            }

            return Task.CompletedTask;
        }

        public Task<LessonRecord?> UpdatePlanAsync(
            int lessonId,
            int planCatalogId,
            string planName,
            int durationMinutes,
            CancellationToken cancellationToken)
        {
            var index = lessons.FindIndex(lesson => lesson.Id == lessonId);
            if (index < 0 || lessons[index].InvoiceNumber is not null)
            {
                return Task.FromResult<LessonRecord?>(null);
            }

            lessons[index] = lessons[index] with
            {
                PlanCatalogId = planCatalogId,
                PlanName = planName,
                DurationMinutes = durationMinutes
            };
            return Task.FromResult<LessonRecord?>(lessons[index]);
        }

        public Task DeleteUninvoicedAsync(IReadOnlyList<int> lessonIds, CancellationToken cancellationToken)
        {
            DeletedLessonIds = lessonIds.ToArray();
            lessons.RemoveAll(lesson => lessonIds.Contains(lesson.Id) && lesson.InvoiceNumber is null);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePlanCatalogRepository(IReadOnlyList<PlanCatalogItem> plans) : IPlanCatalogRepository
    {
        public Task<IReadOnlyList<PlanCatalogItem>> ListAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult(plans);

        public Task<IReadOnlyList<PlanCatalogItem>> ListAssignableAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlanCatalogItem>>(plans.Where(plan => plan.CanBeAssigned).ToArray());

        public Task<PlanCatalogItem?> GetAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(plans.FirstOrDefault(plan => plan.Id == id));

        public Task<OperationResult<PlanCatalogItem>> SaveAsync(PlanCatalogItem plan, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<PlanCatalogItem>.Success(plan));

        public Task<OperationResult<PlanCatalogItem>> RenameAsync(
            int id,
            string newName,
            DateTimeOffset changedAt,
            CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<PlanCatalogItem>.Success(
                plans.First(plan => plan.Id == id) with { Name = newName }));

        public Task<OperationResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<bool>.Success(true));
    }

    private sealed class FakeCalendarGateway(IReadOnlyList<CalendarLesson> lessons) : IGoogleCalendarGateway
    {
        public OperationResult<IReadOnlyList<CalendarLesson>> UpdateResult { get; init; } =
            OperationResult<IReadOnlyList<CalendarLesson>>.Success(lessons);

        public bool UpdateCalled { get; private set; }

        public List<LessonState> SavedLessonStates { get; } = [];

        public List<LessonAddRequest> PopulatedRequests { get; } = [];

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

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> PopulateLessonsAsync(
            IReadOnlyList<LessonAddRequest> requests,
            DateOnly refreshMonth,
            CancellationToken cancellationToken)
        {
            PopulatedRequests.AddRange(requests);
            return Task.FromResult(UpdateResult);
        }

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> UpdateLessonAsync(LessonEditRequest request, DateOnly refreshMonth, CancellationToken cancellationToken)
        {
            UpdateCalled = true;
            return Task.FromResult(UpdateResult);
        }

        public Task<OperationResult<IReadOnlyList<CalendarLesson>>> DeleteLessonAsync(string lessonId, DateOnly refreshMonth, CancellationToken cancellationToken) =>
            Task.FromResult(UpdateResult);

        public Task<OperationResult<LessonState>> UpdateLessonStateAsync(
            string lessonId,
            LessonState state,
            CancellationToken cancellationToken)
        {
            SavedLessonStates.Add(state);
            return Task.FromResult(OperationResult<LessonState>.Success(state));
        }
    }

    private sealed class FakeInvoiceRepository(List<string>? calls = null) : IInvoiceRepository
    {
        public IReadOnlyList<InvoiceLessonStatus> LessonStatuses { get; init; } = [];

        public IReadOnlyList<IssuedInvoiceSummary> StudentInvoices { get; init; } = [];

        public HashSet<string> StudentsWithInvoiceThisMonth { get; init; } = [];

        public PaymentStatus CurrentPaymentStatus { get; init; } = PaymentStatus.Outstanding;

        public IssuedInvoice? IssuedInvoiceToReturn { get; init; }

        public bool ReserveCalled { get; private set; }

        public bool FinalizeCalled { get; private set; }

        public bool UpdatePrintStatusCalled { get; private set; }

        public bool ReleaseReservationCalled { get; private set; }

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

        public Task<OperationResult<IssuedInvoice>> SaveIssuedInvoiceAsync(IssuedInvoice invoice, CancellationToken cancellationToken) =>
            Task.FromResult(OperationResult<IssuedInvoice>.Success(invoice));

        public Task<OperationResult<IssuedInvoice>> ReserveInvoiceAsync(
            InvoiceDraft draft,
            InvoiceTotals totals,
            CancellationToken cancellationToken)
        {
            ReserveCalled = true;
            calls?.Add("reserve");
            return Task.FromResult(OperationResult<IssuedInvoice>.Success(draft.Issue(
                new InvoiceNumber(1),
                totals,
                pdfFilePath: null,
                InvoiceIssueStatus.Issuing,
                InvoicePrintStatus.NotPrinted,
                printedAt: null,
                lastPrintError: null)));
        }

        public Task<OperationResult<IssuedInvoice>> FinalizeIssuedInvoiceAsync(
            InvoiceNumber invoiceNumber,
            string pdfFilePath,
            CancellationToken cancellationToken)
        {
            FinalizeCalled = true;
            calls?.Add($"finalize:{pdfFilePath}");
            return Task.FromResult(OperationResult<IssuedInvoice>.Success(CreateInvoiceDraft().Issue(
                invoiceNumber,
                CreateInvoiceDraft().CalculateTotals(CompanyProfile.Create(
                    "Maple Key Studio",
                    "Musikvej 1",
                    "12345678",
                    "elena@example.test",
                    cvr: null,
                    vatRegistrationDate: null,
                    VatStatus.NotRegistered,
                    "C:\\Invoices")),
                pdfFilePath,
                InvoiceIssueStatus.Issued,
                InvoicePrintStatus.NotPrinted,
                printedAt: null,
                lastPrintError: null)));
        }

        public Task ReleaseInvoiceReservationAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
        {
            ReleaseReservationCalled = true;
            calls?.Add($"release:{invoiceNumber}");
            return Task.CompletedTask;
        }

        public Task<OperationResult<IssuedInvoice>> UpdatePrintStatusAsync(
            InvoiceNumber invoiceNumber,
            InvoicePrintStatus printStatus,
            DateTimeOffset? printedAt,
            string? lastPrintError,
            CancellationToken cancellationToken)
        {
            UpdatePrintStatusCalled = true;
            calls?.Add($"print-status:{printStatus}:{printedAt:O}:{lastPrintError}");
            var source = IssuedInvoiceToReturn ?? CreateInvoiceDraft().Issue(
                invoiceNumber,
                CreateInvoiceDraft().CalculateTotals(CompanyProfile.Create(
                    "Maple Key Studio",
                    "Musikvej 1",
                    "12345678",
                    "elena@example.test",
                    cvr: null,
                    vatRegistrationDate: null,
                    VatStatus.NotRegistered,
                    "C:\\Invoices")),
                "C:\\Invoices\\001.pdf",
                InvoiceIssueStatus.Issued,
                InvoicePrintStatus.NotPrinted,
                printedAt: null,
                lastPrintError: null);

            return Task.FromResult(OperationResult<IssuedInvoice>.Success(new IssuedInvoice(
                invoiceNumber,
                source.InvoiceDate,
                source.Period,
                source.Company,
                source.Student,
                source.Lines,
                source.Totals,
                source.PdfFilePath,
                source.IssueStatus,
                printStatus,
                printedAt,
                lastPrintError)));
        }

        public Task<IssuedInvoice?> GetIssuedInvoiceAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken) =>
            Task.FromResult(IssuedInvoiceToReturn);

        public Task<IssuedInvoice?> GetIssuingInvoiceAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken) =>
            Task.FromResult<IssuedInvoice?>(null);

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

    private sealed class FakeInvoiceFileStore(List<string>? calls = null) : IInvoiceFileStore
    {
        public OperationResult<string> PublishResult { get; init; } =
            OperationResult<string>.Success("C:\\Invoices\\001.pdf");

        public Task<OperationResult<string>> CreateStagingPathAsync(InvoiceDraft draft, InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
        {
            calls?.Add($"stage:{invoiceNumber}");
            return Task.FromResult(OperationResult<string>.Success("C:\\Invoices\\.staging\\001.pdf"));
        }

        public Task<OperationResult<string>> VerifyStagedPdfAsync(string stagingPath, CancellationToken cancellationToken)
        {
            calls?.Add($"verify:{stagingPath}");
            return Task.FromResult(OperationResult<string>.Success(stagingPath));
        }

        public Task<OperationResult<string>> PublishAsync(string stagingPath, InvoiceDraft draft, InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
        {
            calls?.Add($"publish:{stagingPath}");
            return Task.FromResult(PublishResult);
        }

        public Task<OperationResult<string>> DiscardStagingPathAsync(string stagingPath, CancellationToken cancellationToken)
        {
            calls?.Add($"discard:{stagingPath}");
            return Task.FromResult(OperationResult<string>.Success(stagingPath));
        }
    }

    private sealed class FakeInvoicePdfWriter : IInvoicePdfWriter
    {
        public OperationResult<string> WriteResult { get; init; } =
            OperationResult<string>.Success("C:\\Invoices\\.staging\\001.pdf");

        public List<string>? Calls { get; init; }

        public Task<OperationResult<string>> WriteAsync(
            InvoiceDraft draft,
            InvoiceTotals totals,
            InvoiceNumber invoiceNumber,
            string stagingPath,
            CancellationToken cancellationToken)
        {
            Calls?.Add($"write:{stagingPath}");
            return Task.FromResult(WriteResult);
        }
    }

    private sealed class FakeInvoicePrinter(List<string>? calls = null) : IInvoicePrinter
    {
        public OperationResult<string> PrintResult { get; init; } =
            OperationResult<string>.Success("C:\\Invoices\\001.pdf");

        public bool PrintCalled { get; private set; }

        public Task<OperationResult<string>> PrintAsync(
            string pdfFilePath,
            string configuredPrinterName,
            CancellationToken cancellationToken)
        {
            PrintCalled = true;
            calls?.Add($"print:{pdfFilePath}:{configuredPrinterName}");
            return Task.FromResult(PrintResult);
        }
    }
}
