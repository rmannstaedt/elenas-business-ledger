using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.UseCases.Lessons;
using ElenasBusinessLedger.Infrastructure.Calendar;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Util.Store;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ElenasBusinessLedger.Tests.Infrastructure.Calendar;

// Covers calendar scoping, event formatting, refresh outcomes, and reuse of saved Google authorization.
public sealed class GoogleCalendarGatewayTests
{
    [Fact]
    public async Task CalendarOperationsUseOnlyIdsFromTheConfiguredSource()
    {
        var start = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var token = TestContext.Current.CancellationToken;
        var client = new FakeGoogleCalendarEventClient { Events = [new GoogleCalendarEvent("same-id", "$ Mira", start)] };
        var gateway = new GoogleCalendarGateway(client, new GoogleCalendarGatewayOptions(
            "test-calendar", TimeZoneInfo.Utc, TimeSpan.FromHours(1), "original"));
        var date = new DateOnly(2026, 6, 16);
        var lesson = Assert.Single(await gateway.ListLessonsAsync(date, date, token));
        Assert.Equal("original:same-id", lesson.Id);
        Assert.Equal("same-id", lesson.LegacyEventId);

        var wrong = await gateway.DeleteLessonAsync("other:same-id", date, token);
        Assert.False(wrong.Succeeded);
        Assert.Null(client.DeletedEventId);
        var right = await gateway.DeleteLessonAsync(lesson.Id, date, token);
        Assert.True(right.Succeeded);
        Assert.Equal("same-id", client.DeletedEventId);
    }

    [Fact]
    public async Task PopulatePersistsAnAcknowledgedEventBeforeTheNextCreateFails()
    {
        var client = new FakeGoogleCalendarEventClient { FailCreateNumber = 2 };
        var gateway = new GoogleCalendarGateway(client, new GoogleCalendarGatewayOptions("trial", TimeZoneInfo.Utc, TimeSpan.FromHours(1)));
        var date = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero);
        var receipts = new List<(int Index, string EventId)>();
        var result = await gateway.PopulateWithReceiptsAsync(
            [new LessonAddRequest("Mira", date, 1), new LessonAddRequest("Mira", date.AddDays(7), 2)],
            new DateOnly(2026, 6, 1),
            (index, created, _) =>
            {
                Assert.Single(client.CreatedEvents);
                receipts.Add((index, created.Id));
                return Task.FromResult<ValidationError?>(null);
            },
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal((0, "created"), Assert.Single(receipts));
        Assert.Equal("created", Assert.Single(result.Value!).Id);
    }

    [Fact]
    public void CredentialFactoryRequestsOwnedCalendarEventScopeOnly()
    {
        Assert.Equal(
            [CalendarService.Scope.CalendarEventsOwned],
            GoogleCalendarCredentialFactory.RequestedScopes);
    }

    [Fact]
    public async Task CredentialFactoryCreatesCalendarServiceFromSavedCredentialWithoutWebAuthorization()
    {
        var tokenStore = new InMemoryDataStore();
        await tokenStore.StoreAsync(
            "elena@example.test",
            new TokenResponse
            {
                AccessToken = "saved-access-token",
                RefreshToken = "saved-refresh-token",
                ExpiresInSeconds = 3600,
                IssuedUtc = DateTime.UtcNow
            });
        var factory = new GoogleCalendarCredentialFactory(
            new ClientSecrets { ClientId = "client-id", ClientSecret = "client-secret" },
            tokenStore,
            "Elena's Business Ledger");

        var service = await factory.TryCreateCalendarServiceFromSavedCredentialAsync(
            "elena@example.test",
            CancellationToken.None);

        Assert.NotNull(service);
        Assert.IsType<UserCredential>(service.HttpClientInitializer);
    }

