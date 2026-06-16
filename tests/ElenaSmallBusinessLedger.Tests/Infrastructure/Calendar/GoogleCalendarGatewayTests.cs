using ElenaSmallBusinessLedger.Core.Common;
using ElenaSmallBusinessLedger.Core.Lessons;
using ElenaSmallBusinessLedger.Core.UseCases.Lessons;
using ElenaSmallBusinessLedger.Infrastructure.Calendar;
using Google.Apis.Util.Store;

namespace ElenaSmallBusinessLedger.Tests.Infrastructure.Calendar;

public sealed class GoogleCalendarGatewayTests
{
    [Fact]
    public async Task ListLessonsReturnsOnlyEventsMarkedOnTheFirstSummaryLine()
    {
        var client = new FakeGoogleCalendarEventClient
        {
            Events =
            [
                new GoogleCalendarEvent("lesson-1", "$ Anna", new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.FromHours(2))),
                new GoogleCalendarEvent("ignored-1", "Anna", new DateTimeOffset(2026, 6, 16, 16, 0, 0, TimeSpan.FromHours(2))),
                new GoogleCalendarEvent("lesson-2", "$Bo\nextra notes", new DateTimeOffset(2026, 6, 17, 17, 0, 0, TimeSpan.FromHours(2))),
                new GoogleCalendarEvent("ignored-2", " Piano\n$ Clara", new DateTimeOffset(2026, 6, 18, 18, 0, 0, TimeSpan.FromHours(2)))
            ]
        };

