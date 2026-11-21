using ElenasBusinessLedger.Core.Lessons;

namespace ElenasBusinessLedger.Core.Calendar;

public sealed record CalendarLesson(
    string Id,
    string StudentShortName,
    DateTimeOffset StartsAt,
    LessonState State = LessonState.Planned,
    bool HasKnownStartTime = true);