    [Fact]
    public async Task CredentialFactoryReturnsNullWhenSavedCredentialIsMissing()
    {
        var factory = new GoogleCalendarCredentialFactory(
            new ClientSecrets { ClientId = "client-id", ClientSecret = "client-secret" },
            new InMemoryDataStore(),
            "Elena's Business Ledger");

        var service = await factory.TryCreateCalendarServiceFromSavedCredentialAsync(
            "elena@example.test",
            CancellationToken.None);

        Assert.Null(service);
    }

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
    public async Task AddLessonCreatesMarkedEventWithPlanDurationThenRefreshesMonth()
    {
        var client = new FakeGoogleCalendarEventClient
        {
            Events =
            [
                new GoogleCalendarEvent("created", "$ Anna", new DateTimeOffset(2026, 6, 20, 15, 0, 0, TimeSpan.FromHours(2)))
            ]
        };

        var gateway = new GoogleCalendarGateway(client, CreateOptions());
        var result = await gateway.AddLessonAsync(
            new LessonAddRequest("Anna", new DateTimeOffset(2026, 6, 20, 15, 0, 0, TimeSpan.FromHours(2)), PlanCatalogId: 42, DurationMinutes: 45),
            new DateOnly(2026, 6, 1),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("$ Anna", client.CreatedSummary);
        Assert.Equal(new DateTimeOffset(2026, 6, 20, 15, 0, 0, TimeSpan.FromHours(2)), client.CreatedStartsAt);
        Assert.Equal(TimeSpan.FromMinutes(45), client.CreatedDuration);
        Assert.Equal("created", result.Value!.CreatedLesson.Id);
        Assert.Contains(result.Value.Lessons, lesson => lesson.Id == "created");
    }

    [Fact]
    public async Task AddDateOnlyLessonCreatesMarkedAllDayEventThenRefreshesMonth()
    {
        var startsAt = new DateTimeOffset(2026, 6, 20, 0, 0, 0, TimeSpan.FromHours(2));
        var client = new FakeGoogleCalendarEventClient
        {
            Events =
            [
                new GoogleCalendarEvent("created", "$ Anna", startsAt, HasKnownStartTime: false)
            ]
        };

        var gateway = new GoogleCalendarGateway(client, CreateOptions(defaultLessonDuration: TimeSpan.FromMinutes(45)));
        var result = await gateway.AddLessonAsync(
            new LessonAddRequest("Anna", startsAt, PlanCatalogId: 42, HasKnownStartTime: false),
            new DateOnly(2026, 6, 1),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("$ Anna", client.CreatedSummary);
        Assert.Equal(startsAt, client.CreatedStartsAt);
        Assert.False(client.CreatedHasKnownStartTime);
        Assert.Equal("created", result.Value!.CreatedLesson.Id);
        Assert.False(result.Value.CreatedLesson.HasKnownStartTime);
        Assert.Contains(result.Value.Lessons, lesson => lesson.Id == "created" && !lesson.HasKnownStartTime);
    }

    [Fact]
    public async Task PopulateLessonsCreatesMarkedEventsWithEachPlanDurationThenRefreshesMonth()
    {
        var client = new FakeGoogleCalendarEventClient
        {
            Events =
            [
                new GoogleCalendarEvent("created-1", "$ Anna", new DateTimeOffset(2026, 6, 23, 15, 0, 0, TimeSpan.FromHours(2))),
                new GoogleCalendarEvent("created-2", "$ Anna", new DateTimeOffset(2026, 6, 30, 15, 0, 0, TimeSpan.FromHours(2)))
            ]
        };

        var gateway = new GoogleCalendarGateway(client, CreateOptions());
        var result = await gateway.PopulateLessonsAsync(
            [
                new LessonAddRequest("Anna", new DateTimeOffset(2026, 6, 23, 15, 0, 0, TimeSpan.FromHours(2)), PlanCatalogId: 42, DurationMinutes: 45),
                new LessonAddRequest("Anna", new DateTimeOffset(2026, 6, 30, 15, 0, 0, TimeSpan.FromHours(2)), PlanCatalogId: 43, DurationMinutes: 75)
            ],
            new DateOnly(2026, 6, 1),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(["$ Anna", "$ Anna"], client.CreatedEvents.Select(created => created.Summary));
        Assert.Equal(
            [
                new DateTimeOffset(2026, 6, 23, 15, 0, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2026, 6, 30, 15, 0, 0, TimeSpan.FromHours(2))
            ],
            client.CreatedEvents.Select(created => created.StartsAt));
        Assert.Equal([TimeSpan.FromMinutes(45), TimeSpan.FromMinutes(75)], client.CreatedEvents.Select(created => created.Duration));
        Assert.Equal(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.FromHours(2)), client.LastListFromInclusive);
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.FromHours(2)), client.LastListToExclusive);
        Assert.Equal(["created-1", "created-2"], result.Value!.Select(lesson => lesson.Id));
    }

