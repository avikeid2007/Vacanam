using System.Windows;
using Vacanam.App.ViewModels;

namespace Vacanam.App.Views;

/// <summary>
/// Settings window code-behind. Minimal — all logic is in SettingsViewModel.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.SaveCompleted += OnCloseRequested;
        _viewModel.CancelRequested += OnCloseRequested;
        Closed += OnClosed;
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.SaveCompleted -= OnCloseRequested;
        _viewModel.CancelRequested -= OnCloseRequested;
        Closed -= OnClosed;
    }
}
