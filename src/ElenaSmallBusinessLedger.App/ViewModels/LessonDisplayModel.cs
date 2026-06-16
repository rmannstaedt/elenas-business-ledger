using System.Globalization;
using ElenaSmallBusinessLedger.Core.Invoices;
using ElenaSmallBusinessLedger.Core.Lessons;
using ElenaSmallBusinessLedger.Core.UseCases.Startup;

namespace ElenaSmallBusinessLedger.App.ViewModels;

public sealed class LessonDisplayModel
{
    public LessonDisplayModel(DashboardLesson lesson)
    {
        Id = lesson.Id;
        StudentShortName = lesson.StudentShortName;
        StartsAt = lesson.StartsAt;
        State = lesson.State;
        IsInvoiced = lesson.IsInvoiced;
        PaymentStatus = lesson.PaymentStatus;
    }

    public string Id { get; }

    public string StudentShortName { get; }

    public DateTimeOffset StartsAt { get; }

    public LessonState State { get; }

    public bool IsInvoiced { get; }

    public PaymentStatus? PaymentStatus { get; }

    public string StartsAtText => StartsAt.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);

    public string StateText => State == LessonState.Completed ? "Completed" : "Planned";

    public string InvoicedText => IsInvoiced ? "Yes" : "No";

    public string PaymentStatusText => PaymentStatus switch
    {
        Core.Invoices.PaymentStatus.Paid => "Paid",
        Core.Invoices.PaymentStatus.Outstanding => "Outstanding",
        _ => string.Empty
    };
}
