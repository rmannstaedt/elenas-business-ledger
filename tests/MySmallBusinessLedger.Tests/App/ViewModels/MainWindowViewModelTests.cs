using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using MySmallBusinessLedger.Core.Common;
using MySmallBusinessLedger.Core.Invoices;
using MySmallBusinessLedger.Core.Lessons;
using MySmallBusinessLedger.Core.UseCases.Lessons;
using MySmallBusinessLedger.Core.UseCases.Startup;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfButton = System.Windows.Controls.Button;
using WpfBorder = System.Windows.Controls.Border;
using WpfCalendar = System.Windows.Controls.Calendar;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfDataGridGridLinesVisibility = System.Windows.Controls.DataGridGridLinesVisibility;
using WpfDataGridLengthUnitType = System.Windows.Controls.DataGridLengthUnitType;
using WpfDataGridTemplateColumn = System.Windows.Controls.DataGridTemplateColumn;
using WpfDataGridTextColumn = System.Windows.Controls.DataGridTextColumn;
using WpfGrid = System.Windows.Controls.Grid;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfMenuItem = System.Windows.Controls.MenuItem;
using AppMainWindow = MySmallBusinessLedger.App.MainWindow;

namespace MySmallBusinessLedger.Tests.App.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void MainWindowInitializesWithDashboardSurface()
    {
        RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel();
            var window = new AppMainWindow(viewModel);

            Assert.Equal("My Small Business Ledger", window.Title);
            Assert.Same(viewModel, window.DataContext);
            Assert.NotNull(window.Content);

            window.Close();
        });
    }

    [Fact]
    public void StudentMenuOnlyShowsManageStudents()
    {
        RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel();
            var window = new AppMainWindow(viewModel);

            var studentMenu = Assert.Single(FindChildren<WpfMenuItem>(window), item => item.Header?.ToString() == "_Student");
            var studentActions = studentMenu.Items
                .OfType<WpfMenuItem>()
                .Select(item => item.Header?.ToString())
                .Where(header => header is not null)
                .Cast<string>()
                .ToArray();

            Assert.Equal(["_Manage Students"], studentActions);

            window.Close();
        });
    }

    [Fact]
    public void LessonDeleteButtonPreviewClickInvokesDeleteWorkflow()
    {
        RunOnStaThread(() =>
        {
            var lessonEditor = new FakeLessonEditorService();
            var viewModel = CreateViewModel(
                dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
                confirmation: new FakeConfirmationService { NextAnswer = true },
                lessonEditorService: lessonEditor);

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Lessons.Count == 1);

                var deleteButton =
                    FindVisualChildren<WpfButton>(window)
                        .FirstOrDefault(button =>
                            button.Content?.ToString() == "×" &&
                            AutomationProperties.GetName(button) == "Delete lesson");
                Assert.NotNull(deleteButton);
                RaisePreviewLeftClick(deleteButton);
                PumpUntil(() => lessonEditor.DeleteWasCalled);

                Assert.True(lessonEditor.DeleteWasCalled);
                Assert.Empty(viewModel.Lessons);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LessonStateButtonPreviewClickInvokesToggleWorkflow()
    {
        RunOnStaThread(() =>
        {
            var lessonStates = new FakeLessonStateService();
            var viewModel = CreateViewModel(
                dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
                lessonStates: lessonStates);

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Lessons.Count == 1);

                var stateButton =
                    FindVisualChildren<WpfButton>(window)
                        .FirstOrDefault(button => AutomationProperties.GetName(button) == "Mark lesson as completed");
                Assert.NotNull(stateButton);

                RaisePreviewLeftClick(stateButton);
                PumpUntil(() => lessonStates.LastLessonId is not null);

                Assert.Equal("lesson-1", lessonStates.LastLessonId);
                Assert.Equal("Completed", viewModel.Lessons[0].StateText);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void InvoiceStatusButtonPreviewClickInvokesPaymentWorkflow()
    {
        RunOnStaThread(() =>
        {
            var payments = new FakePaymentService();
            var viewModel = CreateViewModel(payments: payments);

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Invoices.Count == 1);

                var paymentButton =
                    FindVisualChildren<WpfButton>(window)
                        .FirstOrDefault(button => AutomationProperties.GetName(button) == "Outstanding");
                Assert.NotNull(paymentButton);

                RaisePreviewLeftClick(paymentButton);
                PumpUntil(() => payments.LastInvoiceNumber.Value == 1);

                Assert.Equal(new InvoiceNumber(1), payments.LastInvoiceNumber);
                Assert.Equal("Paid", viewModel.Invoices[0].PaymentStatusText);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LessonDateButtonOpensInsideWindowCalendarPanel()
    {
        RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel(
                dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null });

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Lessons.Count == 1);

                var dateButton = FindButton(window, "Open lesson date selector");

                RaisePreviewLeftClick(dateButton);
                PumpUntil(() => dateButton.Tag is WpfBorder);

                var panel = Assert.IsType<WpfBorder>(dateButton.Tag);
                Assert.Contains(panel, FindVisualChildren<WpfBorder>(window));
                Assert.Equal(new Thickness(1), panel.BorderThickness);
                Assert.Equal(Color.FromRgb(0xC7, 0xD3, 0xE1), Assert.IsType<SolidColorBrush>(panel.BorderBrush).Color);
                var calendar = Assert.Single(FindVisualChildren<WpfCalendar>(panel));
                Assert.Equal(new Thickness(0), calendar.BorderThickness);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LessonCalendarSelectionUpdatesDateAndClosesPanel()
    {
        RunOnStaThread(() =>
        {
            var lessonEditor = new FakeLessonEditorService();
            var viewModel = CreateViewModel(
                dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
                lessonEditorService: lessonEditor);

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Lessons.Count == 1);

                var dateButton = FindButton(window, "Open lesson date selector");
                RaisePreviewLeftClick(dateButton);
                PumpUntil(() => dateButton.Tag is WpfBorder);

                var panel = Assert.IsType<WpfBorder>(dateButton.Tag);
                var calendar = Assert.Single(FindVisualChildren<WpfCalendar>(panel));

                calendar.SelectedDates.Clear();
                calendar.SelectedDates.Add(new DateTime(2026, 6, 23));
                PumpUntil(() => dateButton.Tag is null);

                Assert.True(lessonEditor.EditWasCalled);
                Assert.Equal(
                    new DateTimeOffset(2026, 6, 23, 15, 0, 0, TimeSpan.FromHours(2)),
                    lessonEditor.LastEditRequest?.StartsAt);
                Assert.DoesNotContain(panel, FindVisualChildren<WpfBorder>(window));
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LessonDatePanelClosesWhenClickingOutsideSelector()
    {
        RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel(
                dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null });

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Lessons.Count == 1);

                var dateButton = FindButton(window, "Open lesson date selector");
                RaisePreviewLeftClick(dateButton);
                PumpUntil(() => dateButton.Tag is WpfBorder);

                var panel = Assert.IsType<WpfBorder>(dateButton.Tag);
                RaisePreviewLeftClick(window);
                PumpUntil(() => dateButton.Tag is null);

                Assert.DoesNotContain(panel, FindVisualChildren<WpfBorder>(window));
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LessonTimeSelectorOpensInsideWindowWhenClickedAfterReceivingFocus()
    {
        RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel(
                dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null });

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Lessons.Count == 1);

                var timeSelector = FindButton(window, "Open lesson time selector");

                timeSelector.Focus();
                RaisePreviewLeftClick(timeSelector);
                PumpUntil(() => timeSelector.Tag is WpfBorder);

                var panel = Assert.IsType<WpfBorder>(timeSelector.Tag);
                Assert.Contains(panel, FindVisualChildren<WpfBorder>(window));
                Assert.Equal(Color.FromRgb(0xC7, 0xD3, 0xE1), Assert.IsType<SolidColorBrush>(panel.BorderBrush).Color);
                var timeList = Assert.Single(FindVisualChildren<WpfListBox>(panel));
                Assert.Equal(420, timeList.MaxHeight);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LessonTimeSelectorClosesWhenClickingOutsideSelector()
    {
        RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel(
                dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null });

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Lessons.Count == 1);

                var timeSelector = FindButton(window, "Open lesson time selector");

                RaisePreviewLeftClick(timeSelector);
                PumpUntil(() => timeSelector.Tag is WpfBorder);

                var panel = Assert.IsType<WpfBorder>(timeSelector.Tag);
                RaisePreviewLeftClick(window);
                PumpUntil(() => timeSelector.Tag is null);

                Assert.DoesNotContain(panel, FindVisualChildren<WpfBorder>(window));
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LessonTimeSelectionUpdatesTimeAndClosesSelector()
    {
        RunOnStaThread(() =>
        {
            var lessonEditor = new FakeLessonEditorService();
            var viewModel = CreateViewModel(
                dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
                lessonEditorService: lessonEditor);

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Lessons.Count == 1);

                var timeSelector = FindButton(window, "Open lesson time selector");
                RaisePreviewLeftClick(timeSelector);
                PumpUntil(() => timeSelector.Tag is WpfBorder);

                var panel = Assert.IsType<WpfBorder>(timeSelector.Tag);
                var timeList = Assert.Single(FindVisualChildren<WpfListBox>(panel));
                timeList.SelectedItem = "15:30";
                PumpUntil(() => timeSelector.Tag is null && lessonEditor.EditWasCalled);

                Assert.Equal(
                    new DateTimeOffset(2026, 6, 16, 15, 30, 0, TimeSpan.FromHours(2)),
                    lessonEditor.LastEditRequest?.StartsAt);
                Assert.DoesNotContain(panel, FindVisualChildren<WpfBorder>(window));
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LessonDatePanelOpensWhenTimeSelectorIsOpen()
    {
        RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel(
                dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null });

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Lessons.Count == 1);

                var timeSelector = FindButton(window, "Open lesson time selector");
                var dateButton = FindButton(window, "Open lesson date selector");

                RaisePreviewLeftClick(timeSelector);
                PumpUntil(() => timeSelector.Tag is WpfBorder);

                var timePanel = Assert.IsType<WpfBorder>(timeSelector.Tag);
                RaisePreviewLeftClick(dateButton);
                PumpUntil(() => dateButton.Tag is WpfBorder && timeSelector.Tag is null);

                var datePanel = Assert.IsType<WpfBorder>(dateButton.Tag);
                Assert.Contains(datePanel, FindVisualChildren<WpfBorder>(window));
                Assert.DoesNotContain(timePanel, FindVisualChildren<WpfBorder>(window));
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LessonDatePanelClosesWhenWindowDeactivates()
    {
        RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel(
                dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null });

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Lessons.Count == 1);

                var dateButton = FindButton(window, "Open lesson date selector");

                RaisePreviewLeftClick(dateButton);
                PumpUntil(() => dateButton.Tag is WpfBorder);
                var panel = Assert.IsType<WpfBorder>(dateButton.Tag);

                var deactivatedHandler = typeof(AppMainWindow).GetMethod(
                    "OnWindowDeactivated",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.NotNull(deactivatedHandler);

                deactivatedHandler.Invoke(window, [window, EventArgs.Empty]);
                PumpUntil(() => dateButton.Tag is null);

                Assert.DoesNotContain(panel, FindVisualChildren<WpfBorder>(window));
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LessonDatePanelClosesWhenApplicationDeactivates()
    {
        RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel(
                dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null });

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Lessons.Count == 1);

                var dateButton = FindButton(window, "Open lesson date selector");

                RaisePreviewLeftClick(dateButton);
                PumpUntil(() => dateButton.Tag is WpfBorder);
                var panel = Assert.IsType<WpfBorder>(dateButton.Tag);

                var deactivatedHandler = typeof(AppMainWindow).GetMethod(
                    "OnApplicationDeactivated",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.NotNull(deactivatedHandler);

                deactivatedHandler.Invoke(window, [Application.Current, EventArgs.Empty]);
                PumpUntil(() => dateButton.Tag is null);

                Assert.DoesNotContain(panel, FindVisualChildren<WpfBorder>(window));
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LessonTimeSelectorClosesWhenWindowDeactivates()
    {
        RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel(
                dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null });

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Lessons.Count == 1);

                var timeSelector = FindButton(window, "Open lesson time selector");

                RaisePreviewLeftClick(timeSelector);
                PumpUntil(() => timeSelector.Tag is WpfBorder);
                var panel = Assert.IsType<WpfBorder>(timeSelector.Tag);

                var deactivatedHandler = typeof(AppMainWindow).GetMethod(
                    "OnWindowDeactivated",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.NotNull(deactivatedHandler);

                deactivatedHandler.Invoke(window, [window, EventArgs.Empty]);
                PumpUntil(() => timeSelector.Tag is null);

                Assert.DoesNotContain(panel, FindVisualChildren<WpfBorder>(window));
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LessonTimeSelectorClosesWhenApplicationDeactivates()
    {
        RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel(
                dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null });

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Lessons.Count == 1);

                var timeSelector = FindButton(window, "Open lesson time selector");

                RaisePreviewLeftClick(timeSelector);
                PumpUntil(() => timeSelector.Tag is WpfBorder);
                var panel = Assert.IsType<WpfBorder>(timeSelector.Tag);

                var deactivatedHandler = typeof(AppMainWindow).GetMethod(
                    "OnApplicationDeactivated",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.NotNull(deactivatedHandler);

                deactivatedHandler.Invoke(window, [Application.Current, EventArgs.Empty]);
                PumpUntil(() => timeSelector.Tag is null);

                Assert.DoesNotContain(panel, FindVisualChildren<WpfBorder>(window));
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void LessonGridUsesBorderedTableColumns()
    {
        RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel(
                dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null });

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Lessons.Count == 1);

                var lessonGrid = Assert.Single(
                    FindVisualChildren<WpfDataGrid>(window),
                    grid => AutomationProperties.GetName(grid) == "Lesson list");
                var headers = lessonGrid.Columns
                    .Select(column => column.Header?.ToString() ?? string.Empty)
                    .ToArray();

                Assert.Equal(["Date", "Time", "Plan", "State", "Invoiced", "Paid", string.Empty], headers);
                Assert.Equal(WpfDataGridGridLinesVisibility.All, lessonGrid.GridLinesVisibility);
                Assert.True(lessonGrid.CanUserResizeColumns);
                Assert.False(lessonGrid.CanUserReorderColumns);

                var dateButton = Assert.Single(
                    FindVisualChildren<WpfButton>(lessonGrid),
                    button => AutomationProperties.GetName(button) == "Open lesson date selector");
                Assert.Equal(6, dateButton.Padding.Left);

                var centeredHeaderStyle = Assert.IsType<Style>(window.FindResource("CenteredColumnHeaderStyle"));
                var centeredTextCellStyle = Assert.IsType<Style>(window.FindResource("CenteredTextCellStyle"));

                var stateColumn = Assert.IsType<WpfDataGridTemplateColumn>(
                    lessonGrid.Columns.Single(column => column.Header?.ToString() == "State"));
                Assert.Same(centeredHeaderStyle, stateColumn.HeaderStyle);

                Assert.IsType<WpfDataGridTemplateColumn>(
                    lessonGrid.Columns.Single(column => column.Header?.ToString() == "Plan"));

                var invoicedColumn = Assert.IsType<WpfDataGridTextColumn>(
                    lessonGrid.Columns.Single(column => column.Header?.ToString() == "Invoiced"));
                Assert.Same(centeredHeaderStyle, invoicedColumn.HeaderStyle);
                Assert.Same(centeredTextCellStyle, invoicedColumn.ElementStyle);

                var paidColumn = Assert.IsType<WpfDataGridTextColumn>(
                    lessonGrid.Columns.Single(column => column.Header?.ToString() == "Paid"));
                Assert.Same(centeredHeaderStyle, paidColumn.HeaderStyle);
                Assert.Same(centeredTextCellStyle, paidColumn.ElementStyle);
                Assert.False(paidColumn.CanUserResize);
                Assert.Equal(WpfDataGridLengthUnitType.Pixel, paidColumn.Width.UnitType);
                Assert.Equal(105, paidColumn.Width.Value);

                var deleteColumn = Assert.IsType<WpfDataGridTemplateColumn>(
                    lessonGrid.Columns.Single(column => string.IsNullOrEmpty(column.Header?.ToString())));
                Assert.False(deleteColumn.CanUserResize);
                Assert.Equal(28, deleteColumn.Width.Value);

                var lessonsPane = Assert.Single(
                    FindVisualChildren<WpfGrid>(window),
                    grid => AutomationProperties.GetName(grid) == "Lessons pane");
                Assert.Equal(640, lessonsPane.Width);

                var invoiceGrid = Assert.Single(
                    FindVisualChildren<WpfDataGrid>(window),
                    grid => AutomationProperties.GetName(grid) == "Student invoices");
                Assert.All(invoiceGrid.Columns, column => Assert.Equal(WpfDataGridLengthUnitType.Star, column.Width.UnitType));
                Assert.Equal(36, invoiceGrid.RowHeight);
                Assert.Equal([56, 84, 88, 70, 112], invoiceGrid.Columns.Select(column => column.MinWidth).ToArray());

                var invoiceStatusStyle = Assert.IsType<Style>(window.FindResource("InvoiceStatusButtonStyle"));
                var invoiceCellStyle = Assert.IsType<Style>(window.FindResource("InvoiceTableCellStyle"));
                var invoiceRowStyle = Assert.IsType<Style>(window.FindResource("InvoiceTableRowStyle"));
                var invoiceStatusButton = Assert.Single(
                    FindVisualChildren<WpfButton>(invoiceGrid),
                    button => AutomationProperties.GetName(button) == "Outstanding");
                Assert.Same(invoiceCellStyle, invoiceGrid.CellStyle);
                Assert.Same(invoiceRowStyle, invoiceGrid.RowStyle);
                Assert.Same(invoiceStatusStyle, invoiceStatusButton.Style);
                Assert.Equal(28, invoiceStatusButton.Height);
                Assert.Equal(new Thickness(6, 0, 6, 0), invoiceStatusButton.Margin);
                Assert.Equal(VerticalAlignment.Center, invoiceStatusButton.VerticalAlignment);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void AddLessonButtonWorksAfterStudentNavigation()
    {
        RunOnStaThread(() =>
        {
            var dashboard = new FakeDashboardService
            {
                StudentShortNames = ["Anna", "Bo"],
                LessonState = LessonState.Planned,
                LessonIsInvoiced = false,
                LessonPaymentStatus = null
            };
            var viewModel = CreateViewModel(dashboard: dashboard);

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.CurrentStudentShortName == "Anna");

                var nextStudentButton = FindButton(window, "Next student");
                InvokeButton(nextStudentButton);
                PumpUntil(() => !viewModel.IsBusy && viewModel.CurrentStudentShortName == "Bo");

                var previousStudentButton = FindButton(window, "Previous student");
                InvokeButton(previousStudentButton);
                PumpUntil(() => !viewModel.IsBusy && viewModel.CurrentStudentShortName == "Anna");

                var addLessonButton = FindButton(window, "Add Lesson");
                Assert.True(addLessonButton.Command?.CanExecute(addLessonButton.CommandParameter));
                InvokeButton(addLessonButton);
                PumpUntil(() => viewModel.Lessons.Count == 2);

                Assert.True(viewModel.SelectedLesson?.IsDraft);
                Assert.Equal("Anna", viewModel.SelectedLesson?.StudentShortName);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void AddLessonButtonPreviewClickWorksAfterStudentNavigation()
    {
        RunOnStaThread(() =>
        {
            var dashboard = new FakeDashboardService
            {
                StudentShortNames = ["Anna", "Bo"],
                LessonState = LessonState.Planned,
                LessonIsInvoiced = false,
                LessonPaymentStatus = null
            };
            var viewModel = CreateViewModel(dashboard: dashboard);

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.CurrentStudentShortName == "Anna");

                InvokeButton(FindButton(window, "Next student"));
                PumpUntil(() => !viewModel.IsBusy && viewModel.CurrentStudentShortName == "Bo");

                InvokeButton(FindButton(window, "Previous student"));
                PumpUntil(() => !viewModel.IsBusy && viewModel.CurrentStudentShortName == "Anna");

                RaisePreviewLeftClick(FindButton(window, "Add Lesson"));
                PumpUntil(() => viewModel.Lessons.Count == 2);

                Assert.True(viewModel.SelectedLesson?.IsDraft);
                Assert.Equal("Anna", viewModel.SelectedLesson?.StudentShortName);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void AddLessonButtonPreviewClickWorksWhileActivationRefreshIsPending()
    {
        RunOnStaThread(() =>
        {
            var activationRefreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var allowActivationRefreshToFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var loadCount = 0;
            var clock = new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));
            var dashboard = new FakeDashboardService
            {
                BeforeReturnAsync = _ =>
                {
                    loadCount++;
                    if (loadCount != 2)
                    {
                        return Task.CompletedTask;
                    }

                    activationRefreshStarted.SetResult();
                    return allowActivationRefreshToFinish.Task;
                }
            };
            var viewModel = CreateViewModel(dashboard: dashboard, clock: clock);

            var window = new AppMainWindow(viewModel);
            try
            {
                window.Show();
                PumpUntil(() => !viewModel.IsBusy && viewModel.Lessons.Count == 1);
                clock.Now = clock.Now.AddSeconds(11);

                InvokeActivated(window);
                RaisePreviewLeftClick(FindButton(window, "Add Lesson"));

                Assert.True(viewModel.SelectedLesson?.IsDraft);
                Assert.Equal(2, viewModel.Lessons.Count);

                PumpUntil(() => activationRefreshStarted.Task.IsCompleted);
                allowActivationRefreshToFinish.SetResult();
                PumpUntil(() => !viewModel.IsBusy);
            }
            finally
            {
                allowActivationRefreshToFinish.TrySetResult();
                window.Close();
            }
        });
    }

    [Fact]
    public async Task LoadPopulatesCurrentStudentLessonsAndInvoices()
    {
        var dashboard = new FakeDashboardService();
        var viewModel = CreateViewModel(dashboard: dashboard);

        await viewModel.LoadAsync();

        Assert.Equal("Anna", viewModel.CurrentStudentShortName);
        Assert.Single(viewModel.Lessons);
        Assert.Equal("16-06-2026 15:00", viewModel.Lessons[0].StartsAtText);
        Assert.Equal("Tuesday 16-06", viewModel.Lessons[0].StartsOnDateText);
        Assert.Equal("Completed", viewModel.Lessons[0].StateText);
        Assert.Equal("Yes", viewModel.Lessons[0].InvoicedText);
        Assert.Equal("Outstanding", viewModel.Lessons[0].PaymentStatusText);
        Assert.Single(viewModel.Invoices);
        Assert.Equal("001", viewModel.Invoices[0].InvoiceNumberText);
        Assert.Equal("Juni 2026", viewModel.Invoices[0].PeriodText);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task MonthNavigationRefreshesDashboardForAdjacentMonth()
    {
        var dashboard = new FakeDashboardService();
        var viewModel = CreateViewModel(dashboard: dashboard);
        await viewModel.LoadAsync();

        await viewModel.NextMonthCommand.ExecuteAsync();

        Assert.Equal(new DateOnly(2026, 7, 1), dashboard.LastMonth);

        await viewModel.PreviousMonthCommand.ExecuteAsync();

        Assert.Equal(new DateOnly(2026, 6, 1), dashboard.LastMonth);
    }

    [Fact]
    public async Task AddLessonDefaultsDraftDateToFirstDayOfDisplayedNonCurrentMonth()
    {
        var viewModel = CreateViewModel(clock: new FakeUiClock(new DateTimeOffset(2026, 6, 26, 12, 0, 0, TimeSpan.Zero)));
        await viewModel.LoadAsync();
        await viewModel.NextMonthCommand.ExecuteAsync();

        await viewModel.AddLessonCommand.ExecuteAsync();

        Assert.Equal(new DateTime(2026, 7, 1), viewModel.SelectedLesson?.StartsOnDate);
    }

    [Fact]
    public async Task MonthNavigationShowsOnlySelectedMonthLessons()
    {
        var dashboard = new FakeDashboardService();
        var viewModel = CreateViewModel(dashboard: dashboard);
        await viewModel.LoadAsync();

        dashboard.NextResult = OperationResult<StartupDashboardResult>.Success(
            new StartupDashboardResult(
                "Anna",
                ["Anna"],
                [
                    new DashboardLesson(
                        "june",
                        "Anna",
                        new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.FromHours(2)),
                        LessonState.Planned,
                        false,
                        null),
                    new DashboardLesson(
                        "july",
                        "Anna",
                        new DateTimeOffset(2026, 7, 7, 15, 0, 0, TimeSpan.FromHours(2)),
                        LessonState.Planned,
                        false,
                        null),
                    new DashboardLesson(
                        "august",
                        "Anna",
                        new DateTimeOffset(2026, 8, 4, 15, 0, 0, TimeSpan.FromHours(2)),
                        LessonState.Planned,
                        false,
                        null)
                ],
                [],
                CurrentStudentHasSchedule: true,
                StudentNavigationEntries:
                [
                    new DashboardStudentNavigationEntry("Anna", new DateTimeOffset(2026, 7, 7, 15, 0, 0, TimeSpan.FromHours(2)))
                ]));

        await viewModel.NextMonthCommand.ExecuteAsync();

        var lesson = Assert.Single(viewModel.Lessons);
        Assert.Equal("july", lesson.Id);
    }

    [Fact]
    public async Task StudentNavigationUsesDashboardOccurrenceListAndCanRepeatCurrentStudent()
    {
        var annaFirst = new DateTimeOffset(2026, 6, 2, 15, 0, 0, TimeSpan.FromHours(2));
        var annaSecond = new DateTimeOffset(2026, 6, 9, 15, 0, 0, TimeSpan.FromHours(2));
        var boFirst = new DateTimeOffset(2026, 6, 10, 14, 0, 0, TimeSpan.FromHours(2));
        var dashboard = new FakeDashboardService
        {
            StudentShortNames = ["Anna", "Bo"],
            StudentNavigationEntries =
            [
                new DashboardStudentNavigationEntry("Anna", annaFirst),
                new DashboardStudentNavigationEntry("Anna", annaSecond),
                new DashboardStudentNavigationEntry("Bo", boFirst)
            ],
            DashboardLessons =
            [
                new DashboardLesson("anna-1", "Anna", annaFirst, LessonState.Planned, false, null),
                new DashboardLesson("anna-2", "Anna", annaSecond, LessonState.Planned, false, null)
            ]
        };
        var viewModel = CreateViewModel(dashboard: dashboard);
        await viewModel.LoadAsync();

        await viewModel.NextStudentCommand.ExecuteAsync();

        Assert.Equal("Anna", dashboard.LastSelectedStudentShortName);
        Assert.Equal("Anna", viewModel.CurrentStudentShortName);
        Assert.Equal("anna-2", viewModel.SelectedLesson?.Id);

        await viewModel.NextStudentCommand.ExecuteAsync();

        Assert.Equal("Bo", dashboard.LastSelectedStudentShortName);
        Assert.Equal("Bo", viewModel.CurrentStudentShortName);
    }

    [Fact]
    public async Task StudentNavigationMovesToAdjacentMonthAndSelectsTargetLesson()
    {
        var annaJune = new DateTimeOffset(2026, 6, 29, 15, 0, 0, TimeSpan.FromHours(2));
        var boJuly = new DateTimeOffset(2026, 7, 6, 14, 0, 0, TimeSpan.FromHours(2));
        var dashboard = new FakeDashboardService
        {
            StudentShortNames = ["Anna", "Bo"],
            StudentNavigationEntries =
            [
                new DashboardStudentNavigationEntry("Anna", annaJune),
                new DashboardStudentNavigationEntry("Bo", boJuly)
            ],
            DashboardLessons =
            [
                new DashboardLesson("anna-june", "Anna", annaJune, LessonState.Planned, false, null),
                new DashboardLesson("bo-july", "Bo", boJuly, LessonState.Planned, false, null)
            ]
        };
        var viewModel = CreateViewModel(dashboard: dashboard);
        await viewModel.LoadAsync();

        await viewModel.NextStudentCommand.ExecuteAsync();

        Assert.Equal(new DateOnly(2026, 7, 1), viewModel.DisplayMonth);
        Assert.Equal(new DateOnly(2026, 7, 1), dashboard.LastMonth);
        Assert.Equal("Bo", viewModel.CurrentStudentShortName);
        Assert.Equal("bo-july", viewModel.SelectedLesson?.Id);
        var lesson = Assert.Single(viewModel.Lessons);
        Assert.Equal("bo-july", lesson.Id);
    }

    [Fact]
    public async Task StudentDropdownSelectionRefreshesDashboardWithoutSelectingLesson()
    {
        var dashboard = new FakeDashboardService { StudentShortNames = ["Bo", "Anna"] };
        var viewModel = CreateViewModel(dashboard: dashboard);
        await viewModel.LoadAsync();

        Assert.Equal(["Bo"], viewModel.SelectableStudentShortNames);

        await viewModel.SelectStudentCommand.ExecuteAsync("Bo");

        Assert.Equal("Bo", dashboard.LastSelectedStudentShortName);
        Assert.Equal("Bo", viewModel.CurrentStudentShortName);
        Assert.Null(viewModel.SelectedLesson);
        Assert.Equal(["Anna"], viewModel.SelectableStudentShortNames);
        Assert.Equal(2, dashboard.LoadCount);
    }

    [Fact]
    public async Task RefreshOnActivationIsDebounced()
    {
        var clock = new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));
        var dashboard = new FakeDashboardService();
        var viewModel = CreateViewModel(dashboard: dashboard, clock: clock);
        await viewModel.LoadAsync();

        await viewModel.RefreshAfterActivationAsync();
        clock.Now = clock.Now.AddSeconds(4);
        await viewModel.RefreshAfterActivationAsync();
        clock.Now = clock.Now.AddSeconds(6);
        await viewModel.RefreshAfterActivationAsync();

        Assert.Equal(2, dashboard.LoadCount);
    }

    [Fact]
    public async Task ManualRefreshShowsCalendarErrorWithoutClearingExistingRows()
    {
        var dashboard = new FakeDashboardService();
        var viewModel = CreateViewModel(dashboard: dashboard);
        await viewModel.LoadAsync();
        dashboard.NextResult = OperationResult<StartupDashboardResult>.Failure(
            new ValidationError("Calendar.RefreshFailed", "Could not refresh Google Calendar."));

        await viewModel.RefreshCommand.ExecuteAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal("Could not refresh Google Calendar.", viewModel.ErrorMessage);
        Assert.Single(viewModel.Lessons);
    }

    [Fact]
    public async Task OutstandingInvoiceTogglesToPaidWithoutConfirmation()
    {
        var payments = new FakePaymentService();
        var viewModel = CreateViewModel(payments: payments);
        await viewModel.LoadAsync();

        await viewModel.ToggleInvoicePaymentStatusCommand.ExecuteAsync(viewModel.Invoices[0]);

        Assert.Equal(new InvoiceNumber(1), payments.LastInvoiceNumber);
        Assert.False(payments.LastConfirmMarkOutstanding);
        Assert.Equal("Paid", viewModel.Invoices[0].PaymentStatusText);
    }

    [Fact]
    public async Task PaidInvoiceRequiresConfirmationBeforeMarkingOutstanding()
    {
        var payments = new FakePaymentService();
        var confirmation = new FakeConfirmationService { NextAnswer = true };
        var viewModel = CreateViewModel(payments: payments, confirmation: confirmation);
        await viewModel.LoadAsync();
        await viewModel.ToggleInvoicePaymentStatusCommand.ExecuteAsync(viewModel.Invoices[0]);

        await viewModel.ToggleInvoicePaymentStatusCommand.ExecuteAsync(viewModel.Invoices[0]);

        Assert.True(confirmation.WasAsked);
        Assert.True(payments.LastConfirmMarkOutstanding);
        Assert.Equal("Outstanding", viewModel.Invoices[0].PaymentStatusText);
    }

    [Fact]
    public async Task InvoicePaymentStatusUpdatesVisibleLessonRowsImmediately()
    {
        var payments = new FakePaymentService();
        var confirmation = new FakeConfirmationService { NextAnswer = true };
        var viewModel = CreateViewModel(payments: payments, confirmation: confirmation);
        await viewModel.LoadAsync();

        await viewModel.ToggleInvoicePaymentStatusCommand.ExecuteAsync(viewModel.Invoices[0]);

        Assert.Equal("Paid", viewModel.Invoices[0].PaymentStatusText);
        Assert.Equal("Paid", viewModel.Lessons[0].PaymentStatusText);

        await viewModel.ToggleInvoicePaymentStatusCommand.ExecuteAsync(viewModel.Invoices[0]);

        Assert.Equal("Outstanding", viewModel.Invoices[0].PaymentStatusText);
        Assert.Equal("Outstanding", viewModel.Lessons[0].PaymentStatusText);
    }

    [Fact]
    public async Task PaidInvoiceReturnToOutstandingIsNotLostWhenActivationRefreshRunsDuringPaymentUpdate()
    {
        var clock = new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));
        var payments = new FakePaymentService();
        var confirmation = new FakeConfirmationService { NextAnswer = true };
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { InvoicePaymentStatus = PaymentStatus.Paid },
            payments: payments,
            confirmation: confirmation,
            clock: clock);
        payments.BeforeReturnAsync = () => viewModel.RefreshAfterActivationAsync();
        await viewModel.LoadAsync();
        clock.Now = clock.Now.AddSeconds(11);

        await viewModel.ToggleInvoicePaymentStatusCommand.ExecuteAsync(viewModel.Invoices[0]);

        Assert.True(confirmation.WasAsked);
        Assert.Equal("Outstanding", viewModel.Invoices[0].PaymentStatusText);
    }

    [Fact]
    public async Task OpenCalendarCommandUsesExternalBrowserService()
    {
        var calendar = new FakeCalendarLauncher();
        var viewModel = CreateViewModel(calendar: calendar);

        await viewModel.OpenCalendarCommand.ExecuteAsync();

        Assert.True(calendar.WasOpened);
    }

    [Fact]
    public async Task WriteInvoiceLaunchesWorkflowAndRefreshesAfterIssue()
    {
        var dashboard = new FakeDashboardService
        {
            LessonIsInvoiced = false,
            LessonPaymentStatus = null,
            InvoiceSummaries = []
        };
        var invoices = new FakeInvoiceWorkflowLauncher { NextWriteResult = true };
        var viewModel = CreateViewModel(dashboard: dashboard, invoiceWorkflowLauncher: invoices);
        await viewModel.LoadAsync();

        await viewModel.WriteInvoiceCommand.ExecuteAsync();

        Assert.True(invoices.WriteWasShown);
        Assert.Equal(2, dashboard.LoadCount);
    }

    [Fact]
    public async Task WriteInvoicePassesCurrentStudentToWorkflow()
    {
        var dashboard = new FakeDashboardService
        {
            StudentShortNames = ["Anna", "Mira"],
            LessonIsInvoiced = false,
            LessonPaymentStatus = null,
            InvoiceSummaries = []
        };
        var invoices = new FakeInvoiceWorkflowLauncher();
        var viewModel = CreateViewModel(dashboard: dashboard, invoiceWorkflowLauncher: invoices);
        await viewModel.LoadAsync();

        await viewModel.SelectStudentCommand.ExecuteAsync("Mira");
        await viewModel.WriteInvoiceCommand.ExecuteAsync();

        Assert.Equal("Mira", invoices.LastWriteInitialStudentShortName);
    }

    [Fact]
    public async Task WriteInvoiceOpensExistingInvoiceForVisibleInvoicedLessons()
    {
        var invoices = new FakeInvoiceWorkflowLauncher();
        var viewModel = CreateViewModel(invoiceWorkflowLauncher: invoices);
        await viewModel.LoadAsync();

        await viewModel.WriteInvoiceCommand.ExecuteAsync();

        Assert.False(invoices.WriteWasShown);
        Assert.Equal(new InvoiceNumber(1), invoices.LastViewedInvoiceNumber);
    }

    [Fact]
    public async Task WriteInvoiceOpensSelectedInvoiceWhenInvoiceRowIsSelected()
    {
        var invoices = new FakeInvoiceWorkflowLauncher();
        var viewModel = CreateViewModel(invoiceWorkflowLauncher: invoices);
        await viewModel.LoadAsync();

        viewModel.SelectedInvoice = viewModel.Invoices[0];
        await viewModel.WriteInvoiceCommand.ExecuteAsync();

        Assert.False(invoices.WriteWasShown);
        Assert.Equal(new InvoiceNumber(1), invoices.LastViewedInvoiceNumber);
    }

    [Fact]
    public async Task WriteInvoiceStartsCurrentMonthFlowWhenVisibleLessonsAreUninvoicedEvenIfInvoiceRowIsSelected()
    {
        var dashboard = new FakeDashboardService
        {
            LessonIsInvoiced = false,
            LessonPaymentStatus = null,
            InvoiceSummaries =
            [
                new IssuedInvoiceSummary(
                    new InvoiceNumber(1),
                    "Anna",
                    new DateOnly(2026, 6, 25),
                    PaymentStatus.Outstanding,
                    "C:\\Invoices\\001-Anna.pdf")
            ]
        };
        var invoices = new FakeInvoiceWorkflowLauncher { NextWriteResult = true };
        var viewModel = CreateViewModel(dashboard: dashboard, invoiceWorkflowLauncher: invoices);
        await viewModel.LoadAsync();

        viewModel.SelectedInvoice = viewModel.Invoices[0];
        await viewModel.WriteInvoiceCommand.ExecuteAsync();

        Assert.True(invoices.WriteWasShown);
        Assert.Null(invoices.LastViewedInvoiceNumber);
        Assert.Equal(2, dashboard.LoadCount);
    }

    [Fact]
    public async Task WriteInvoiceWaitsForActivationRefreshInsteadOfDroppingClick()
    {
        var activationRefreshGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loadCall = 0;
        var clock = new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));
        var dashboard = new FakeDashboardService
        {
            LessonIsInvoiced = false,
            LessonPaymentStatus = null,
            InvoiceSummaries = [],
            BeforeReturnAsync = _ =>
            {
                loadCall++;
                return loadCall == 2
                    ? activationRefreshGate.Task
                    : Task.CompletedTask;
            }
        };
        var invoices = new FakeInvoiceWorkflowLauncher();
        var viewModel = CreateViewModel(dashboard: dashboard, invoiceWorkflowLauncher: invoices, clock: clock);
        await viewModel.LoadAsync();
        clock.Now = clock.Now.AddSeconds(11);

        var activationRefresh = viewModel.RefreshAfterActivationAsync();
        await EventuallyAsync(() => viewModel.IsBusy);

        var writeInvoice = viewModel.WriteInvoiceCommand.ExecuteAsync();
        Assert.False(invoices.WriteWasShown);

        activationRefreshGate.SetResult();
        await Task.WhenAll(activationRefresh, writeInvoice);

        Assert.True(invoices.WriteWasShown);
    }

    [Fact]
    public async Task SettingsRefreshesDashboardAfterSave()
    {
        var dashboard = new FakeDashboardService();
        var settings = new FakeSettingsLauncher { NextResult = true };
        var viewModel = CreateViewModel(dashboard: dashboard, settingsLauncher: settings);
        await viewModel.LoadAsync();

        await viewModel.OpenSettingsCommand.ExecuteAsync();

        Assert.True(settings.WasShown);
        Assert.Equal(2, dashboard.LoadCount);
    }

    [Fact]
    public async Task ManagePlansCommandOpensPlanCatalog()
    {
        var planCatalog = new FakePlanCatalogLauncher();
        var viewModel = CreateViewModel(planCatalogLauncher: planCatalog);

        await viewModel.OpenManagePlansCommand.ExecuteAsync();

        Assert.True(planCatalog.WasShown);
    }

    [Fact]
    public async Task ManageStudentsCommandRefreshesDashboardWithSelectedOverviewStudent()
    {
        var dashboard = new FakeDashboardService { StudentShortNames = ["Anna", "Bo"] };
        var studentOverview = new FakeStudentOverviewLauncher { SelectedStudentShortName = "Bo" };
        var viewModel = CreateViewModel(dashboard: dashboard, studentOverviewLauncher: studentOverview);
        await viewModel.LoadAsync();

        await viewModel.OpenManageStudentsCommand.ExecuteAsync();

        Assert.True(studentOverview.WasShown);
        Assert.Equal("Anna", studentOverview.InitialSelectedStudentShortName);
        Assert.Equal("Bo", dashboard.LastSelectedStudentShortName);
        Assert.Equal("Bo", viewModel.CurrentStudentShortName);
        Assert.Equal(2, dashboard.LoadCount);
    }

    [Fact]
    public async Task ManageStudentsSelectionImmediatelyRefreshesDashboard()
    {
        var dashboard = new FakeDashboardService { StudentShortNames = ["Anna", "Erik"] };
        var selectionRefreshWasSeenBeforeClose = false;
        var studentOverview = new FakeStudentOverviewLauncher
        {
            SelectionChangedStudentShortName = "Erik",
            OnAfterSelectionChanged = () =>
            {
                selectionRefreshWasSeenBeforeClose =
                    dashboard.LastSelectedStudentShortName == "Erik" &&
                    dashboard.LoadCount == 2;
            },
            SelectedStudentShortName = "Erik"
        };
        var viewModel = CreateViewModel(dashboard: dashboard, studentOverviewLauncher: studentOverview);
        await viewModel.LoadAsync();

        await viewModel.OpenManageStudentsCommand.ExecuteAsync();

        Assert.True(selectionRefreshWasSeenBeforeClose);
        Assert.Equal("Erik", viewModel.CurrentStudentShortName);
    }

    [Fact]
    public async Task ManageStudentsIgnoresStaleLiveSelectionRefreshes()
    {
        var erikStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseErik = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var boStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dashboard = new FakeDashboardService
        {
            StudentShortNames = ["Anna", "Bo", "Erik"],
            BeforeReturnAsync = async selectedStudentShortName =>
            {
                if (selectedStudentShortName == "Erik")
                {
                    erikStarted.SetResult();
                    await releaseErik.Task;
                }
                else if (selectedStudentShortName == "Bo")
                {
                    boStarted.SetResult();
                }
            }
        };
        var studentOverview = new FakeStudentOverviewLauncher
        {
            SelectionChangedStudentShortNames = ["Erik", "Bo"],
            FireSelectionChangesConcurrently = true
        };
        var viewModel = CreateViewModel(dashboard: dashboard, studentOverviewLauncher: studentOverview);
        await viewModel.LoadAsync();

        var openTask = viewModel.OpenManageStudentsCommand.ExecuteAsync();
        await erikStarted.Task;
        await boStarted.Task;
        releaseErik.SetResult();
        await openTask;

        Assert.Equal("Bo", viewModel.CurrentStudentShortName);
    }

    [Fact]
    public async Task OpenInvoiceLaunchesViewerForSelectedInvoice()
    {
        var invoices = new FakeInvoiceWorkflowLauncher();
        var viewModel = CreateViewModel(invoiceWorkflowLauncher: invoices);
        await viewModel.LoadAsync();

        await viewModel.OpenInvoiceCommand.ExecuteAsync(viewModel.Invoices[0]);

        Assert.Equal(new InvoiceNumber(1), invoices.LastViewedInvoiceNumber);
    }

    [Fact]
    public async Task OpenInvoicePdfLaunchesPublishedPdfAndPreservesSelection()
    {
        var pdfLauncher = new FakeInvoicePdfLauncher();
        var viewModel = CreateViewModel(invoicePdfLauncher: pdfLauncher);
        await viewModel.LoadAsync();
        viewModel.SelectedInvoice = viewModel.Invoices[0];

        await viewModel.OpenInvoicePdfCommand.ExecuteAsync(viewModel.Invoices[0]);

        Assert.Equal("C:\\Invoices\\000001.pdf", pdfLauncher.LastOpenedPdfFilePath);
        Assert.Same(viewModel.Invoices[0], viewModel.SelectedInvoice);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task OpenInvoicePdfShowsErrorWhenNoPublishedPdfExists()
    {
        var pdfLauncher = new FakeInvoicePdfLauncher();
        var dashboard = new FakeDashboardService
        {
            InvoiceSummaries =
            [
                new IssuedInvoiceSummary(
                    new InvoiceNumber(1),
                    "Anna",
                    new DateOnly(2026, 6, 16),
                    PaymentStatus.Outstanding,
                    string.Empty)
            ]
        };
        var viewModel = CreateViewModel(dashboard: dashboard, invoicePdfLauncher: pdfLauncher);
        await viewModel.LoadAsync();

        await viewModel.OpenInvoicePdfCommand.ExecuteAsync(viewModel.Invoices[0]);

        Assert.Null(pdfLauncher.LastOpenedPdfFilePath);
        Assert.True(viewModel.HasError);
        Assert.Equal("The selected invoice does not have a saved PDF file yet.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task AddLessonAddsEditableDraftRowWithoutOpeningDialog()
    {
        var lessonEditorLauncher = new FakeLessonEditorLauncher();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
            lessonEditorLauncher: lessonEditorLauncher);
        await viewModel.LoadAsync();

        await viewModel.AddLessonCommand.ExecuteAsync();

        Assert.False(lessonEditorLauncher.AddWasShown);
        Assert.Equal(2, viewModel.Lessons.Count);
        var draft = viewModel.Lessons[1];
        Assert.True(draft.IsDraft);
        Assert.Equal("Anna", draft.StudentShortName);
        Assert.Null(draft.StartsOnDate);
        Assert.Equal(string.Empty, draft.StartsAtTimeText);
        Assert.True(draft.CanEdit);
    }

    [Fact]
    public async Task LessonRowsOfferTeachingDayTimeOptions()
    {
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null });
        await viewModel.LoadAsync();

        var options = viewModel.Lessons[0].TimeOptions;

        Assert.Equal("08:00", options.First());
        Assert.Equal("20:00", options.Last());
        Assert.DoesNotContain("07:45", options);
        Assert.DoesNotContain("20:15", options);
    }

    [Fact]
    public async Task DatedDraftLessonSortsIntoLessonListAndRemainsSelected()
    {
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
            lessonTimeZone: TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
        await viewModel.LoadAsync();
        await viewModel.AddLessonCommand.ExecuteAsync();
        var draft = viewModel.SelectedLesson!;

        draft.StartsAtTimeText = "14:00";
        draft.StartsOnDate = new DateTime(2026, 6, 15);

        Assert.Same(draft, viewModel.Lessons[0]);
        Assert.Same(draft, viewModel.SelectedLesson);
        Assert.Equal("Monday 15-06", draft.StartsOnDateText);
    }

    [Fact]
    public async Task CompleteDraftLessonAutosavesWhenDateAndTimeAreSet()
    {
        var lessonEditor = new FakeLessonEditorService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
            lessonEditorService: lessonEditor,
            lessonTimeZone: TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
        await viewModel.LoadAsync();
        await viewModel.AddLessonCommand.ExecuteAsync();
        var draft = viewModel.SelectedLesson!;

        draft.StartsOnDate = new DateTime(2026, 6, 15);
        draft.StartsAtTimeText = "14:00";

        await EventuallyAsync(() => lessonEditor.AddWasCalled);
        Assert.Equal("Anna", lessonEditor.LastAddRequest?.StudentShortName);
        Assert.Equal(new DateTimeOffset(2026, 6, 15, 14, 0, 0, TimeSpan.FromHours(2)), lessonEditor.LastAddRequest?.StartsAt);
        Assert.False(draft.IsDraft);
        Assert.Same(draft, viewModel.SelectedLesson);
    }

    [Fact]
    public async Task AutosavedDraftRaisesLessonStateCommandAvailability()
    {
        var lessonEditor = new FakeLessonEditorService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
            lessonEditorService: lessonEditor,
            lessonTimeZone: TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
        await viewModel.LoadAsync();
        await viewModel.AddLessonCommand.ExecuteAsync();
        var draft = viewModel.SelectedLesson!;
        var availabilityChanged = false;
        viewModel.ToggleLessonStateCommand.CanExecuteChanged += (_, _) => availabilityChanged = true;

        draft.StartsOnDate = new DateTime(2026, 6, 15);
        draft.StartsAtTimeText = "14:00";

        await EventuallyAsync(() => !draft.IsDraft);
        Assert.True(draft.CanToggleState);
        Assert.True(viewModel.ToggleLessonStateCommand.CanExecute(draft));
        Assert.True(availabilityChanged);
    }

    [Fact]
    public async Task ActivationRefreshKeepsIncompleteDraftLessonSelected()
    {
        var clock = new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.FromHours(2)));
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
            clock: clock);
        await viewModel.LoadAsync();
        await viewModel.AddLessonCommand.ExecuteAsync();
        var draft = viewModel.SelectedLesson!;
        draft.StartsOnDate = new DateTime(2026, 6, 1);
        clock.Now = clock.Now.AddSeconds(11);

        await viewModel.RefreshAfterActivationAsync();

        Assert.Contains(draft, viewModel.Lessons);
        Assert.Same(draft, viewModel.SelectedLesson);
        Assert.True(draft.IsDraft);
    }

    [Fact]
    public async Task PopulateCalendarCommandPopulatesSelectedStudentMonthAndRefreshesDashboard()
    {
        var dashboard = new FakeDashboardService
        {
            LessonState = LessonState.Planned,
            LessonIsInvoiced = false,
            LessonPaymentStatus = null
        };
        var lessonEditor = new FakeLessonEditorService();
        var viewModel = CreateViewModel(
            dashboard: dashboard,
            lessonEditorService: lessonEditor,
            clock: new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.FromHours(2))));
        await viewModel.LoadAsync();

        await viewModel.PopulateCalendarCommand.ExecuteAsync();

        Assert.Equal("Anna", lessonEditor.LastPopulateStudentShortName);
        Assert.Equal(new DateOnly(2026, 6, 1), lessonEditor.LastPopulateDisplayMonth);
        Assert.Equal(new DateOnly(2026, 6, 16), lessonEditor.LastPopulateToday);
        Assert.Equal(2, dashboard.LoadCount);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task PopulateCalendarCommandIsUnavailableForPastMonths()
    {
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService
            {
                LessonState = LessonState.Planned,
                LessonIsInvoiced = false,
                LessonPaymentStatus = null
            },
            clock: new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.FromHours(2))));
        await viewModel.LoadAsync();

        await viewModel.PreviousMonthCommand.ExecuteAsync();

        Assert.False(viewModel.PopulateCalendarCommand.CanExecute(null));
    }

    [Fact]
    public async Task PopulateCalendarCommandStillCallsUseCaseWhenDashboardScheduleFlagIsStale()
    {
        var lessonEditor = new FakeLessonEditorService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService
            {
                CurrentStudentHasSchedule = false,
                LessonState = LessonState.Planned,
                LessonIsInvoiced = false,
                LessonPaymentStatus = null
            },
            lessonEditorService: lessonEditor);
        await viewModel.LoadAsync();

        await viewModel.PopulateCalendarCommand.ExecuteAsync();

        Assert.Equal("Anna", lessonEditor.LastPopulateStudentShortName);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task PopulateCalendarFailureShowsErrorWithoutClearingRows()
    {
        var lessonEditor = new FakeLessonEditorService
        {
            PopulateResult = OperationResult<PopulateCalendarResult>.Failure(
                new ValidationError("Calendar.UpdateFailed", "Google Calendar could not be updated."))
        };
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService
            {
                LessonState = LessonState.Planned,
                LessonIsInvoiced = false,
                LessonPaymentStatus = null
            },
            lessonEditorService: lessonEditor);
        await viewModel.LoadAsync();

        await viewModel.PopulateCalendarCommand.ExecuteAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal("Google Calendar could not be updated.", viewModel.ErrorMessage);
        Assert.Single(viewModel.Lessons);
    }

    [Fact]
    public async Task SavingDraftLessonUsesEditedDateOffsetForLocalTimeZone()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        var lessonEditor = new FakeLessonEditorService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
            lessonEditorService: lessonEditor,
            clock: new FakeUiClock(new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.FromHours(2))),
            lessonTimeZone: timeZone);
        await viewModel.LoadAsync();
        await viewModel.AddLessonCommand.ExecuteAsync();
        var draft = Assert.Single(viewModel.Lessons, lesson => lesson.IsDraft);
        draft.StartsOnDate = new DateTime(2026, 1, 15);
        draft.StartsAtTimeText = "15:30";

        await viewModel.SaveLessonCommand.ExecuteAsync(draft);

        Assert.Equal(
            new DateTimeOffset(2026, 1, 15, 15, 30, 0, TimeSpan.FromHours(1)),
            lessonEditor.LastAddRequest?.StartsAt);
    }

    [Fact]
    public async Task SavingCompleteDraftLessonAddsCalendarLesson()
    {
        var lessonEditor = new FakeLessonEditorService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
            lessonEditorService: lessonEditor,
            lessonTimeZone: TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
        await viewModel.LoadAsync();
        await viewModel.AddLessonCommand.ExecuteAsync();
        var draft = viewModel.Lessons[1];
        draft.StartsOnDate = new DateTime(2026, 6, 24);
        draft.StartsAtTimeText = "15:30";

        await viewModel.SaveLessonCommand.ExecuteAsync(draft);

        Assert.True(lessonEditor.AddWasCalled);
        Assert.Equal("Anna", lessonEditor.LastAddRequest?.StudentShortName);
        Assert.Equal(new DateTimeOffset(2026, 6, 24, 15, 30, 0, TimeSpan.FromHours(2)), lessonEditor.LastAddRequest?.StartsAt);
        Assert.False(draft.IsDraft);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task ChangingUninvoicedLessonPlanSavesLocalPlanSelection()
    {
        var lessonEditor = new FakeLessonEditorService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService
            {
                LessonState = LessonState.Planned,
                LessonIsInvoiced = false,
                LessonPaymentStatus = null,
                DashboardLessons =
                [
                    new DashboardLesson(
                        "lesson-1",
                        "Anna",
                        new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.FromHours(2)),
                        LessonState.Planned,
                        IsInvoiced: false,
                        PaymentStatus: null,
                        PlanCatalogId: 42,
                        PlanName: "Standard",
                        LocalLessonId: 9)
                ],
                PlanOptions =
                [
                    new DashboardPlanOption(42, "Standard"),
                    new DashboardPlanOption(77, "Proevetime")
                ]
            },
            lessonEditorService: lessonEditor);
        await viewModel.LoadAsync();

        viewModel.Lessons[0].PlanCatalogId = 77;

        await EventuallyAsync(() => lessonEditor.LastPlanLessonId == 9);
        Assert.Equal(77, lessonEditor.LastPlanCatalogId);
        Assert.Equal("Proevetime", viewModel.Lessons[0].PlanName);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task InvoicedLessonEditIsBlockedWithoutOpeningDialog()
    {
        var lessonEditor = new FakeLessonEditorLauncher();
        var viewModel = new MainWindowViewModel(
            new FakeDashboardService(),
            new FakePaymentService(),
            new FakeConfirmationService(),
            new FakeCalendarLauncher(),
            new FakeStudentOverviewLauncher(),
            lessonEditor,
            new FakeLessonEditorService(),
            new FakeLessonStateService(),
            new FakeInvoiceWorkflowLauncher(),
            new FakeInvoicePdfLauncher(),
            new FakeSettingsLauncher(),
            new FakePlanCatalogLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero)));
        await viewModel.LoadAsync();

        await viewModel.SaveLessonCommand.ExecuteAsync(viewModel.Lessons[0]);

        Assert.False(lessonEditor.EditWasShown);
        Assert.True(viewModel.HasError);
    }

    [Fact]
    public async Task PlannedLessonCanBeMarkedCompletedManually()
    {
        var lessonStates = new FakeLessonStateService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
            lessonStates: lessonStates);
        await viewModel.LoadAsync();

        await viewModel.ToggleLessonStateCommand.ExecuteAsync(viewModel.Lessons[0]);

        Assert.Equal("lesson-1", lessonStates.LastLessonId);
        Assert.Equal(LessonState.Planned, lessonStates.LastCurrentState);
        Assert.Equal("Completed", viewModel.Lessons[0].StateText);
        Assert.False(viewModel.Lessons[0].CanEdit);
    }

    [Fact]
    public async Task CompletedLessonRequiresConfirmationBeforeReturningToPlanned()
    {
        var confirmation = new FakeConfirmationService { NextAnswer = true };
        var lessonStates = new FakeLessonStateService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Completed, LessonIsInvoiced = false, LessonPaymentStatus = null },
            confirmation: confirmation,
            lessonStates: lessonStates);
        await viewModel.LoadAsync();

        await viewModel.ToggleLessonStateCommand.ExecuteAsync(viewModel.Lessons[0]);

        Assert.True(confirmation.MarkLessonPlannedWasAsked);
        Assert.Equal(LessonState.Completed, lessonStates.LastCurrentState);
        Assert.Equal("Planned", viewModel.Lessons[0].StateText);
        Assert.True(viewModel.Lessons[0].CanEdit);
    }

    [Fact]
    public async Task CompletedLessonReturnToPlannedIsNotLostWhenConfirmationReactivatesWindow()
    {
        var clock = new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));
        var confirmation = new FakeConfirmationService { NextAnswer = true };
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Completed, LessonIsInvoiced = false, LessonPaymentStatus = null },
            confirmation: confirmation,
            clock: clock);
        confirmation.OnMarkLessonPlannedAsync = () => viewModel.RefreshAfterActivationAsync();
        await viewModel.LoadAsync();
        clock.Now = clock.Now.AddSeconds(11);

        await viewModel.ToggleLessonStateCommand.ExecuteAsync(viewModel.Lessons[0]);

        Assert.Equal("Planned", viewModel.Lessons[0].StateText);
        Assert.True(viewModel.Lessons[0].CanEdit);
    }

    [Fact]
    public async Task CompletedLessonReturnToPlannedIsNotLostWhenActivationRefreshRunsDuringStateUpdate()
    {
        var clock = new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));
        var confirmation = new FakeConfirmationService { NextAnswer = true };
        var lessonStates = new FakeLessonStateService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Completed, LessonIsInvoiced = false, LessonPaymentStatus = null },
            confirmation: confirmation,
            lessonStates: lessonStates,
            clock: clock);
        lessonStates.BeforeReturnAsync = () => viewModel.RefreshAfterActivationAsync();
        await viewModel.LoadAsync();
        clock.Now = clock.Now.AddSeconds(11);

        await viewModel.ToggleLessonStateCommand.ExecuteAsync(viewModel.Lessons[0]);

        Assert.True(confirmation.MarkLessonPlannedWasAsked);
        Assert.Equal("Planned", viewModel.Lessons[0].StateText);
        Assert.True(viewModel.Lessons[0].CanEdit);
    }

    [Fact]
    public async Task CompletedLessonEditIsBlocked()
    {
        var lessonEditor = new FakeLessonEditorService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Completed, LessonIsInvoiced = false, LessonPaymentStatus = null },
            lessonEditorService: lessonEditor);
        await viewModel.LoadAsync();

        await viewModel.SaveLessonCommand.ExecuteAsync(viewModel.Lessons[0]);

        Assert.False(lessonEditor.EditWasCalled);
        Assert.True(viewModel.HasError);
        Assert.Equal("Completed lessons cannot be edited.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task SuccessfulLessonDeleteRemovesVisibleLesson()
    {
        var lessonEditor = new FakeLessonEditorService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
            lessonEditorService: lessonEditor);
        await viewModel.LoadAsync();

        await viewModel.DeleteLessonCommand.ExecuteAsync(viewModel.Lessons[0]);

        Assert.True(lessonEditor.DeleteWasCalled);
        Assert.Empty(viewModel.Lessons);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task ConfirmedLessonDeleteIsNotLostWhenActivationRefreshRunsDuringDelete()
    {
        var clock = new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));
        var lessonEditor = new FakeLessonEditorService();
        var viewModel = CreateViewModel(
            dashboard: new FakeDashboardService { LessonState = LessonState.Planned, LessonIsInvoiced = false, LessonPaymentStatus = null },
            confirmation: new FakeConfirmationService { NextAnswer = true },
            lessonEditorService: lessonEditor,
            clock: clock);
        lessonEditor.BeforeDeleteReturnAsync = () => viewModel.RefreshAfterActivationAsync();
        await viewModel.LoadAsync();
        clock.Now = clock.Now.AddSeconds(11);

        await viewModel.DeleteLessonCommand.ExecuteAsync(viewModel.Lessons[0]);

        Assert.Empty(viewModel.Lessons);
        Assert.False(viewModel.HasError);
    }

    private static MainWindowViewModel CreateViewModel(
        FakeDashboardService? dashboard = null,
        FakePaymentService? payments = null,
        FakeConfirmationService? confirmation = null,
        FakeCalendarLauncher? calendar = null,
        FakeStudentOverviewLauncher? studentOverviewLauncher = null,
        FakeLessonEditorLauncher? lessonEditorLauncher = null,
        FakeLessonEditorService? lessonEditorService = null,
        FakeLessonStateService? lessonStates = null,
        FakeInvoiceWorkflowLauncher? invoiceWorkflowLauncher = null,
        FakeInvoicePdfLauncher? invoicePdfLauncher = null,
        FakeSettingsLauncher? settingsLauncher = null,
        FakePlanCatalogLauncher? planCatalogLauncher = null,
        FakeUiClock? clock = null,
        TimeZoneInfo? lessonTimeZone = null) =>
        new(
            dashboard ?? new FakeDashboardService(),
            payments ?? new FakePaymentService(),
            confirmation ?? new FakeConfirmationService(),
            calendar ?? new FakeCalendarLauncher(),
            studentOverviewLauncher ?? new FakeStudentOverviewLauncher(),
            lessonEditorLauncher ?? new FakeLessonEditorLauncher(),
            lessonEditorService ?? new FakeLessonEditorService(),
            lessonStates ?? new FakeLessonStateService(),
            invoiceWorkflowLauncher ?? new FakeInvoiceWorkflowLauncher(),
            invoicePdfLauncher ?? new FakeInvoicePdfLauncher(),
            settingsLauncher ?? new FakeSettingsLauncher(),
            planCatalogLauncher ?? new FakePlanCatalogLauncher(),
            clock ?? new FakeUiClock(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero)),
            lessonTimeZone);

    private static void RunOnStaThread(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
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

    private static IReadOnlyList<T> FindChildren<T>(DependencyObject root)
        where T : DependencyObject
    {
        var matches = new List<T>();
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is T match)
            {
                matches.Add(match);
            }

            if (child is DependencyObject dependencyObject)
            {
                matches.AddRange(FindChildren<T>(dependencyObject));
            }
        }

        return matches;
    }

    private static IReadOnlyList<T> FindVisualChildren<T>(DependencyObject root)
        where T : DependencyObject
    {
        var matches = new List<T>();
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                matches.Add(match);
            }

            matches.AddRange(FindVisualChildren<T>(child));
        }

        return matches;
    }

    private static void RaisePreviewLeftClick(UIElement element)
    {
        element.RaiseEvent(new MouseButtonEventArgs(
            Mouse.PrimaryDevice,
            Environment.TickCount,
            MouseButton.Left)
        {
            RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
            Source = element
        });
    }

    private static void RaiseHandledPreviewLeftClick(UIElement element)
    {
        element.RaiseEvent(new MouseButtonEventArgs(
            Mouse.PrimaryDevice,
            Environment.TickCount,
            MouseButton.Left)
        {
            RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
            Source = element,
            Handled = true
        });
    }

    private static WpfButton FindButton(DependencyObject root, string nameOrContent)
    {
        var button = FindVisualChildren<WpfButton>(root)
            .FirstOrDefault(button =>
                string.Equals(AutomationProperties.GetName(button), nameOrContent, StringComparison.Ordinal) ||
                string.Equals(button.Content?.ToString(), nameOrContent, StringComparison.Ordinal));
        Assert.NotNull(button);
        return button;
    }

    private static void InvokeButton(WpfButton button)
    {
        var peer = new ButtonAutomationPeer(button);
        ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();
    }

    private static void InvokeActivated(AppMainWindow window)
    {
        var method = typeof(AppMainWindow).GetMethod(
            "OnActivated",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            [typeof(object), typeof(EventArgs)],
            modifiers: null);
        Assert.NotNull(method);
        method.Invoke(window, [window, EventArgs.Empty]);
    }

    private static void PumpDispatcher(DispatcherPriority priority)
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            priority,
            new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (!condition() && DateTimeOffset.UtcNow < deadline)
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        Assert.True(condition());
    }

    private sealed class FakeDashboardService : IStartupDashboardService
    {
        public int LoadCount { get; private set; }

        public DateOnly? LastMonth { get; private set; }

        public string? LastSelectedStudentShortName { get; private set; }

        public IReadOnlyList<string> StudentShortNames { get; init; } = ["Anna"];

        public IReadOnlyList<DashboardStudentNavigationEntry>? StudentNavigationEntries { get; init; }

        public IReadOnlyList<DashboardLesson>? DashboardLessons { get; init; }

        public IReadOnlyList<DashboardPlanOption>? PlanOptions { get; init; }

        public OperationResult<StartupDashboardResult>? NextResult { get; set; }

        public LessonState LessonState { get; init; } = LessonState.Completed;

        public bool LessonIsInvoiced { get; init; } = true;

        public InvoiceNumber? LessonInvoiceNumber { get; init; } = new(1);

        public PaymentStatus? LessonPaymentStatus { get; init; } = PaymentStatus.Outstanding;

        public PaymentStatus InvoicePaymentStatus { get; init; } = PaymentStatus.Outstanding;

        public IReadOnlyList<IssuedInvoiceSummary>? InvoiceSummaries { get; init; }

        public bool CurrentStudentHasSchedule { get; init; } = true;

        public Func<string?, Task>? BeforeReturnAsync { get; init; }

        public Task<OperationResult<StartupDashboardResult>> LoadAsync(
            DateOnly month,
            string? selectedStudentShortName,
            CancellationToken cancellationToken) =>
            LoadCoreAsync(month, selectedStudentShortName);

        private async Task<OperationResult<StartupDashboardResult>> LoadCoreAsync(
            DateOnly month,
            string? selectedStudentShortName)
        {
            LoadCount++;
            LastMonth = month;
            LastSelectedStudentShortName = selectedStudentShortName;
            var currentStudent = selectedStudentShortName ?? "Anna";
            if (BeforeReturnAsync is not null)
            {
                await BeforeReturnAsync(selectedStudentShortName);
            }

            var result = NextResult ?? OperationResult<StartupDashboardResult>.Success(
                new StartupDashboardResult(
                    currentStudent,
                    StudentShortNames,
                    DashboardLessons ??
                    [
                            new DashboardLesson(
                                "lesson-1",
                                currentStudent,
                                new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.FromHours(2)),
                                LessonState,
                                LessonIsInvoiced,
                                LessonPaymentStatus,
                                LessonIsInvoiced ? LessonInvoiceNumber : null)
                    ],
                    InvoiceSummaries ??
                    [
                        new IssuedInvoiceSummary(
                            new InvoiceNumber(1),
                            currentStudent,
                            new DateOnly(2026, 6, 16),
                            InvoicePaymentStatus,
                            "C:\\Invoices\\000001.pdf",
                            Period: new InvoicePeriod(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)))
                    ],
                    CurrentStudentHasSchedule,
                    StudentNavigationEntries ??
                    StudentShortNames
                        .Select((shortName, index) => new DashboardStudentNavigationEntry(
                            shortName,
                            new DateTimeOffset(2026, 6, 16, 15, index, 0, TimeSpan.FromHours(2))))
                        .ToArray(),
                    PlanOptions ?? [new DashboardPlanOption(42, "Standard")]));

            NextResult = null;
            return result;
        }
    }

    private static async Task EventuallyAsync(Func<bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.True(condition());
    }

    private sealed class FakePaymentService : IInvoicePaymentStatusService
    {
        public InvoiceNumber LastInvoiceNumber { get; private set; }

        public bool LastConfirmMarkOutstanding { get; private set; }

        public Func<Task>? BeforeReturnAsync { get; set; }

        public async Task<OperationResult<PaymentStatus>> ToggleAsync(
            InvoiceNumber invoiceNumber,
            bool confirmMarkOutstanding,
            CancellationToken cancellationToken)
        {
            LastInvoiceNumber = invoiceNumber;
            LastConfirmMarkOutstanding = confirmMarkOutstanding;
            if (BeforeReturnAsync is not null)
            {
                await BeforeReturnAsync();
            }

            var next = confirmMarkOutstanding ? PaymentStatus.Outstanding : PaymentStatus.Paid;
            return OperationResult<PaymentStatus>.Success(next);
        }
    }

    private sealed class FakeConfirmationService : IConfirmationService
    {
        public bool NextAnswer { get; init; } = true;

        public bool WasAsked { get; private set; }

        public bool MarkLessonPlannedWasAsked { get; private set; }

        public Func<Task>? OnMarkLessonPlannedAsync { get; set; }

        public Task<bool> ConfirmMarkInvoiceOutstandingAsync(InvoiceDisplayModel invoice, CancellationToken cancellationToken)
        {
            WasAsked = true;
            return Task.FromResult(NextAnswer);
        }

        public Task<bool> ConfirmDeleteLessonAsync(LessonDisplayModel lesson, CancellationToken cancellationToken) =>
            Task.FromResult(NextAnswer);

        public Task<bool> ConfirmMarkLessonPlannedAsync(LessonDisplayModel lesson, CancellationToken cancellationToken)
        {
            MarkLessonPlannedWasAsked = true;
            return ConfirmMarkLessonPlannedCoreAsync();
        }

        private async Task<bool> ConfirmMarkLessonPlannedCoreAsync()
        {
            if (OnMarkLessonPlannedAsync is not null)
            {
                await OnMarkLessonPlannedAsync();
            }

            return NextAnswer;
        }
    }

    private sealed class FakeCalendarLauncher : ICalendarLauncher
    {
        public bool WasOpened { get; private set; }

        public Task OpenAsync(CancellationToken cancellationToken)
        {
            WasOpened = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStudentOverviewLauncher : IStudentOverviewLauncher
    {
        public bool WasShown { get; private set; }

        public string? InitialSelectedStudentShortName { get; private set; }

        public string? SelectedStudentShortName { get; init; }

        public string? SelectionChangedStudentShortName { get; init; }

        public IReadOnlyList<string> SelectionChangedStudentShortNames { get; init; } = [];

        public bool FireSelectionChangesConcurrently { get; init; }

        public Action? OnAfterSelectionChanged { get; init; }

        public async Task<string?> ShowStudentOverviewAsync(
            string? selectedStudentShortName,
            Func<string?, CancellationToken, Task> selectedStudentChangedAsync,
            CancellationToken cancellationToken)
        {
            WasShown = true;
            InitialSelectedStudentShortName = selectedStudentShortName;
            if (SelectionChangedStudentShortNames.Count > 0)
            {
                var tasks = SelectionChangedStudentShortNames
                    .Select(shortName => selectedStudentChangedAsync(shortName, cancellationToken))
                    .ToArray();

                if (FireSelectionChangesConcurrently)
                {
                    await Task.WhenAll(tasks);
                }
                else
                {
                    foreach (var task in tasks)
                    {
                        await task;
                    }
                }

                OnAfterSelectionChanged?.Invoke();
            }

            if (SelectionChangedStudentShortName is not null)
            {
                await selectedStudentChangedAsync(SelectionChangedStudentShortName, cancellationToken);
                OnAfterSelectionChanged?.Invoke();
            }

            return SelectedStudentShortName;
        }
    }

    private sealed class FakeLessonEditorLauncher : ILessonEditorLauncher
    {
        public bool AddWasShown { get; private set; }

        public bool EditWasShown { get; private set; }

        public Task<bool> ShowAddLessonAsync(
            string studentShortName,
            DateTimeOffset startsAt,
            DateOnly refreshMonth,
            CancellationToken cancellationToken)
        {
            AddWasShown = true;
            return Task.FromResult(false);
        }

        public Task<bool> ShowEditLessonAsync(
            LessonDisplayModel lesson,
            DateOnly refreshMonth,
            CancellationToken cancellationToken)
        {
            EditWasShown = true;
            return Task.FromResult(false);
        }
    }

    private sealed class FakeLessonEditorService : ILessonEditorService
    {
        public bool AddWasCalled { get; private set; }

        public bool EditWasCalled { get; private set; }

        public bool DeleteWasCalled { get; private set; }

        public MySmallBusinessLedger.Core.UseCases.Lessons.LessonAddRequest? LastAddRequest { get; private set; }

        public MySmallBusinessLedger.Core.UseCases.Lessons.LessonEditRequest? LastEditRequest { get; private set; }

        public string? LastPopulateStudentShortName { get; private set; }

        public DateOnly? LastPopulateDisplayMonth { get; private set; }

        public DateOnly? LastPopulateToday { get; private set; }

        public int? LastPlanLessonId { get; private set; }

        public int? LastPlanCatalogId { get; private set; }

        public Func<Task>? BeforeDeleteReturnAsync { get; set; }

        public OperationResult<PopulateCalendarResult> PopulateResult { get; init; } =
            OperationResult<PopulateCalendarResult>.Success(new PopulateCalendarResult(2, 0, []));

        public Task<OperationResult<IReadOnlyList<MySmallBusinessLedger.Core.Calendar.CalendarLesson>>> AddAsync(
            MySmallBusinessLedger.Core.UseCases.Lessons.LessonAddRequest request,
            DateOnly refreshMonth,
            CancellationToken cancellationToken)
        {
            AddWasCalled = true;
            LastAddRequest = request;
            return Task.FromResult(OperationResult<IReadOnlyList<MySmallBusinessLedger.Core.Calendar.CalendarLesson>>.Success(
                [new MySmallBusinessLedger.Core.Calendar.CalendarLesson("new-lesson", request.StudentShortName, request.StartsAt)]));
        }

        public Task<OperationResult<PopulateCalendarResult>> PopulateCalendarAsync(
            string studentShortName,
            DateOnly displayMonth,
            DateOnly today,
            CancellationToken cancellationToken)
        {
            LastPopulateStudentShortName = studentShortName;
            LastPopulateDisplayMonth = displayMonth;
            LastPopulateToday = today;
            return Task.FromResult(PopulateResult);
        }

        public Task<OperationResult<IReadOnlyList<MySmallBusinessLedger.Core.Calendar.CalendarLesson>>> EditAsync(
            MySmallBusinessLedger.Core.UseCases.Lessons.LessonEditRequest request,
            DateOnly refreshMonth,
            CancellationToken cancellationToken)
        {
            EditWasCalled = true;
            LastEditRequest = request;
            return Task.FromResult(OperationResult<IReadOnlyList<MySmallBusinessLedger.Core.Calendar.CalendarLesson>>.Success([]));
        }

        public async Task<OperationResult<IReadOnlyList<MySmallBusinessLedger.Core.Calendar.CalendarLesson>>> DeleteAsync(
            string lessonId,
            DateOnly refreshMonth,
            CancellationToken cancellationToken)
        {
            DeleteWasCalled = true;
            if (BeforeDeleteReturnAsync is not null)
            {
                await BeforeDeleteReturnAsync();
            }

            return OperationResult<IReadOnlyList<MySmallBusinessLedger.Core.Calendar.CalendarLesson>>.Success([]);
        }

        public Task<OperationResult<LessonRecord>> UpdatePlanAsync(
            int lessonId,
            int planCatalogId,
            CancellationToken cancellationToken)
        {
            LastPlanLessonId = lessonId;
            LastPlanCatalogId = planCatalogId;
            return Task.FromResult(OperationResult<LessonRecord>.Success(new LessonRecord(
                lessonId,
                StudentId: 1,
                StudentShortName: "Anna",
                PlanCatalogId: planCatalogId,
                PlanName: planCatalogId == 77 ? "Proevetime" : "Standard",
                StartsAt: new DateTimeOffset(2026, 6, 16, 15, 0, 0, TimeSpan.FromHours(2)),
                DurationMinutes: 60,
                State: LessonState.Planned,
                GoogleCalendarEventId: "lesson-1",
                InvoiceNumber: null)));
        }
    }

    private sealed class FakeLessonStateService : ILessonStateService
    {
        public string? LastLessonId { get; private set; }

        public LessonState? LastCurrentState { get; private set; }

        public Func<Task>? BeforeReturnAsync { get; set; }

        public async Task<OperationResult<LessonState>> ToggleAsync(
            string lessonId,
            LessonState currentState,
            bool confirmReturnToPlanned,
            CancellationToken cancellationToken)
        {
            LastLessonId = lessonId;
            LastCurrentState = currentState;
            if (BeforeReturnAsync is not null)
            {
                await BeforeReturnAsync();
            }

            var nextState = currentState == LessonState.Planned ? LessonState.Completed : LessonState.Planned;
            return OperationResult<LessonState>.Success(nextState);
        }
    }

    private sealed class FakeInvoiceWorkflowLauncher : IInvoiceWorkflowLauncher
    {
        public bool NextWriteResult { get; init; }

        public bool WriteWasShown { get; private set; }

        public string? LastWriteInitialStudentShortName { get; private set; }

        public InvoiceNumber? LastViewedInvoiceNumber { get; private set; }

        public Task<bool> ShowWriteInvoiceAsync(
            DateOnly displayMonth,
            string? initialStudentShortName,
            CancellationToken cancellationToken)
        {
            WriteWasShown = true;
            LastWriteInitialStudentShortName = initialStudentShortName;
            return Task.FromResult(NextWriteResult);
        }

        public Task ShowInvoiceViewerAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
        {
            LastViewedInvoiceNumber = invoiceNumber;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInvoicePdfLauncher : IInvoicePdfLauncher
    {
        public string? LastOpenedPdfFilePath { get; private set; }

        public Task OpenAsync(string pdfFilePath, CancellationToken cancellationToken)
        {
            LastOpenedPdfFilePath = pdfFilePath;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSettingsLauncher : ISettingsLauncher
    {
        public bool NextResult { get; init; }

        public bool WasShown { get; private set; }

        public Task<bool> ShowSettingsAsync(CancellationToken cancellationToken)
        {
            WasShown = true;
            return Task.FromResult(NextResult);
        }
    }

    private sealed class FakePlanCatalogLauncher : IPlanCatalogLauncher
    {
        public bool WasShown { get; private set; }

        public Task ShowPlanCatalogAsync(CancellationToken cancellationToken)
        {
            WasShown = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUiClock : IUiClock
    {
        public FakeUiClock(DateTimeOffset now)
        {
            Now = now;
        }

        public DateTimeOffset Now { get; set; }
    }
}
