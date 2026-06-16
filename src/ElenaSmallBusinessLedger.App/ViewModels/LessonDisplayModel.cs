using System.Globalization;
using ElenaSmallBusinessLedger.Core.Invoices;
using ElenaSmallBusinessLedger.Core.Calendar;
using ElenaSmallBusinessLedger.Core.Lessons;
using ElenaSmallBusinessLedger.Core.UseCases.Startup;

namespace ElenaSmallBusinessLedger.App.ViewModels;

public sealed class LessonDisplayModel : ViewModelBase
{
    private static readonly IReadOnlyList<string> TimeOptionValues = Enumerable
        .Range(0, 24 * 4)
        .Select(index => TimeOnly.MinValue.AddMinutes(index * 15).ToString("HH\\:mm", CultureInfo.InvariantCulture))
        .ToArray();

    private TimeSpan offset;
    private string id;
    private DateTimeOffset? savedStartsAt;
    private DateTime? startsOnDate;
    private string startsAtTimeText;
    private LessonState state;
    private bool isDraft;

    public LessonDisplayModel(DashboardLesson lesson)
    {
        id = lesson.Id;
        StudentShortName = lesson.StudentShortName;
        offset = lesson.StartsAt.Offset;
        startsOnDate = lesson.StartsAt.Date;
        startsAtTimeText = lesson.StartsAt.ToString("HH:mm", CultureInfo.InvariantCulture);
        savedStartsAt = lesson.StartsAt;
        state = lesson.State;
        IsInvoiced = lesson.IsInvoiced;
        PaymentStatus = lesson.PaymentStatus;
    }

    private LessonDisplayModel(string studentShortName, TimeSpan offset)
    {
        id = $"draft-{Guid.NewGuid():N}";
        StudentShortName = studentShortName;
        this.offset = offset;
        startsOnDate = null;
        startsAtTimeText = string.Empty;
        state = LessonState.Planned;
        IsInvoiced = false;
        PaymentStatus = null;
        isDraft = true;
    }

    public static LessonDisplayModel CreateDraft(string studentShortName, TimeSpan offset) =>
        new(studentShortName, offset);

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
        offset = lesson.StartsAt.Offset;
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

        startsAt = new DateTimeOffset(DateOnly.FromDateTime(date).ToDateTime(time), offset);
        return true;
    }
}
