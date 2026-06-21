using System.IO;
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

    private void OnBrowseInvoiceFolder(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFolderDialog
        {
            Title = "Select invoice folder"
        };
        if (Directory.Exists(viewModel.InvoiceFolderPath))
        {
            dialog.InitialDirectory = viewModel.InvoiceFolderPath;
        }

        if (dialog.ShowDialog(this) == true)
        {
            viewModel.InvoiceFolderPath = dialog.FolderName;
        }
    }
}
