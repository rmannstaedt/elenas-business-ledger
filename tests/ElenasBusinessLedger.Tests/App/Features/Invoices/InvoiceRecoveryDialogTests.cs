using System.Windows;
using System.Windows.Controls;
using ElenasBusinessLedger.App.Views;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Money;
using ElenasBusinessLedger.Core.UseCases.Invoices;

namespace ElenasBusinessLedger.Tests.App.Views;

// Checks which recovery actions the dialog presents for unpublished, checkpointed, and legacy reservations.
public sealed class InvoiceRecoveryDialogTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void RecoveryOptionsReflectPublicationAndLegacyState(bool legacy, bool canRevise)
    {
        WpfTestThread.Run(() =>
        {
            var date = new DateOnly(2026, 6, 16);
            var draft = InvoiceDraft.Create(date, new CompanySnapshot("Maple Key Studio", "Example Road 1", null),
                new StudentSnapshot("Mira", "Mira Example", "Example Road 2"),
                [new InvoiceLineDraft(new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.Zero), "Standard",
                    Money.Dkk(320), planCatalogId: 1, lessonId: 1)], null);
            var totals = new InvoiceTotals(Money.Dkk(320), Money.Dkk(0), Money.Dkk(320));
            var invoice = draft.Issue(new InvoiceNumber(1), totals, null, InvoiceIssueStatus.Issuing,
                snapshotRevision: legacy ? null : "test-revision");
            var view = new InvoiceRecoveryDialog(new PendingInvoiceRecovery(invoice, draft, totals, canRevise))
            {
                ShowActivated = false
            };
            try
            {
                view.Show();
                Assert.True(view.IsVisible);
                var resume = Assert.IsType<Button>(view.FindName("ResumeButton"));
                var revise = Assert.IsType<Button>(view.FindName("ReviseButton"));
                var confirmation = Assert.IsType<CheckBox>(view.FindName("LegacyConfirmation"));

                Assert.Equal(InvoiceRecoveryChoice.Cancel, view.Choice);
                Assert.Equal(legacy ? Visibility.Collapsed : Visibility.Visible, resume.Visibility);
                Assert.Equal(canRevise ? Visibility.Visible : Visibility.Collapsed, revise.Visibility);
                Assert.Equal(!legacy, revise.IsEnabled);
                if (legacy)
                {
                    confirmation.IsChecked = true;
                    Assert.True(revise.IsEnabled);
                    confirmation.IsChecked = false;
                    Assert.False(revise.IsEnabled);
                }
            }
            finally
            {
                view.Close();
            }
        });
    }
}