    [Fact]
    public async Task PopulateLessonsReturnsCreatedLessonFallbacksWhenRefreshFailsAfterCreate()
    {
        var client = new FakeGoogleCalendarEventClient
        {
            ListException = new InvalidOperationException("refresh failed")
        };

        var gateway = new GoogleCalendarGateway(client, CreateOptions(defaultLessonDuration: TimeSpan.FromMinutes(45)));
        var result = await gateway.PopulateLessonsAsync(
            [
                new LessonAddRequest("Anna", new DateTimeOffset(2026, 6, 23, 15, 0, 0, TimeSpan.FromHours(2)), PlanCatalogId: 42),
                new LessonAddRequest("Anna", new DateTimeOffset(2026, 6, 30, 15, 0, 0, TimeSpan.FromHours(2)), PlanCatalogId: 42)
            ],
            new DateOnly(2026, 6, 1),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "Calendar.RefreshFailed");
        Assert.Equal(["created", "created-2"], result.Value!.Select(lesson => lesson.Id));
        Assert.Equal(2, client.CreatedEvents.Count);
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
    public async Task GoogleCalendarServiceEventClientReadsAllEventPages()
    {
        var requestedPageTokens = new List<string?>();
        var client = new GoogleCalendarServiceEventClient(
            "Romance Standard Time",
            (calendarId, fromInclusive, toExclusive, pageToken, cancellationToken) =>
            {
                requestedPageTokens.Add(pageToken);
                return Task.FromResult(pageToken is null
                    ? new Events
                    {
                        NextPageToken = "page-2",
                        Items =
                        [
                            new Event
                            {
                                Id = "lesson-1",
                                Summary = "$ Anna",
                                Start = new EventDateTime
                                {
                                    DateTimeDateTimeOffset = new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.FromHours(2))
                                }
                            }
                        ]
                    }
                    : new Events
                    {
                        Items =
                        [
                            new Event
                            {
                                Id = "lesson-2",
                                Summary = "$ Bo",
                                Start = new EventDateTime
                                {
                                    DateTimeDateTimeOffset = new DateTimeOffset(2026, 6, 17, 16, 0, 0, TimeSpan.FromHours(2))
                                }
                            }
                        ]
                    });
            });

        var events = await client.ListEventsAsync(
            "primary",
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.FromHours(2)),
            CancellationToken.None);

