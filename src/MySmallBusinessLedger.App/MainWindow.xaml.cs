using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MySmallBusinessLedger.App.Commands;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;

namespace MySmallBusinessLedger.App;

public partial class MainWindow : Window
{
    private static readonly Color PickerBorderColor = Color.FromRgb(0xC7, 0xD3, 0xE1);
    private const double DatePickerMaxHeight = 320;
    private const double TimePickerMaxHeight = 420;

    private bool hasLoaded;
    private bool activationRefreshQueued;
    private Border? openLessonDatePanel;
    private Button? openLessonDateTarget;
    private Border? openLessonTimePanel;
    private Button? openLessonTimeTarget;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        WindowPlacementStore.Attach(this, nameof(MainWindow));
        Closed += OnClosed;
        if (Application.Current is { } application)
        {
            application.Deactivated += OnApplicationDeactivated;
        }
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

    private void OnWindowPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        CloseOpenLessonDatePanelIfOutside(source);
        CloseOpenLessonTimePanelIfOutside(source);
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        CloseOpenLessonDatePanel();
        CloseOpenLessonTimePanel();
    }

    private void OnStudentDropdownButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { ContextMenu: { } menu } button)
        {
            return;
        }

        CloseOpenLessonDatePanel();
        CloseOpenLessonTimePanel();
        menu.PlacementTarget = button;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void OnApplicationDeactivated(object? sender, EventArgs e)
    {
        CloseOpenLessonDatePanel();
        CloseOpenLessonTimePanel();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        if (Application.Current is { } application)
        {
            application.Deactivated -= OnApplicationDeactivated;
        }

        CloseOpenLessonDatePanel();
        CloseOpenLessonTimePanel();
    }

    private void OnGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CloseOpenLessonDatePanel();
        CloseOpenLessonTimePanel();
        ViewModel.ClearError();
    }

    private async void OnLessonEditCommitted(object sender, RoutedEventArgs e)
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

    private void OnLessonTimeButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            ToggleLessonTimePanel(button);
        }
    }

    private void OnLessonTimeButtonPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CloseOpenLessonDatePanel();
        if (sender is Button button && ToggleLessonTimePanel(button))
        {
            e.Handled = true;
        }
    }

    private void OnLessonDateButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            ToggleLessonDatePanel(button);
        }
    }

    private void OnLessonDateButtonPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CloseOpenLessonTimePanel();
        if (sender is Button button && ToggleLessonDatePanel(button))
        {
            e.Handled = true;
        }
    }

    private bool ToggleLessonDatePanel(Button button)
    {
        if (!button.IsEnabled || button.DataContext is not LessonDisplayModel lesson)
        {
            return false;
        }

        if (button.Tag is Border panel && ReferenceEquals(openLessonDatePanel, panel))
        {
            CloseOpenLessonDatePanel();
            return true;
        }

        CloseOpenLessonDatePanel();
        CloseOpenLessonTimePanel();
        var newPanel = CreateLessonDatePanel(button, lesson);
        AddPickerPanel(newPanel);
        button.Tag = newPanel;
        button.Unloaded -= OnLessonDateButtonUnloaded;
        button.Unloaded += OnLessonDateButtonUnloaded;
        openLessonDatePanel = newPanel;
        openLessonDateTarget = button;
        return true;
    }

    private bool ToggleLessonTimePanel(Button button)
    {
        if (!button.IsEnabled || button.DataContext is not LessonDisplayModel lesson)
        {
            return false;
        }

        if (button.Tag is Border panel && ReferenceEquals(openLessonTimePanel, panel))
        {
            CloseOpenLessonTimePanel();
            return true;
        }

        CloseOpenLessonTimePanel();
        CloseOpenLessonDatePanel();
        var newPanel = CreateLessonTimePanel(button, lesson);
        AddPickerPanel(newPanel);
        button.Tag = newPanel;
        button.Unloaded -= OnLessonTimeButtonUnloaded;
        button.Unloaded += OnLessonTimeButtonUnloaded;
        openLessonTimePanel = newPanel;
        openLessonTimeTarget = button;
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
        CloseOpenLessonDatePanel();
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

    private void CloseOpenLessonDatePanel()
    {
        if (openLessonDatePanel is not null)
        {
            if (openLessonDatePanel.Child is Calendar calendar)
            {
                calendar.SelectedDatesChanged -= OnLessonCalendarSelectedDatesChanged;
            }

            if (openLessonDatePanel.Parent is Panel parent)
            {
                parent.Children.Remove(openLessonDatePanel);
            }

            openLessonDatePanel = null;
        }

        if (openLessonDateTarget is not null)
        {
            openLessonDateTarget.Unloaded -= OnLessonDateButtonUnloaded;
            if (openLessonDateTarget.Tag is Border)
            {
                openLessonDateTarget.Tag = null;
            }

            openLessonDateTarget = null;
        }
    }

    private void CloseOpenLessonDatePanelIfOutside(DependencyObject? source)
    {
        if (openLessonDatePanel is not { } panel)
        {
            return;
        }

        if (source is not null &&
            (IsWithin(source, openLessonDateTarget) || IsWithin(source, panel)))
        {
            return;
        }

        CloseOpenLessonDatePanel();
    }

    private void CloseOpenLessonTimePanel()
    {
        if (openLessonTimePanel is not null)
        {
            if (openLessonTimePanel.Child is ListBox timeList)
            {
                timeList.SelectionChanged -= OnLessonTimeSelectionChanged;
            }

            if (openLessonTimePanel.Parent is Panel parent)
            {
                parent.Children.Remove(openLessonTimePanel);
            }

            openLessonTimePanel = null;
        }

        if (openLessonTimeTarget is not null)
        {
            openLessonTimeTarget.Unloaded -= OnLessonTimeButtonUnloaded;
            if (openLessonTimeTarget.Tag is Border)
            {
                openLessonTimeTarget.Tag = null;
            }

            openLessonTimeTarget = null;
        }
    }

    private void CloseOpenLessonTimePanelIfOutside(DependencyObject? source)
    {
        if (openLessonTimePanel is not { } panel)
        {
            return;
        }

        if (source is not null &&
            (IsWithin(source, openLessonTimeTarget) || IsWithin(source, panel)))
        {
            return;
        }

        CloseOpenLessonTimePanel();
    }

    private static bool IsWithin(DependencyObject source, DependencyObject? possibleAncestor)
    {
        if (possibleAncestor is null)
        {
            return false;
        }

        for (var current = source; current is not null; current = GetParent(current))
        {
            if (ReferenceEquals(current, possibleAncestor))
            {
                return true;
            }
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject child)
    {
        if (child is FrameworkElement { Parent: { } frameworkParent })
        {
            return frameworkParent;
        }

        if (child is FrameworkContentElement { Parent: DependencyObject contentParent })
        {
            return contentParent;
        }

        try
        {
            return VisualTreeHelper.GetParent(child);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private Border CreateLessonDatePanel(Button button, LessonDisplayModel lesson)
    {
        var calendar = new Calendar
        {
            BorderThickness = new Thickness(0),
            DataContext = lesson,
            SelectedDate = lesson.StartsOnDate
        };
        calendar.SelectedDatesChanged += OnLessonCalendarSelectedDatesChanged;

        return CreatePickerBorder(button, lesson, calendar, width: null, maxPanelHeight: DatePickerMaxHeight);
    }

    private Border CreateLessonTimePanel(Button button, LessonDisplayModel lesson)
    {
        var width = Math.Max(70, button.ActualWidth);
        var timeList = new ListBox
        {
            DataContext = lesson,
            ItemsSource = lesson.TimeOptions,
            MinWidth = width,
            Width = width,
            SelectedItem = string.IsNullOrWhiteSpace(lesson.StartsAtTimeText)
                ? null
                : lesson.StartsAtTimeText
        };
        timeList.SelectionChanged += OnLessonTimeSelectionChanged;

        return CreatePickerBorder(button, lesson, timeList, width, TimePickerMaxHeight);
    }

    private Border CreatePickerBorder(
        Button button,
        LessonDisplayModel lesson,
        Control child,
        double? width,
        double maxPanelHeight)
    {
        button.UpdateLayout();
        RootGrid.UpdateLayout();
        var anchor = button.TransformToAncestor(RootGrid).Transform(new Point(0, button.ActualHeight));
        var maxHeight = Math.Max(80, Math.Min(maxPanelHeight, RootGrid.ActualHeight - anchor.Y - 4));

        child.MaxHeight = maxHeight;
        if (width is { } requestedWidth)
        {
            child.MinWidth = requestedWidth;
            child.Width = requestedWidth;
        }

        return new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(PickerBorderColor),
            BorderThickness = new Thickness(1),
            Child = child,
            DataContext = lesson,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(anchor.X, anchor.Y, 0, 0),
            SnapsToDevicePixels = true,
            VerticalAlignment = VerticalAlignment.Top
        };
    }

    private void AddPickerPanel(Border panel)
    {
        Grid.SetRow(panel, 0);
        Grid.SetRowSpan(panel, 3);
        Panel.SetZIndex(panel, 1000);
        RootGrid.Children.Add(panel);
    }

    private async void OnLessonTimeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox { DataContext: LessonDisplayModel lesson, SelectedItem: string selectedTime } ||
            e.AddedItems.Count == 0)
        {
            return;
        }

        lesson.StartsAtTimeText = selectedTime;
        CloseOpenLessonTimePanel();
        await ViewModel.SaveLessonCommand.ExecuteAsync(lesson);
    }

    private void OnLessonDateButtonUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && ReferenceEquals(openLessonDateTarget, button))
        {
            CloseOpenLessonDatePanel();
        }
    }

    private void OnLessonTimeButtonUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && ReferenceEquals(openLessonTimeTarget, button))
        {
            CloseOpenLessonTimePanel();
        }
    }
}
