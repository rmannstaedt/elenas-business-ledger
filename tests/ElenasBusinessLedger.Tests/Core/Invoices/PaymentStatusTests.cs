using ElenasBusinessLedger.Core.Invoices;

namespace ElenasBusinessLedger.Tests.Core.Invoices;

// Checks payment transitions and the confirmation required to reopen a paid invoice.
public sealed class PaymentStatusTests
{
    [Fact]
    public void ToggleOutstandingMarksInvoicePaid()
    {
        var result = PaymentStatusRules.Toggle(PaymentStatus.Outstanding, confirmMarkOutstanding: false);

        Assert.True(result.Succeeded);
        Assert.False(result.RequiresConfirmation);
        Assert.Equal(PaymentStatus.Paid, result.Value);
    }

    [Fact]
    public void TogglePaidRequiresConfirmationBeforeMarkingOutstanding()
    {
        var result = PaymentStatusRules.Toggle(PaymentStatus.Paid, confirmMarkOutstanding: false);

        Assert.False(result.Succeeded);
        Assert.True(result.RequiresConfirmation);
        Assert.Equal(PaymentStatus.Paid, result.Value);
    }

    [Fact]
    public void TogglePaidMarksOutstandingWhenConfirmed()
    {
        var result = PaymentStatusRules.Toggle(PaymentStatus.Paid, confirmMarkOutstanding: true);

        Assert.True(result.Succeeded);
        Assert.False(result.RequiresConfirmation);
        Assert.Equal(PaymentStatus.Outstanding, result.Value);
    }
}
