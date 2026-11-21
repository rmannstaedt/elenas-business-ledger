using System.Windows;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.App.ViewModels;
using ElenasBusinessLedger.Core.Lessons;
using ElenasBusinessLedger.Core.UseCases.Startup;

namespace ElenasBusinessLedger.Tests.App.Services;

// Checks that the lesson confirmation is owned by the dashboard window.
public sealed class MessageBoxConfirmationServiceTests
{
    [Fact]
    public void DeleteLessonConfirmationUsesMainWindowOwner()
    {
        RunOnStaThread(async () =>
        {
            var owner = new Window();
            var presenter = new CapturingMessageBoxPresenter(MessageBoxResult.Yes);
            var service = new MessageBoxConfirmationService(() => owner, presenter);
            var lesson = new LessonDisplayModel(
                new DashboardLesson(
                    "lesson-1",
                    "Mira",
                    new DateTimeOffset(2026, 6, 1, 14, 0, 0, TimeSpan.FromHours(2)),
                    LessonState.Planned,
                    IsInvoiced: false,
                    PaymentStatus: null));

            var confirmed = await service.ConfirmDeleteLessonAsync(lesson, CancellationToken.None);

            Assert.True(confirmed);
            Assert.Same(owner, presenter.Owner);
            Assert.Equal("Confirm lesson deletion", presenter.Caption);
            Assert.Equal(MessageBoxButton.YesNo, presenter.Button);
            Assert.Equal(MessageBoxImage.Warning, presenter.Image);
            Assert.Equal(MessageBoxResult.No, presenter.DefaultResult);
        });
    }

    private static void RunOnStaThread(Func<Task> action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw exception;
        }
    }

    // Records confirmation arguments so tests can inspect ownership and button defaults without showing a message box.
    private sealed class CapturingMessageBoxPresenter : IMessageBoxPresenter
    {
        private readonly MessageBoxResult result;

        public CapturingMessageBoxPresenter(MessageBoxResult result)
        {
            this.result = result;
        }

        public Window? Owner { get; private set; }

        public string? Caption { get; private set; }

        public MessageBoxButton Button { get; private set; }

        public MessageBoxImage Image { get; private set; }

        public MessageBoxResult DefaultResult { get; private set; }

        public MessageBoxResult Show(
            Window? owner,
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon,
            MessageBoxResult defaultResult)
        {
            Owner = owner;
            Caption = caption;
            Button = button;
            Image = icon;
            DefaultResult = defaultResult;
            return result;
        }
    }
}
