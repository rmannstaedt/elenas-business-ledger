using System.Windows;
using ElenaSmallBusinessLedger.App.ViewModels;

namespace ElenaSmallBusinessLedger.App.Views;

public partial class LessonEditorView : Window
{
    public LessonEditorView(LessonEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
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
