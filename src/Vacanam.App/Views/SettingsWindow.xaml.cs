using System.Windows;
using Vacanam.App.ViewModels;

namespace Vacanam.App.Views;

/// <summary>
/// Settings window code-behind. Minimal — all logic is in SettingsViewModel.
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.SaveCompleted += (_, _) => Close();
        viewModel.CancelRequested += (_, _) => Close();
    }
}
