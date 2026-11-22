using System.Windows;
using ElenasBusinessLedger.App.Services;
using ElenasBusinessLedger.App.ViewModels;

namespace ElenasBusinessLedger.App.Views;

// Presents the separate lesson editor and connects its completion result to WPF dialog closing.
public partial class LessonEditorView : Window
{
    public LessonEditorView(LessonEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
        WindowPlacementStore.Attach(this, nameof(LessonEditorView));
    }

    private void OnCloseRequested(object? sender, bool? dialogResult)
    {
        DialogResult = dialogResult;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
