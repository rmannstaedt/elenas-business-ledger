using System.Globalization;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Calendar;
using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.UseCases.Startup;

namespace MySmallBusinessLedger.App.ViewModels;

public sealed class LessonDisplayModel : ViewModelBase
{
    private static readonly IReadOnlyList<string> TimeOptionValues = Enumerable
        .Range(0, 24 * 4)
        .Select(index => TimeOnly.MinValue.AddMinutes(index * 15).ToString("HH\\:mm", CultureInfo.InvariantCulture))
        .ToArray();

    private TimeZoneInfo lessonTimeZone;
    private string id;
    private DateTimeOffset? savedStartsAt;
    private DateTime? startsOnDate;
    private string startsAtTimeText;
    private LessonState state;
    private bool isDraft;

    public LessonDisplayModel(DashboardLesson lesson, TimeZoneInfo? lessonTimeZone = null)
    {
        id = lesson.Id;
        StudentShortName = lesson.StudentShortName;
        this.lessonTimeZone = lessonTimeZone ?? TimeZoneInfo.Local;
        startsOnDate = lesson.StartsAt.Date;
        startsAtTimeText = lesson.StartsAt.ToString("HH:mm", CultureInfo.InvariantCulture);
        savedStartsAt = lesson.StartsAt;
        state = lesson.State;
        IsInvoiced = lesson.IsInvoiced;
        PaymentStatus = lesson.PaymentStatus;
    }

    private LessonDisplayModel(string studentShortName, TimeZoneInfo lessonTimeZone)
    {
        id = $"draft-{Guid.NewGuid():N}";
        StudentShortName = studentShortName;
        this.lessonTimeZone = lessonTimeZone;
        startsOnDate = null;
        startsAtTimeText = string.Empty;
        state = LessonState.Planned;
        IsInvoiced = false;
        PaymentStatus = null;
        isDraft = true;
    }

    public static LessonDisplayModel CreateDraft(string studentShortName, TimeZoneInfo lessonTimeZone) =>
        new(studentShortName, lessonTimeZone);

    public string Id => id;

    public string StudentShortName { get; }

    public DateTimeOffset StartsAt
    {
        get
        {
            return TryGetStartsAt(out var startsAt)
                ? startsAt
                : DateTimeOffset.MinValue;
        }
    }

    public DateTime? StartsOnDate
    {
        get => startsOnDate;
        set
        {
            if (SetProperty(ref startsOnDate, value))
            {
                OnPropertyChanged(nameof(StartsAt));
                OnPropertyChanged(nameof(StartsAtText));
            }
        }
    }

    public string StartsAtTimeText
    {
        get => startsAtTimeText;
        set
        {
            if (SetProperty(ref startsAtTimeText, value))
            {
                OnPropertyChanged(nameof(StartsAt));
                OnPropertyChanged(nameof(StartsAtText));
            }
        }
    }

    public IReadOnlyList<string> TimeOptions => TimeOptionValues;

    public LessonState State
    {
        get => state;
        set
        {
            if (SetProperty(ref state, value))
            {
                OnPropertyChanged(nameof(StateText));
                OnPropertyChanged(nameof(CanEdit));
                OnPropertyChanged(nameof(CanToggleState));
                OnPropertyChanged(nameof(StateToggleToolTip));
            }
        }
    }

    public bool IsDraft
    {
        get => isDraft;
        private set
        {
            if (SetProperty(ref isDraft, value))
            {
                OnPropertyChanged(nameof(CanToggleState));
                OnPropertyChanged(nameof(HasDateTimeChange));
            }
        }
    }

    public bool IsInvoiced { get; }

    public bool CanEdit => (IsDraft || State == LessonState.Planned) && !IsInvoiced;

    public bool CanToggleState => !IsDraft && !IsInvoiced;

    public bool HasDateTimeChange => TryGetStartsAt(out var startsAt) && startsAt != savedStartsAt;

    public PaymentStatus? PaymentStatus { get; }

    public string StartsAtText => TryGetStartsAt(out var startsAt)
        ? startsAt.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture)
        : string.Empty;

    public string StateText => State == LessonState.Completed ? "Completed" : "Planned";

    public string StateToggleToolTip => State == LessonState.Completed
        ? "Mark lesson as planned"
        : "Mark lesson as completed";

    public string InvoicedText => IsInvoiced ? "Yes" : "No";

    public string PaymentStatusText => PaymentStatus switch
    {
        Core.Invoices.PaymentStatus.Paid => "Paid",
        Core.Invoices.PaymentStatus.Outstanding => "Outstanding",
        _ => string.Empty
    };

    public void AcceptDateTimeChange()
    {
        savedStartsAt = StartsAt;
    }

    public void AcceptCreatedLesson(CalendarLesson lesson)
    {
        id = lesson.Id;
        startsOnDate = lesson.StartsAt.Date;
        startsAtTimeText = lesson.StartsAt.ToString("HH:mm", CultureInfo.InvariantCulture);
        savedStartsAt = lesson.StartsAt;
        IsDraft = false;
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(StartsAt));
        OnPropertyChanged(nameof(StartsAtText));
        OnPropertyChanged(nameof(StartsOnDate));
        OnPropertyChanged(nameof(StartsAtTimeText));
    }

    public bool TryGetStartsAt(out DateTimeOffset startsAt)
    {
        startsAt = default;
        if (StartsOnDate is not { } date)
        {
            return false;
        }

        if (!TimeOnly.TryParseExact(StartsAtTimeText, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return false;
        }

        var localDateTime = DateOnly.FromDateTime(date).ToDateTime(time);
        startsAt = new DateTimeOffset(localDateTime, lessonTimeZone.GetUtcOffset(localDateTime));
        return true;
    }
}
