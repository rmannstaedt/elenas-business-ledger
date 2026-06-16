using ElenaSmallBusinessLedger.Core.Calendar;

namespace ElenaSmallBusinessLedger.Tests.Core.Calendar;

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
}
