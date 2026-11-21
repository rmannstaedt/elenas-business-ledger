using ElenasBusinessLedger.Core.Calendar;
using ElenasBusinessLedger.Core.Lessons;

namespace ElenasBusinessLedger.Tests.Core.Calendar;

public sealed class CalendarLessonEntryTests
{
    [Theory]
    [InlineData("$ Anna", "Anna")]
    [InlineData("$Anna", "Anna")]
    public void TryParseRelevantEntryUsesFirstLineStudentMarker(string text, string expectedShortName)
    {
        var parsed = CalendarLessonEntry.TryParse(
            text,
            new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
            out var entry);

        Assert.True(parsed);
        Assert.NotNull(entry);
        Assert.Equal(expectedShortName, entry.StudentShortName);
    }

    [Theory]
    [InlineData("Anna")]
    [InlineData("Piano Anna")]
    [InlineData(" $ Anna")]
    [InlineData("")]
    public void TryParseIgnoresEntriesWithoutFirstLineMarker(string text)
    {
        var parsed = CalendarLessonEntry.TryParse(
            text,
            new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
            out var entry);

        Assert.False(parsed);
        Assert.Null(entry);
    }

    [Fact]
    public void TryParseReadsCompletedStateFromLaterSummaryLine()
    {
        var parsed = CalendarLessonEntry.TryParse(
            "$ Anna\nStatus: Completed",
            new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero),
            out var entry);

        Assert.True(parsed);
        Assert.NotNull(entry);
        Assert.Equal(LessonState.Completed, entry.State);
    }

    [Fact]
    public void WithStatePreservesFirstLineMarkerAndRemovesCompletedMarkerForPlannedState()
    {
        var completed = CalendarLessonEntry.WithState("$ Anna\nTeacher note", LessonState.Completed);
        var planned = CalendarLessonEntry.WithState(completed, LessonState.Planned);

        Assert.Equal("$ Anna\nTeacher note\nStatus: Completed", completed);
        Assert.Equal("$ Anna\nTeacher note", planned);
    }
}
