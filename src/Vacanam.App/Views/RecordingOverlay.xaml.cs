using System.Windows;
using Vacanam.App.ViewModels;

namespace Vacanam.App.Views;

/// <summary>
/// Floating overlay displayed at the bottom-center of the primary screen during recording.
/// This window does NOT capture keyboard or mouse input (IsHitTestVisible=False).
/// </summary>
public partial class RecordingOverlay : Window
{
    public RecordingOverlay(RecordingOverlayViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        PositionAtBottomCenter();
    }

    private void PositionAtBottomCenter()
    {
        var screen = System.Windows.SystemParameters.WorkArea;
        // Position will be refreshed after content renders
        Loaded += (_, _) =>
        {
            Left = (screen.Width - ActualWidth) / 2;
            Top = screen.Bottom - ActualHeight - 48;
        };
    }
}
