using ElenasBusinessLedger.App.Commands;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.App.ViewModels;
using ElenasBusinessLedger.App.Views;
using ElenasBusinessLedger.Core.Common;
using ElenasBusinessLedger.Core.Invoices;
using ElenasBusinessLedger.Core.Plans;
using ElenasBusinessLedger.Core.Ports;
using ElenasBusinessLedger.Core.Students;
using ElenasBusinessLedger.Core.UseCases.Plans;
using ElenasBusinessLedger.Core.UseCases.Students;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Data;
using System.Windows.Controls.Primitives;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfDataGridLengthUnitType = System.Windows.Controls.DataGridLengthUnitType;
using WpfDatePicker = System.Windows.Controls.DatePicker;
using WpfGrid = System.Windows.Controls.Grid;
using WpfScrollViewer = System.Windows.Controls.ScrollViewer;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace ElenasBusinessLedger.Tests.App.ViewModels;

// Checks shared student selection, editor saves, cancelled edits, and the WPF Details layout.
public sealed class StudentOverviewViewModelTests
{
    [Fact]
    public void ViewInitializesWithOverviewSurface()
    {
        WpfTestThread.Run(() =>
        {
            var viewModel = new StudentOverviewViewModel(
                new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
                new FakeStudentEditorService(),
                new FakePlanCatalogService(),
                new FakeInvoiceWorkflowLauncher(),
                new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
            var window = new StudentOverviewView(viewModel);

            Assert.Equal("Manage Students", window.Title);
            Assert.Same(viewModel, window.DataContext);
            Assert.NotNull(window.Content);

            window.Close();
        });
    }

    [Fact]
    public void CloseButtonIsCompactAndRightAligned()
    {
        WpfTestThread.Run(() =>
        {
            var viewModel = new StudentOverviewViewModel(
                new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
                new FakeStudentEditorService(),
                new FakePlanCatalogService(),
                new FakeInvoiceWorkflowLauncher(),
                new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
            var window = new StudentOverviewView(viewModel);

            var closeButton = Assert.Single(FindChildren<WpfButton>(window), button => button.Content?.ToString() == "Close");
            var closeButtonHost = Assert.IsType<WpfGrid>(closeButton.Parent);

            Assert.Equal(HorizontalAlignment.Right, closeButton.HorizontalAlignment);
            Assert.Equal(84, closeButton.MinWidth);
            Assert.Equal("StudentListBottomActions", closeButtonHost.Name);

            window.Close();
        });
    }

    [Fact]
    public void StudentOverviewHeadingsNamePaneContext()
    {
        WpfTestThread.Run(() =>
        {
            var viewModel = new StudentOverviewViewModel(
                new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
                new FakeStudentEditorService(),
                new FakePlanCatalogService(),
                new FakeInvoiceWorkflowLauncher(),
                new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
            var window = new StudentOverviewView(viewModel);
            var detailsWindow = new StudentDetailsView(viewModel);

            var headings = FindChildren<WpfTextBlock>(window)
                .Select(textBlock => textBlock.Text)
                .ToArray();
            var detailHeadings = FindChildren<WpfTextBlock>(detailsWindow)
                .Select(textBlock => textBlock.Text)
                .ToArray();

            Assert.Contains("Students", headings);
            Assert.DoesNotContain("Student Plan", headings);
            Assert.DoesNotContain("Student Invoices", headings);
            Assert.Contains("Student", detailHeadings);
            Assert.Contains("Student Plan", detailHeadings);
            Assert.Contains("Student Invoices", detailHeadings);
            Assert.DoesNotContain("Current Plan", headings);
            Assert.DoesNotContain("Invoices", headings);

            detailsWindow.Close();
            window.Close();
        });
    }

    [Fact]
    public void StudentOverviewKeepsAddStudentActionNearListHeading()
    {
        WpfTestThread.Run(() =>
        {
            var viewModel = new StudentOverviewViewModel(
                new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
                new FakeStudentEditorService(),
                new FakePlanCatalogService(),
                new FakeInvoiceWorkflowLauncher(),
                new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
            var window = new StudentOverviewView(viewModel);

            var addStudentButton = Assert.Single(FindChildren<WpfButton>(window), button => AutomationProperties.GetName(button) == "Add student");
            var addStudentTexts = FindChildren<WpfTextBlock>(addStudentButton)
                .Select(textBlock => textBlock.Text)
                .ToArray();
            var addStudentLabel = Assert.Single(FindChildren<WpfTextBlock>(addStudentButton), textBlock => textBlock.Text == "Add student");

            Assert.Equal(HorizontalAlignment.Right, addStudentButton.HorizontalAlignment);
            Assert.Contains("+", addStudentTexts);
            Assert.Contains("Add student", addStudentTexts);
            Assert.Equal(14, addStudentLabel.FontSize);
            Assert.Equal(FontWeights.Bold, addStudentLabel.FontWeight);
            Assert.Equal(new Thickness(0), addStudentButton.BorderThickness);
            Assert.Equal(1, WpfGrid.GetColumn(addStudentButton));
            Assert.Equal(0, WpfGrid.GetRow(addStudentButton));

            window.Close();
        });
    }

    [Fact]
    public void StudentDetailsLayoutUsesFixedColumnsWithoutResizeSplitter()
    {
        WpfTestThread.Run(() =>
        {
            var viewModel = new StudentOverviewViewModel(
                new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
                new FakeStudentEditorService(),
                new FakePlanCatalogService(),
                new FakeInvoiceWorkflowLauncher(),
                new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
            var window = new StudentDetailsView(viewModel);

            var outerColumns = Assert.Single(FindChildren<WpfGrid>(window), grid => grid.Name == "StudentDetailsOuterColumns");
            Assert.Equal(ResizeMode.CanMinimize, window.ResizeMode);
            Assert.Equal(1080, window.Width);
            Assert.Equal(735, window.Height);
            Assert.Equal(1080, window.MinWidth);
            Assert.Equal(735, window.MinHeight);
            Assert.Equal(3, outerColumns.ColumnDefinitions.Count);
            Assert.Equal(GridUnitType.Pixel, outerColumns.ColumnDefinitions[0].Width.GridUnitType);
            Assert.Equal(GridUnitType.Pixel, outerColumns.ColumnDefinitions[1].Width.GridUnitType);
            Assert.Equal(GridUnitType.Pixel, outerColumns.ColumnDefinitions[2].Width.GridUnitType);
            Assert.Equal(550, outerColumns.ColumnDefinitions[0].Width.Value);
            Assert.Equal(18, outerColumns.ColumnDefinitions[1].Width.Value);
            Assert.Equal(432, outerColumns.ColumnDefinitions[2].Width.Value);

            var detailColumns = Assert.Single(FindChildren<WpfGrid>(window), grid => grid.Name == "StudentDetailsColumns");
            Assert.Equal(3, detailColumns.ColumnDefinitions.Count);
            Assert.Equal(GridUnitType.Pixel, detailColumns.ColumnDefinitions[0].Width.GridUnitType);
            Assert.Equal(GridUnitType.Pixel, detailColumns.ColumnDefinitions[2].Width.GridUnitType);
            Assert.Equal(252, detailColumns.ColumnDefinitions[0].Width.Value);
            Assert.Equal(280, detailColumns.ColumnDefinitions[2].Width.Value);

            var invoicesGrid = Assert.Single(FindChildren<WpfDataGrid>(window), grid => grid.Name == "InvoicesGrid");
            Assert.Equal(430, invoicesGrid.Columns.Sum(column => column.Width.Value));
            Assert.Equal(WpfDataGridLengthUnitType.Pixel, invoicesGrid.Columns[4].Width.UnitType);
            Assert.Equal(95, invoicesGrid.Columns[4].Width.Value);

            window.Close();
        });
    }

    [Fact]
    public void StudentDetailsFixedSizeLayoutDoesNotNeedScrollBars()
    {
        WpfTestThread.Run(() =>
        {
            var viewModel = new StudentOverviewViewModel(
                new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
                new FakeStudentEditorService(),
                new FakePlanCatalogService(),
                new FakeInvoiceWorkflowLauncher(),
                new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
            var window = new StudentDetailsView(viewModel);
            try
            {
                window.Show();
                window.UpdateLayout();

                var fieldsScroller = Assert.Single(
                    FindChildren<WpfScrollViewer>(window),
                    scrollViewer => scrollViewer.Content is WpfGrid { Name: "StudentDetailsFieldsGrid" });
                var invoicesGrid = Assert.Single(FindChildren<WpfDataGrid>(window), grid => grid.Name == "InvoicesGrid");
                var invoicesScroller = Assert.Single(FindVisualChildren<WpfScrollViewer>(invoicesGrid));

                Assert.NotEqual(Visibility.Visible, fieldsScroller.ComputedVerticalScrollBarVisibility);
                Assert.NotEqual(Visibility.Visible, invoicesScroller.ComputedHorizontalScrollBarVisibility);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void StudentDetailsRestoresPositionWithoutRestoringOversizedWindowSize()
    {
        WpfTestThread.Run(() =>
        {
            var storeDirectory = Path.Combine(
                Path.GetTempPath(),
                "ElenasBusinessLedgerLayoutTests",
                Guid.NewGuid().ToString("N"));
            var storePath = Path.Combine(storeDirectory, "window-placement.json");
            WindowPlacementStore.StorePathOverride = storePath;
            StudentDetailsView? window = null;
            try
            {
                var savedLeft = SystemParameters.VirtualScreenLeft + 100;
                var savedTop = SystemParameters.VirtualScreenTop + 120;
                Directory.CreateDirectory(storeDirectory);
                File.WriteAllText(
                    storePath,
                    $$"""
                      {
                        "StudentDetailsView": {
                          "Left": {{savedLeft}},
                          "Top": {{savedTop}},
                          "Width": 1400,
                          "Height": 900
                        }
                      }
                      """);
                var viewModel = new StudentOverviewViewModel(
                    new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
                    new FakeStudentEditorService(),
                    new FakePlanCatalogService(),
                    new FakeInvoiceWorkflowLauncher(),
                    new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));

                window = new StudentDetailsView(viewModel);
                window.Show();

                Assert.Equal(1080, window.Width);
                Assert.Equal(735, window.Height);
                Assert.Equal(savedLeft, window.Left);
                Assert.Equal(savedTop, window.Top);

                window.Close();
            }
            finally
            {
                if (window?.IsVisible == true)
                {
                    window.Close();
                }

                WindowPlacementStore.StorePathOverride = null;
                if (Directory.Exists(storeDirectory))
                {
                    Directory.Delete(storeDirectory, recursive: true);
                }
            }
        });
    }

    [Fact]
    public void StudentDetailsFieldInputsUseConsistentHeight()
    {
        WpfTestThread.Run(() =>
        {
            var viewModel = new StudentOverviewViewModel(
                new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
                new FakeStudentEditorService(),
                new FakePlanCatalogService(),
                new FakeInvoiceWorkflowLauncher(),
                new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
            var window = new StudentDetailsView(viewModel);
            window.Show();

            Assert.All(FindChildren<WpfTextBox>(window), textBox => Assert.Equal(24, textBox.Height));
            Assert.All(FindChildren<WpfComboBox>(window), comboBox => Assert.Equal(24, comboBox.Height));
            Assert.All(FindChildren<WpfDatePicker>(window), datePicker => Assert.Equal(24, datePicker.Height));

            window.Close();
        });
    }

    [Fact]
    public void StudentDetailsPlanFieldsShareStudentFieldRowRhythm()
    {
        WpfTestThread.Run(() =>
        {
            var viewModel = new StudentOverviewViewModel(
                new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
                new FakeStudentEditorService(),
                new FakePlanCatalogService(),
                new FakeInvoiceWorkflowLauncher(),
                new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
            var window = new StudentDetailsView(viewModel);

            var outerColumns = Assert.Single(FindChildren<WpfGrid>(window), grid => grid.Name == "StudentDetailsOuterColumns");
            var detailsGrid = Assert.IsType<WpfGrid>(window.FindName("StudentDetailsFieldsGrid"));
            var priceGroup = Assert.IsType<WpfStackPanel>(window.FindName("PlanPriceWithoutVatGroup"));
            var lessonsGroup = Assert.IsType<WpfStackPanel>(window.FindName("PlanLessonsGroup"));
            var durationGroup = Assert.IsType<WpfStackPanel>(window.FindName("PlanDurationGroup"));
            var studentCountGroup = Assert.IsType<WpfStackPanel>(window.FindName("PlanNumberOfStudentsGroup"));
            var flexibilityGroup = Assert.IsType<WpfStackPanel>(window.FindName("PlanFlexibilityGroup"));
            var paymentTermsGroup = Assert.IsType<WpfStackPanel>(window.FindName("PlanPaymentTermsGroup"));
            var startsOnGroup = Assert.IsType<WpfStackPanel>(window.FindName("PlanStartsOnGroup"));
            var scheduleHeader = Assert.IsType<WpfTextBlock>(window.FindName("ScheduleHeader"));

            Assert.Equal(550, outerColumns.ColumnDefinitions[0].Width.Value);
            Assert.Equal(18, outerColumns.ColumnDefinitions[1].Width.Value);
            Assert.Equal(432, outerColumns.ColumnDefinitions[2].Width.Value);
            Assert.Equal(280, detailsGrid.ColumnDefinitions[2].Width.Value);
            Assert.Equal(1, WpfGrid.GetRow(priceGroup));
            Assert.Equal(2, WpfGrid.GetRow(lessonsGroup));
            Assert.Equal(3, WpfGrid.GetRow(durationGroup));
            Assert.Equal(4, WpfGrid.GetRow(studentCountGroup));
            Assert.Equal(5, WpfGrid.GetRow(flexibilityGroup));
            Assert.Equal(6, WpfGrid.GetRow(paymentTermsGroup));
            Assert.Equal(7, WpfGrid.GetRow(startsOnGroup));
            Assert.True(scheduleHeader.FontSize < 18);

            window.Close();
        });
    }

    [Fact]
    public void StudentDetailsActionsAreBottomLeftWithSaveAndCancel()
    {
        WpfTestThread.Run(() =>
        {
            var viewModel = new StudentOverviewViewModel(
                new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
                new FakeStudentEditorService(),
                new FakePlanCatalogService(),
                new FakeInvoiceWorkflowLauncher(),
                new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
            var window = new StudentDetailsView(viewModel);

            var actionHost = Assert.Single(FindChildren<WpfStackPanel>(window), panel => panel.Name == "StudentDetailsBottomActions");
            var actionButtons = FindChildren<WpfButton>(actionHost).ToArray();
            var saveButton = Assert.Single(actionButtons, button => button.Content?.ToString() == "Save");
            var cancelButton = Assert.Single(actionButtons, button => button.Content?.ToString() == "Cancel");
            var actionButtonLabels = actionButtons
                .Select(button => button.Content?.ToString() ?? string.Empty)
                .ToArray();

            Assert.Equal(HorizontalAlignment.Left, actionHost.HorizontalAlignment);
            Assert.Equal(new Thickness(0), actionHost.Margin);
            Assert.Equal(1, WpfGrid.GetRow(actionHost));
            Assert.Equal(["Save", "Cancel"], actionButtonLabels);
            var saveCommandBinding = saveButton.GetBindingExpression(ButtonBase.CommandProperty);
            Assert.NotNull(saveCommandBinding);
            Assert.Equal("SaveStudentCommand", saveCommandBinding!.ParentBinding.Path.Path);
            Assert.Equal(new Thickness(0, 0, 8, 0), saveButton.Margin);
            Assert.Equal(84, cancelButton.MinWidth);
            Assert.Equal("CancelStudentCommand", cancelButton.GetBindingExpression(ButtonBase.CommandProperty)!.ParentBinding.Path.Path);

            window.Close();
        });
    }

    [Theory]
    [InlineData("Save", false, false)]
    [InlineData("Save", true, false)]
    [InlineData("Cancel", false, false)]
    [InlineData("Cancel", false, true)]
    public void StudentDetailsActionsCloseOnlyWhenSavedOrCancelled(string action, bool saveFails, bool newStudent)
    {
        WpfTestThread.Run(() =>
        {
            var interactions = new FakeStudentOverviewInteractionService();
            var editor = new FakeStudentEditorService { SaveFails = saveFails };
            var viewModel = new StudentOverviewViewModel(
                new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
                editor,
                new FakePlanCatalogService(),
                new FakeInvoiceWorkflowLauncher(),
                new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)),
                interactions);
            viewModel.LoadAsync().GetAwaiter().GetResult();
            if (newStudent)
            {
                viewModel.AddStudentCommand.ExecuteAsync().GetAwaiter().GetResult();
            }

            viewModel.SelectedStudentEditor!.FullName = "Unsaved edit";
            var storePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Ledger-StudentDetails-{Guid.NewGuid():N}.json");
            WindowPlacementStore.StorePathOverride = storePath;
            var window = new StudentDetailsView(viewModel);
            try
            {
                window.Show();
                var button = Assert.Single(FindChildren<WpfButton>(window), candidate => Equals(candidate.Content, action));
                Assert.IsType<AsyncRelayCommand>(button.Command).ExecuteAsync().GetAwaiter().GetResult();

                Assert.Equal(saveFails, window.IsVisible);
                Assert.Equal(0, interactions.PromptSaveChangesCount);
                Assert.Equal("Anna", viewModel.SelectedStudent?.ShortName);
                if (action == "Save")
                {
                    Assert.Equal("Unsaved edit", editor.LastRequest?.FullName);
                }
                else
                {
                    Assert.Null(editor.LastRequest);
                    Assert.Equal("Anna Andersen", viewModel.SelectedStudentEditor?.FullName);
                }

                if (saveFails)
                {
                    Assert.Equal("Unsaved edit", viewModel.SelectedStudentEditor?.FullName);
                    Assert.Equal("Student could not be saved.", viewModel.SelectedStudentEditor?.ErrorMessage);
                }
            }
            finally
            {
                window.CloseWithoutPrompt();
                WindowPlacementStore.StorePathOverride = null;
                System.IO.File.Delete(storePath);
            }
        });
    }

    [Fact]
    public void StudentOverviewAndDetailsWindowsShareSelectedStudent()
    {
        WpfTestThread.Run(() =>
        {
            var viewModel = new StudentOverviewViewModel(
                new FakeStudentOverviewService(
                    [
                        CreateSnapshot("Anna", isActive: true),
                        CreateSnapshot("Bo", isActive: true)
                    ]),
                new FakeStudentEditorService(),
                new FakePlanCatalogService(),
                new FakeInvoiceWorkflowLauncher(),
                new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
            viewModel.LoadAsync().GetAwaiter().GetResult();
            var window = new StudentOverviewView(viewModel);
            var detailsWindow = new StudentDetailsView(viewModel);

            var studentsGrid = Assert.Single(FindChildren<WpfDataGrid>(window), grid => grid.Name == "StudentsGrid");
            var selectedItemBinding = studentsGrid.GetBindingExpression(WpfDataGrid.SelectedItemProperty);
            Assert.NotNull(selectedItemBinding);
            Assert.Equal("SelectedStudent", selectedItemBinding!.ParentBinding.Path.Path);
            Assert.Equal(BindingMode.TwoWay, selectedItemBinding.ParentBinding.Mode);

            viewModel.SelectStudentAsync(viewModel.Students.Single(student => student.ShortName == "Bo")).GetAwaiter().GetResult();

            Assert.Same(viewModel, detailsWindow.DataContext);
            Assert.Null(detailsWindow.Owner);
            Assert.Equal("Bo", viewModel.SelectedStudent?.ShortName);
            Assert.Equal("Bo Andersen", viewModel.SelectedStudentEditor?.FullName);

            detailsWindow.Close();
            window.Close();
        });
    }

    [Fact]
    public async Task LoadListsStudentsAndShowsSelectedStudentDetailsPlanAndInvoices()
    {
        var service = new FakeStudentOverviewService(
            [
                CreateSnapshot("Anna", isActive: true),
                CreateSnapshot("Bo", isActive: false)
            ]);
        var viewModel = new StudentOverviewViewModel(
            service,
            new FakeStudentEditorService(),
            new FakePlanCatalogService(),
            new FakeInvoiceWorkflowLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));

        await viewModel.LoadAsync();

        Assert.Equal(["Anna", "Bo"], viewModel.Students.Select(student => student.ShortName).ToArray());
        Assert.Equal("Anna", viewModel.SelectedStudent?.ShortName);
        Assert.Equal("Anna Andersen", viewModel.SelectedStudentEditor?.FullName);
        Assert.True(viewModel.SelectedStudentEditor?.IsActive);
        Assert.Equal("Standard", viewModel.SelectedStudentEditor?.PlanName);
        Assert.Equal("400 kr.", viewModel.SelectedStudentEditor?.PlanPriceWithoutVatText);
        Assert.Equal("Paid monthly in advance", viewModel.SelectedStudentEditor?.PlanPaymentTerms);
        var invoice = Assert.Single(viewModel.Invoices);
        Assert.Equal("001", invoice.InvoiceNumberText);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task OpenInvoiceCommandUsesExistingInvoiceViewerLauncher()
    {
        var invoices = new FakeInvoiceWorkflowLauncher();
        var viewModel = new StudentOverviewViewModel(
            new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
            new FakeStudentEditorService(),
            new FakePlanCatalogService(),
            invoices,
            new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
        await viewModel.LoadAsync();

        await viewModel.OpenInvoiceCommand.ExecuteAsync(viewModel.Invoices[0]);

        Assert.Equal(new InvoiceNumber(1), invoices.LastViewedInvoiceNumber);
    }

    [Fact]
    public async Task SaveStudentChangesThroughEmbeddedEditorAndRefreshesSelectedStudent()
    {
        var service = new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]);
        var editor = new FakeStudentEditorService();
        var viewModel = new StudentOverviewViewModel(
            service,
            editor,
            new FakePlanCatalogService(),
            new FakeInvoiceWorkflowLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
        await viewModel.LoadAsync();

        viewModel.SelectedStudentEditor!.FullName = "Anna A.";
        viewModel.SelectedStudentEditor.IsActive = false;

        await viewModel.SaveStudentCommand.ExecuteAsync();

        Assert.NotNull(editor.LastRequest);
        Assert.Equal("Anna", editor.LastRequest.ShortName);
        Assert.Equal("Anna A.", editor.LastRequest.FullName);
        Assert.False(editor.LastRequest.IsActive);
        Assert.Equal("Anna", viewModel.SelectedStudent?.ShortName);
        Assert.Equal(2, service.LoadCount);
    }

    [Fact]
    public async Task SelectingAnotherStudentPromptsAndSavesChangedStudent()
    {
        var service = new FakeStudentOverviewService(
            [
                CreateSnapshot("Anna", isActive: true),
                CreateSnapshot("Bo", isActive: true)
            ]);
        var editor = new FakeStudentEditorService();
        var interactions = new FakeStudentOverviewInteractionService
        {
            NextSaveChangesChoice = StudentEditSaveChangesChoice.Save
        };
        var viewModel = new StudentOverviewViewModel(
            service,
            editor,
            new FakePlanCatalogService(),
            new FakeInvoiceWorkflowLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)),
            interactions);
        await viewModel.LoadAsync();
        viewModel.SelectedStudentEditor!.FullName = "Anna Changed";

        await viewModel.SelectStudentAsync(viewModel.Students.Single(student => student.ShortName == "Bo"));

        Assert.Equal(1, interactions.PromptSaveChangesCount);
        Assert.NotNull(editor.LastRequest);
        Assert.Equal("Anna Changed", editor.LastRequest.FullName);
        Assert.Equal("Bo", viewModel.SelectedStudent?.ShortName);
    }

    [Fact]
    public async Task SelectingAnotherStudentCanCancelSaveChangesPrompt()
    {
        var interactions = new FakeStudentOverviewInteractionService
        {
            NextSaveChangesChoice = StudentEditSaveChangesChoice.Cancel
        };
        var viewModel = new StudentOverviewViewModel(
            new FakeStudentOverviewService(
                [
                    CreateSnapshot("Anna", isActive: true),
                    CreateSnapshot("Bo", isActive: true)
                ]),
            new FakeStudentEditorService(),
            new FakePlanCatalogService(),
            new FakeInvoiceWorkflowLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)),
            interactions);
        await viewModel.LoadAsync();
        viewModel.SelectedStudentEditor!.FullName = "Unsaved";

        await viewModel.SelectStudentAsync(viewModel.Students.Single(student => student.ShortName == "Bo"));

        Assert.Equal(1, interactions.PromptSaveChangesCount);
        Assert.Equal("Anna", viewModel.SelectedStudent?.ShortName);
        Assert.Equal("Unsaved", viewModel.SelectedStudentEditor?.FullName);
    }

    [Fact]
    public async Task ClosePromptCanCancelWindowCloseWhenStudentHasUnsavedChanges()
    {
        var interactions = new FakeStudentOverviewInteractionService
        {
            NextSaveChangesChoice = StudentEditSaveChangesChoice.Cancel
        };
        var viewModel = new StudentOverviewViewModel(
            new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
            new FakeStudentEditorService(),
            new FakePlanCatalogService(),
            new FakeInvoiceWorkflowLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)),
            interactions);
        await viewModel.LoadAsync();
        viewModel.SelectedStudentEditor!.FullName = "Unsaved";

        var canClose = await viewModel.PrepareForCloseAsync();

        Assert.False(canClose);
        Assert.Equal(1, interactions.PromptSaveChangesCount);
        Assert.Equal("Unsaved", viewModel.SelectedStudentEditor?.FullName);
    }

    [Fact]
    public async Task DetailWindowCloseDiscardResetsUnsavedEditorWhileListRemainsOpen()
    {
        var interactions = new FakeStudentOverviewInteractionService
        {
            NextSaveChangesChoice = StudentEditSaveChangesChoice.Discard
        };
        var viewModel = new StudentOverviewViewModel(
            new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
            new FakeStudentEditorService(),
            new FakePlanCatalogService(),
            new FakeInvoiceWorkflowLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)),
            interactions);
        await viewModel.LoadAsync();
        viewModel.SelectedStudentEditor!.FullName = "Unsaved";

        var canCloseDetails = await viewModel.PrepareForDetailsWindowCloseAsync();

        Assert.True(canCloseDetails);
        Assert.Equal(1, interactions.PromptSaveChangesCount);
        Assert.Equal("Anna Andersen", viewModel.SelectedStudentEditor?.FullName);
    }

    [Fact]
    public async Task AddStudentCreatesEmbeddedDraftAndSaveSelectsSavedStudent()
    {
        var service = new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]);
        var editor = new FakeStudentEditorService { SavedShortName = "Bo" };
        var viewModel = new StudentOverviewViewModel(
            service,
            editor,
            new FakePlanCatalogService(),
            new FakeInvoiceWorkflowLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
        await viewModel.LoadAsync();

        await viewModel.AddStudentCommand.ExecuteAsync();
        viewModel.SelectedStudentEditor!.ShortName = "Bo";
        viewModel.SelectedStudentEditor.FullName = "Bo Berg";
        viewModel.SelectedStudentEditor.Address = "Testvej 2";
        viewModel.SelectedStudentEditor.Phone = "87654321";
        viewModel.SelectedStudentEditor.Email = "bo@example.test";
        service.AddSnapshot(CreateSnapshot("Bo", isActive: true));

        await viewModel.SaveStudentCommand.ExecuteAsync();

        Assert.NotNull(editor.LastRequest);
        Assert.Equal("Bo", editor.LastRequest.ShortName);
        Assert.Equal("Bo", viewModel.SelectedStudent?.ShortName);
    }

    [Fact]
    public async Task SelectingAssignablePlanInEmbeddedEditorCopiesPlanSnapshotIntoSaveRequest()
    {
        var editor = new FakeStudentEditorService();
        var planCatalog = new FakePlanCatalogService(
            [
                CreateCatalogPlan(
                    "Duo",
                    priceWithoutVat: 650m,
                    lessonsPerWeek: LessonsPerWeek.Two,
                    durationMinutes: 45,
                    flexibility: PlanFlexibility.None,
                    numberOfStudents: 2)
            ]);
        var viewModel = new StudentOverviewViewModel(
            new FakeStudentOverviewService([CreateSnapshot("Anna", isActive: true)]),
            editor,
            planCatalog,
            new FakeInvoiceWorkflowLauncher(),
            new FakeUiClock(new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)));
        await viewModel.LoadAsync();

        viewModel.SelectedStudentEditor!.SelectedAssignablePlan =
            viewModel.SelectedStudentEditor.AssignablePlans.Single(plan => plan.Name == "Duo");
        await viewModel.SaveStudentCommand.ExecuteAsync();

        Assert.NotNull(editor.LastRequest);
        var savedPlan = Assert.Single(editor.LastRequest.PlanPeriods);
        Assert.Equal("Duo", savedPlan.PlanName);
        Assert.Equal(650m, savedPlan.PriceWithoutVat);
        Assert.Equal(LessonsPerWeek.Two, savedPlan.LessonsPerWeek);
        Assert.Equal(45, savedPlan.DurationMinutes);
        Assert.Equal(PlanFlexibility.None, savedPlan.Flexibility);
        Assert.Equal(2, savedPlan.NumberOfStudents);
    }

    private static StudentOverviewSnapshot CreateSnapshot(string shortName, bool isActive)
    {
        var student = Student.Create(
            shortName,
            $"{shortName} Andersen",
            "Testvej 1" + Environment.NewLine + "1234 Testby",
            "12345678",
            $"{shortName.ToLowerInvariant()}@example.test",
            AgeBracket.Standard,
            birthdate: null,
            isActive).Value!;

        var record = new StudentRecord(
            0,
            student,
            [
                new StudentPlanPeriod(
                    new Plan(
                        "Old",
                        priceWithoutVat: 300m,
                        LessonsPerWeek.One,
                        durationMinutes: 30,
                        PlanFlexibility.MoveOrCancelByAgreement,
                        numberOfStudents: 1,
                        effectiveFrom: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                        paymentTerms: "Paid monthly in advance"),
                    new DateOnly(2026, 1, 1),
                    new DateOnly(2026, 5, 31)),
                new StudentPlanPeriod(
                    new Plan(
                        "Standard",
                        priceWithoutVat: 400m,
                        LessonsPerWeek.One,
                        durationMinutes: 60,
                        PlanFlexibility.MoveOrCancelByAgreement,
                        numberOfStudents: 1,
                        effectiveFrom: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                        paymentTerms: "Paid monthly in advance"),
                    new DateOnly(2026, 6, 1),
                    EndsOn: null)
            ],
            []);

        return new StudentOverviewSnapshot(
            record,
            [
                new IssuedInvoiceSummary(
                    new InvoiceNumber(1),
                    shortName,
                    new DateOnly(2026, 6, 16),
                    PaymentStatus.Outstanding,
                    "C:\\Invoices\\000001.pdf")
            ]);
    }

    private static PlanCatalogItem CreateCatalogPlan(
        string name,
        decimal priceWithoutVat = 400m,
        LessonsPerWeek lessonsPerWeek = LessonsPerWeek.One,
        int durationMinutes = 60,
        PlanFlexibility flexibility = PlanFlexibility.MoveOrCancelByAgreement,
        int numberOfStudents = 1) =>
        new(
            Id: 1,
            name,
            priceWithoutVat,
            lessonsPerWeek,
            durationMinutes,
            flexibility,
            numberOfStudents,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "Paid monthly in advance",
            RetireOn: null,
            PlanCatalogState.Active);

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
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                matches.Add(match);
            }

