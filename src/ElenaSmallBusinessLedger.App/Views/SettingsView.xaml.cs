using System.Windows;
using MySmallBusinessLedger.App.Services;
using MySmallBusinessLedger.App.ViewModels;
using Microsoft.Win32;

namespace MySmallBusinessLedger.App.Views;

public partial class SettingsView : Window
{
    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
        WindowPlacementStore.Attach(this, nameof(SettingsView));
    }

    private SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

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

    private void OnBrowseGoogleClientSecrets(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Select Google Calendar login setup file"
        };

        if (dialog.ShowDialog(this) == true)
        {
            ViewModel.GoogleClientSecretsSourcePath = dialog.FileName;
        }
    }
}