        var gateway = new GoogleCalendarGateway(client, CreateOptions());
        var lessons = await gateway.ListLessonsAsync(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), CancellationToken.None);

        Assert.Equal(["lesson-1", "lesson-2"], lessons.Select(lesson => lesson.Id));
        Assert.Equal(["Anna", "Bo"], lessons.Select(lesson => lesson.StudentShortName));
        Assert.All(lessons, lesson => Assert.Equal(LessonState.Planned, lesson.State));
        Assert.Equal("primary", client.LastListCalendarId);
        Assert.Equal(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.FromHours(2)), client.LastListFromInclusive);
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.FromHours(2)), client.LastListToExclusive);
    }

    [Fact]
    public async Task ListLessonsReadsCompletedStateFromMarkedEvent()
    {
        var client = new FakeGoogleCalendarEventClient
        {
            Events =
            [
                new GoogleCalendarEvent("lesson-1", "$ Anna\nStatus: Completed", new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.FromHours(2)))
            ]
        };

        var gateway = new GoogleCalendarGateway(client, CreateOptions());
        var lessons = await gateway.ListLessonsAsync(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), CancellationToken.None);

        Assert.Single(lessons);
        Assert.Equal(LessonState.Completed, lessons[0].State);
    }

    [Fact]
    public async Task AddLessonCreatesMarkedEventWithConfiguredDurationThenRefreshesMonth()
    {
        var client = new FakeGoogleCalendarEventClient
        {
            Events =
            [
                new GoogleCalendarEvent("created", "$ Anna", new DateTimeOffset(2026, 6, 20, 15, 0, 0, TimeSpan.FromHours(2)))
            ]
        };

        var gateway = new GoogleCalendarGateway(client, CreateOptions(defaultLessonDuration: TimeSpan.FromMinutes(45)));
        var result = await gateway.AddLessonAsync(
            new LessonAddRequest("Anna", new DateTimeOffset(2026, 6, 20, 15, 0, 0, TimeSpan.FromHours(2))),
            new DateOnly(2026, 6, 1),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("$ Anna", client.CreatedSummary);
        Assert.Equal(new DateTimeOffset(2026, 6, 20, 15, 0, 0, TimeSpan.FromHours(2)), client.CreatedStartsAt);
        Assert.Equal(TimeSpan.FromMinutes(45), client.CreatedDuration);
        Assert.Contains(result.Value!, lesson => lesson.Id == "created");
    }

    [Fact]
    public async Task UpdateLessonMovesEventThenRefreshesMonth()
    {
        var client = new FakeGoogleCalendarEventClient
        {
            Events =
            [
                new GoogleCalendarEvent("lesson-1", "$ Anna", new DateTimeOffset(2026, 6, 21, 15, 0, 0, TimeSpan.FromHours(2)))
            ]
        };

        var gateway = new GoogleCalendarGateway(client, CreateOptions());
        var result = await gateway.UpdateLessonAsync(
            new LessonEditRequest("lesson-1", "Anna", new DateTimeOffset(2026, 6, 21, 15, 0, 0, TimeSpan.FromHours(2))),
            new DateOnly(2026, 6, 1),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("lesson-1", client.MovedEventId);
        Assert.Equal(new DateTimeOffset(2026, 6, 21, 15, 0, 0, TimeSpan.FromHours(2)), client.MovedStartsAt);
        Assert.Contains(result.Value!, lesson => lesson.Id == "lesson-1");
    }

    [Fact]
    public async Task DeleteLessonDeletesEventThenRefreshesMonth()
    {
        var client = new FakeGoogleCalendarEventClient
        {
            Events =
            [
                new GoogleCalendarEvent("lesson-2", "$ Bo", new DateTimeOffset(2026, 6, 22, 17, 0, 0, TimeSpan.FromHours(2)))
            ]
        };

        var gateway = new GoogleCalendarGateway(client, CreateOptions());
        var result = await gateway.DeleteLessonAsync("lesson-1", new DateOnly(2026, 6, 1), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("lesson-1", client.DeletedEventId);
        Assert.Contains(result.Value!, lesson => lesson.Id == "lesson-2");
    }

    [Fact]
    public async Task UpdateLessonStateUpdatesCalendarEventSummary()
    {
        var client = new FakeGoogleCalendarEventClient();
        var gateway = new GoogleCalendarGateway(client, CreateOptions());

        var result = await gateway.UpdateLessonStateAsync("lesson-1", LessonState.Completed, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("lesson-1", client.StateUpdatedEventId);
        Assert.Equal(LessonState.Completed, client.StateUpdatedState);
    }

    [Fact]
    public async Task CalendarWriteFailureIsMappedToCoreFailure()
    {
        var client = new FakeGoogleCalendarEventClient
        {
            WriteException = new InvalidOperationException("Google rejected the request.")
        };

        var gateway = new GoogleCalendarGateway(client, CreateOptions());
        var result = await gateway.UpdateLessonAsync(
            new LessonEditRequest("lesson-1", "Anna", new DateTimeOffset(2026, 6, 21, 15, 0, 0, TimeSpan.FromHours(2))),
            new DateOnly(2026, 6, 1),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Calendar.UpdateFailed");
    }

    [Fact]
    public async Task RefreshFailureAfterSuccessfulCalendarWriteIsReportedTruthfully()
    {
        var client = new FakeGoogleCalendarEventClient
        {
            ListException = new InvalidOperationException("Refresh failed after update.")
        };

        var gateway = new GoogleCalendarGateway(client, CreateOptions());
        var result = await gateway.UpdateLessonAsync(
            new LessonEditRequest("lesson-1", "Anna", new DateTimeOffset(2026, 6, 21, 15, 0, 0, TimeSpan.FromHours(2))),
            new DateOnly(2026, 6, 1),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("lesson-1", client.MovedEventId);
        Assert.DoesNotContain(result.Errors, error => error.Code == "Calendar.UpdateFailed");
        Assert.Contains(result.Errors, error => error.Code == "Calendar.RefreshFailed");
    }

    [Fact]
    public async Task WindowsProtectedFileDataStoreDoesNotPersistPlaintextTokens()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var directory = Path.Combine(Path.GetTempPath(), "ElenaLedgerTokenTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            IDataStore store = new WindowsProtectedFileDataStore(directory);

            await store.StoreAsync("google-token", new StoredToken("visible-secret-token"));
            var loaded = await store.GetAsync<StoredToken>("google-token");

            Assert.NotNull(loaded);
            Assert.Equal("visible-secret-token", loaded.AccessToken);

            var persistedBytes = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .SelectMany(File.ReadAllBytes)
                .ToArray();
            var persistedText = System.Text.Encoding.UTF8.GetString(persistedBytes);
            Assert.DoesNotContain("visible-secret-token", persistedText);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static GoogleCalendarGatewayOptions CreateOptions(TimeSpan? defaultLessonDuration = null) =>
        new(
            CalendarId: "primary",
            TimeZone: TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"),
            DefaultLessonDuration: defaultLessonDuration ?? TimeSpan.FromHours(1));

    private sealed record StoredToken(string AccessToken);

    private sealed class FakeGoogleCalendarEventClient : IGoogleCalendarEventClient
    {
        public IReadOnlyList<GoogleCalendarEvent> Events { get; init; } = [];

        public Exception? WriteException { get; init; }

        public Exception? ListException { get; init; }

        public string? LastListCalendarId { get; private set; }

        public DateTimeOffset? LastListFromInclusive { get; private set; }

        public DateTimeOffset? LastListToExclusive { get; private set; }

        public string? CreatedSummary { get; private set; }

        public DateTimeOffset? CreatedStartsAt { get; private set; }

        public TimeSpan? CreatedDuration { get; private set; }

        public string? MovedEventId { get; private set; }

        public DateTimeOffset? MovedStartsAt { get; private set; }

        public string? DeletedEventId { get; private set; }

        public string? StateUpdatedEventId { get; private set; }

        public LessonState? StateUpdatedState { get; private set; }

        public Task<IReadOnlyList<GoogleCalendarEvent>> ListEventsAsync(
            string calendarId,
            DateTimeOffset fromInclusive,
            DateTimeOffset toExclusive,
            CancellationToken cancellationToken)
        {
            if (ListException is not null)
            {
                throw ListException;
            }

            LastListCalendarId = calendarId;
            LastListFromInclusive = fromInclusive;
            LastListToExclusive = toExclusive;
            return Task.FromResult(Events);
        }

        public Task<string> CreateLessonEventAsync(
            string calendarId,
            string summary,
            DateTimeOffset startsAt,
            TimeSpan duration,
            CancellationToken cancellationToken)
        {
            if (WriteException is not null)
            {
                throw WriteException;
            }

            CreatedSummary = summary;
            CreatedStartsAt = startsAt;
            CreatedDuration = duration;
            return Task.FromResult("created");
        }

        public Task MoveEventAsync(
            string calendarId,
            string eventId,
            DateTimeOffset startsAt,
            CancellationToken cancellationToken)
        {
            if (WriteException is not null)
            {
                throw WriteException;
            }

            MovedEventId = eventId;
            MovedStartsAt = startsAt;
            return Task.CompletedTask;
        }

        public Task DeleteEventAsync(string calendarId, string eventId, CancellationToken cancellationToken)
        {
            if (WriteException is not null)
            {
                throw WriteException;
            }

            DeletedEventId = eventId;
            return Task.CompletedTask;
        }

        public Task UpdateLessonStateAsync(
            string calendarId,
            string eventId,
            LessonState state,
            CancellationToken cancellationToken)
        {
            if (WriteException is not null)
            {
                throw WriteException;
            }

            StateUpdatedEventId = eventId;
            StateUpdatedState = state;
            return Task.CompletedTask;
        }
    }
}