        Assert.Equal(["lesson-1", "lesson-2"], events.Select(calendarEvent => calendarEvent.Id));
        Assert.Equal([null, "page-2"], requestedPageTokens);
    }

    [Fact]
    public async Task GoogleCalendarServiceEventClientCreatesEventsWithIanaTimeZone()
    {
        Event? createdEvent = null;
        var client = new GoogleCalendarServiceEventClient(
            "Romance Standard Time",
            "DK",
            (calendarId, calendarEvent, cancellationToken) =>
            {
                createdEvent = calendarEvent;
                return Task.FromResult(new Event { Id = "created" });
            });

        var createdId = await client.CreateLessonEventAsync(
            "primary",
            "$ Anna",
            new DateTimeOffset(2026, 6, 23, 15, 0, 0, TimeSpan.FromHours(2)),
            hasKnownStartTime: true,
            TimeSpan.FromMinutes(45),
            CancellationToken.None);

        Assert.Equal("created", createdId);
        Assert.Equal("Europe/Copenhagen", createdEvent?.Start?.TimeZone);
        Assert.Equal("Europe/Copenhagen", createdEvent?.End?.TimeZone);
    }

    [Fact]
    public async Task GoogleCalendarServiceEventClientCreatesAllDayEventsForDateOnlyLessons()
    {
        Event? createdEvent = null;
        var client = new GoogleCalendarServiceEventClient(
            "Romance Standard Time",
            "DK",
            (calendarId, calendarEvent, cancellationToken) =>
            {
                createdEvent = calendarEvent;
                return Task.FromResult(new Event { Id = "created" });
            });

        var createdId = await client.CreateLessonEventAsync(
            "primary",
            "$ Anna",
            new DateTimeOffset(2026, 6, 23, 0, 0, 0, TimeSpan.FromHours(2)),
            hasKnownStartTime: false,
            TimeSpan.FromMinutes(45),
            CancellationToken.None);

        Assert.Equal("created", createdId);
        Assert.Equal("2026-06-23", createdEvent?.Start?.Date);
        Assert.Equal("2026-06-24", createdEvent?.End?.Date);
        Assert.Null(createdEvent?.Start?.DateTimeDateTimeOffset);
        Assert.Null(createdEvent?.End?.DateTimeDateTimeOffset);
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

    [Fact]
    public async Task WindowsProtectedFileDataStoreUsesDistinctFilesForDistinctKeysThatEscapeSimilarly()
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

            await store.StoreAsync("a%b@example.com", new StoredToken("first-token"));
            await store.StoreAsync("a_25b@example.com", new StoredToken("second-token"));

            var first = await store.GetAsync<StoredToken>("a%b@example.com");
            var second = await store.GetAsync<StoredToken>("a_25b@example.com");

            Assert.Equal("first-token", first.AccessToken);
            Assert.Equal("second-token", second.AccessToken);
            Assert.Equal(2, Directory.GetFiles(directory).Length);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task WindowsProtectedFileDataStoreReadsAndMigratesLegacyEscapedTokenFile()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var directory = Path.Combine(Path.GetTempPath(), "ElenaLedgerTokenTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var legacyFile = Path.Combine(directory, LegacyEscapedFileName("a%b@example.com"));
            var token = JsonSerializer.SerializeToUtf8Bytes(new StoredToken("legacy-token"));
            var originalEntropy = Encoding.UTF8.GetBytes("MySmallBusinessLedger.GoogleCalendarOAuth");
            File.WriteAllBytes(legacyFile, ProtectedData.Protect(token, originalEntropy, DataProtectionScope.CurrentUser));

            var store = new WindowsProtectedFileDataStore(directory);
            var loaded = await store.GetAsync<StoredToken>("a%b@example.com");

            Assert.Equal("legacy-token", loaded.AccessToken);
            Assert.False(File.Exists(legacyFile));
            Assert.Single(Directory.GetFiles(directory));
            Assert.Equal("legacy-token", (await store.GetAsync<StoredToken>("a%b@example.com")).AccessToken);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task WindowsProtectedFileDataStoreReportsCurrentAndLegacyTokenFilesForKey()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var directory = Path.Combine(Path.GetTempPath(), "ElenaLedgerTokenTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var store = new WindowsProtectedFileDataStore(directory);

            Assert.False(store.ContainsKey("elena@example.test"));

            await store.StoreAsync("elena@example.test", new StoredToken("current-token"));
            Assert.True(store.ContainsKey("elena@example.test"));

            var hashedFile = Assert.Single(Directory.GetFiles(directory));
            var legacyFile = Path.Combine(directory, LegacyEscapedFileName("elena@example.test"));
            File.Move(hashedFile, legacyFile);

            Assert.True(store.ContainsKey("elena@example.test"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task WindowsProtectedFileDataStoreClearDeletesOnlyFilesInsideTokenFolder()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var root = Path.Combine(Path.GetTempPath(), "ElenaLedgerTokenTests", Guid.NewGuid().ToString("N"));
        var tokenDirectory = Path.Combine(root, "GoogleTokens");
        Directory.CreateDirectory(tokenDirectory);
        var outsideFile = Path.Combine(root, "ledger.db");
        File.WriteAllText(outsideFile, "keep");

        try
        {
            IDataStore store = new WindowsProtectedFileDataStore(tokenDirectory);
            await store.StoreAsync("google-token", new StoredToken("visible-secret-token"));

            await store.ClearAsync();

            Assert.Empty(Directory.GetFiles(tokenDirectory));
            Assert.True(File.Exists(outsideFile));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static GoogleCalendarGatewayOptions CreateOptions(TimeSpan? defaultLessonDuration = null) =>
        new(
            CalendarId: "primary",
            TimeZone: TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"),
            DefaultLessonDuration: defaultLessonDuration ?? TimeSpan.FromHours(1));

    // Provides a minimal token fixture so credential tests do not need a real Google account.
    private sealed record StoredToken(string AccessToken);

    private static string LegacyEscapedFileName(string key) =>
        $"{Uri.EscapeDataString(key).Replace("%", "_")}.bin";

    // Stores OAuth fixtures in memory so credential reuse can be tested without protected files.
    private sealed class InMemoryDataStore : IDataStore
    {
        private readonly Dictionary<string, object?> values = [];

        public Task StoreAsync<T>(string key, T value)
        {
            values[key] = value;
            return Task.CompletedTask;
        }

        public Task DeleteAsync<T>(string key)
        {
            values.Remove(key);
            return Task.CompletedTask;
        }

        public Task<T> GetAsync<T>(string key)
        {
            return Task.FromResult(values.TryGetValue(key, out var value)
                ? (T)value!
                : default!);
        }

        public Task ClearAsync()
        {
            values.Clear();
            return Task.CompletedTask;
        }
    }

    // Records Google event requests and simulates API failures without a network call.
    private sealed class FakeGoogleCalendarEventClient : IGoogleCalendarEventClient
    {
        public IReadOnlyList<GoogleCalendarEvent> Events { get; init; } = [];

        public Exception? WriteException { get; init; }

        public int? FailCreateNumber { get; init; }

        public Exception? ListException { get; init; }

        public string? LastListCalendarId { get; private set; }

        public DateTimeOffset? LastListFromInclusive { get; private set; }

        public DateTimeOffset? LastListToExclusive { get; private set; }

        public string? CreatedSummary { get; private set; }

        public DateTimeOffset? CreatedStartsAt { get; private set; }

        public bool CreatedHasKnownStartTime { get; private set; } = true;

        public TimeSpan? CreatedDuration { get; private set; }

        public List<CreatedCalendarEvent> CreatedEvents { get; } = [];

        public string? MovedEventId { get; private set; }

        public DateTimeOffset? MovedStartsAt { get; private set; }

        public bool MovedHasKnownStartTime { get; private set; } = true;

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
            bool hasKnownStartTime,
            TimeSpan duration,
            CancellationToken cancellationToken)
        {
            if (WriteException is not null)
            {
                throw WriteException;
            }

            if (CreatedEvents.Count + 1 == FailCreateNumber)
            {
                throw new InvalidOperationException("Simulated calendar failure");
            }

            CreatedSummary = summary;
            CreatedStartsAt = startsAt;
            CreatedHasKnownStartTime = hasKnownStartTime;
            CreatedDuration = duration;
            CreatedEvents.Add(new CreatedCalendarEvent(summary, startsAt, hasKnownStartTime, duration));
            var id = CreatedEvents.Count == 1
                ? "created"
                : $"created-{CreatedEvents.Count}";
            return Task.FromResult(id);
        }

        public Task MoveEventAsync(
            string calendarId,
            string eventId,
            DateTimeOffset startsAt,
            bool hasKnownStartTime,
            CancellationToken cancellationToken)
        {
            if (WriteException is not null)
            {
                throw WriteException;
            }

            MovedEventId = eventId;
            MovedStartsAt = startsAt;
            MovedHasKnownStartTime = hasKnownStartTime;
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

    // Captures created event values so tests can verify calendar titles, timing, and duration.
    private sealed record CreatedCalendarEvent(
        string Summary,
        DateTimeOffset StartsAt,
        bool HasKnownStartTime,
        TimeSpan Duration);
}