            matches.AddRange(FindVisualChildren<T>(child));
        }

        return matches;
    }

    // Supplies student snapshots and records reload and availability requests for the shared editor.
    private sealed class FakeStudentOverviewService : IStudentOverviewService
    {
        private readonly List<StudentOverviewSnapshot> snapshots;

        public FakeStudentOverviewService(IReadOnlyList<StudentOverviewSnapshot> snapshots)
        {
            this.snapshots = snapshots.ToList();
        }

        public int LoadCount { get; private set; }

        public string? LastSetActiveStudentShortName { get; private set; }

        public bool? LastSetActiveValue { get; private set; }

        public Task<IReadOnlyList<StudentOverviewSnapshot>> LoadAsync(CancellationToken cancellationToken)
        {
            LoadCount++;
            return Task.FromResult<IReadOnlyList<StudentOverviewSnapshot>>(snapshots.ToArray());
        }

        public void AddSnapshot(StudentOverviewSnapshot snapshot) =>
            snapshots.Add(snapshot);

        public Task<OperationResult<StudentOverviewSnapshot>> SetStudentActiveAsync(
            string studentShortName,
            bool isActive,
            CancellationToken cancellationToken)
        {
            LastSetActiveStudentShortName = studentShortName;
            LastSetActiveValue = isActive;
            var index = snapshots.FindIndex(snapshot => snapshot.Student.ShortName == studentShortName);
            var snapshot = snapshots[index];
            var updatedStudent = snapshot.Student with { IsActive = isActive };
            var updated = snapshot with
            {
                Record = snapshot.Record with { Student = updatedStudent }
            };
            snapshots[index] = updated;
            return Task.FromResult(OperationResult<StudentOverviewSnapshot>.Success(updated));
        }
    }

    // Captures student save requests and can return a failure to exercise the Details dialog.
    private sealed class FakeStudentEditorService : IStudentEditorService
    {
        public string SavedShortName { get; init; } = "Anna";

        public bool SaveFails { get; init; }

        public SaveStudentRequest? LastRequest { get; private set; }

        public Task<StudentEditorSnapshot?> LoadAsync(string shortName, CancellationToken cancellationToken) =>
            Task.FromResult<StudentEditorSnapshot?>(null);

        public Task<OperationResult<string>> SaveAsync(SaveStudentRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(SaveFails
                ? OperationResult<string>.Failure(new ValidationError("Student.SaveFailed", "Student could not be saved."))
                : OperationResult<string>.Success(SavedShortName));
        }
    }

    // Provides assignable catalog plans for student editor tests without a database.
    private sealed class FakePlanCatalogService : IPlanCatalogService
    {
        private readonly IReadOnlyList<PlanCatalogItem> assignablePlans;

        public FakePlanCatalogService(IReadOnlyList<PlanCatalogItem>? assignablePlans = null)
        {
            this.assignablePlans = assignablePlans ?? [CreateCatalogPlan("Standard")];
        }

        public Task<IReadOnlyList<PlanCatalogItem>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(assignablePlans);

        public Task<IReadOnlyList<PlanCatalogItem>> ListAssignableAsync(CancellationToken cancellationToken) =>
            Task.FromResult(assignablePlans);

        public Task<OperationResult<PlanCatalogItem>> CreateDraftAsync(PlanDraftInput input, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<PlanCatalogItem>> UpdateDraftAsync(int planId, PlanDraftInput input, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<PlanCatalogItem>> RenameAsync(int planId, string newName, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<bool>> DeleteDraftAsync(int planId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<PlanCatalogItem>> PublishDraftAsync(int planId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<PlanCatalogItem>> RetireAsync(int planId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<PlanCatalogItem>> ReactivateAsync(int planId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<PlanCatalogItem>> UpdateRetireOnAsync(int planId, DateTimeOffset? retireOn, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OperationResult<int>> RetireDueAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    // Scripts how unsaved student changes are resolved and counts the prompts.
    private sealed class FakeStudentOverviewInteractionService : IStudentOverviewInteractionService
    {
        public StudentEditSaveChangesChoice NextSaveChangesChoice { get; init; } = StudentEditSaveChangesChoice.Discard;

        public int PromptSaveChangesCount { get; private set; }

        public Task<StudentEditSaveChangesChoice> PromptSaveChangesAsync(CancellationToken cancellationToken)
        {
            PromptSaveChangesCount++;
            return Task.FromResult(NextSaveChangesChoice);
        }
    }

    // Captures the invoice number sent to the viewer from Student Details.
    private sealed class FakeInvoiceWorkflowLauncher : IInvoiceWorkflowLauncher
    {
        public InvoiceNumber? LastViewedInvoiceNumber { get; private set; }

        public Task<bool> ShowWriteInvoiceAsync(
            DateOnly displayMonth,
            StudentChoice student,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> ShowResumeInvoiceAsync(CancellationToken cancellationToken) => Task.FromResult(false);

        public Task ShowInvoiceViewerAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
        {
            LastViewedInvoiceNumber = invoiceNumber;
            return Task.CompletedTask;
        }
    }

    // Fixes the time used by student editor tests so date defaults are reproducible.
    private sealed class FakeUiClock : IUiClock
    {
        public FakeUiClock(DateTimeOffset now)
        {
            Now = now;
        }

        public DateTimeOffset Now { get; }
    }
}
