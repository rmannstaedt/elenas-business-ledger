using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MySmallBusinessLedger.App.Commands;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;

namespace MySmallBusinessLedger.App;

public partial class MainWindow : Window
{
    private bool hasLoaded;
    private bool activationRefreshQueued;
    private Popup? openLessonDatePopup;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        WindowPlacementStore.Attach(this, nameof(MainWindow));
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (hasLoaded)
        {
            return;
        }

        hasLoaded = true;
        await ViewModel.LoadAsync();
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        if (!hasLoaded || activationRefreshQueued)
        {
            return;
        }

        activationRefreshQueued = true;
        Dispatcher.BeginInvoke(
            new Action(async () =>
            {
                try
                {
                    await ViewModel.RefreshAfterActivationAsync();
                }
                finally
                {
                    activationRefreshQueued = false;
                }
            }),
            DispatcherPriority.Background);
    }

    private async void OnInvoiceDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is DataGrid { SelectedItem: InvoiceDisplayModel invoice })
        {
            await ViewModel.OpenInvoiceCommand.ExecuteAsync(invoice);
        }
    }

    private void OnGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CloseOpenLessonDatePopup();
        ViewModel.ClearError();
    }

    private async void OnLessonEditCommitted(object sender, RoutedEventArgs e)
    {
        await CommitLessonEditAsync(sender);
    }

    private async void OnLessonTimeDropDownClosed(object? sender, EventArgs e)
    {
        await CommitLessonEditAsync(sender);
    }

    private async Task CommitLessonEditAsync(object? sender)
    {
        if (sender is FrameworkElement { DataContext: LessonDisplayModel lesson })
        {
            await ViewModel.SaveLessonCommand.ExecuteAsync(lesson);
        }
    }

    private void OnLessonTimeSelectorPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ComboBox comboBox || !comboBox.IsEnabled || comboBox.IsKeyboardFocusWithin)
        {
            return;
        }

        e.Handled = true;
        comboBox.Focus();
        comboBox.Dispatcher.BeginInvoke(
            new Action(() => comboBox.IsDropDownOpen = true),
            DispatcherPriority.Input);
    }

    private void OnLessonDateButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            ToggleLessonDatePopup(button);
        }
    }

    private void OnLessonDateButtonPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Button button && ToggleLessonDatePopup(button))
        {
            e.Handled = true;
        }
    }

    private bool ToggleLessonDatePopup(Button button)
    {
        if (!button.IsEnabled || GetOrCreateLessonDatePopup(button) is not { } popup)
        {
            return false;
        }

        if (popup.IsOpen)
        {
            popup.IsOpen = false;
            if (ReferenceEquals(openLessonDatePopup, popup))
            {
                openLessonDatePopup = null;
            }

            return true;
        }

        CloseOpenLessonDatePopup();
        popup.IsOpen = true;
        openLessonDatePopup = popup;
        return true;
    }

    private async void OnLessonCalendarSelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not Calendar { DataContext: LessonDisplayModel lesson } || e.AddedItems.Count == 0)
        {
            return;
        }

        var selectedDate = e.AddedItems.OfType<DateTime>().LastOrDefault();
        if (selectedDate == default)
        {
            return;
        }

        lesson.StartsOnDate = selectedDate.Date;
        CloseOpenLessonDatePopup();
        await ViewModel.SaveLessonCommand.ExecuteAsync(lesson);
    }

    private async void OnCommandButtonPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Button { Command: { } command } button)
        {
            return;
        }

        var parameter = button.CommandParameter;
        if (!command.CanExecute(parameter))
        {
            return;
        }

        e.Handled = true;
        if (command is AsyncRelayCommand asyncCommand)
        {
            await asyncCommand.ExecuteAsync(parameter);
            return;
        }

        command.Execute(parameter);
    }

    private void CloseOpenLessonDatePopup()
    {
        if (openLessonDatePopup is not null)
        {
            openLessonDatePopup.IsOpen = false;
            openLessonDatePopup = null;
        }
    }

    private Popup? GetOrCreateLessonDatePopup(Button button)
    {
        if (button.Tag is Popup popup)
        {
            return popup;
        }

        if (button.DataContext is not LessonDisplayModel lesson)
        {
            return null;
        }

        var calendar = new Calendar
        {
            DataContext = lesson,
            SelectedDate = lesson.StartsOnDate
        };
        calendar.SelectedDatesChanged += OnLessonCalendarSelectedDatesChanged;

        popup = new Popup
        {
            AllowsTransparency = true,
            Child = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x6A, 0x8F, 0xBF)),
                BorderThickness = new Thickness(1),
                Child = calendar
            },
            DataContext = lesson,
            Placement = PlacementMode.Bottom,
            PlacementTarget = button,
            StaysOpen = true
        };

        button.Tag = popup;
        button.Unloaded += OnLessonDateButtonUnloaded;
        return popup;
    }

    private void OnLessonDateButtonUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Popup popup } button)
        {
            return;
        }

        popup.IsOpen = false;
        if (popup.Child is Border { Child: Calendar calendar })
        {
            calendar.SelectedDatesChanged -= OnLessonCalendarSelectedDatesChanged;
        }

        if (ReferenceEquals(openLessonDatePopup, popup))
        {
            openLessonDatePopup = null;
        }

        button.Unloaded -= OnLessonDateButtonUnloaded;
        button.Tag = null;
    }
}
