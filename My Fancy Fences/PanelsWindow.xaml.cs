using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;

namespace My_Fancy_Fences;

public partial class PanelsWindow : Window
{
    public event EventHandler<PanelVisibilityChangedEventArgs>? PanelVisibilityChanged;
    public event EventHandler? RefreshIconsRequested;
    public event EventHandler<ActivationModeChangedEventArgs>? ActivationModeChanged;

    public PanelsWindow(IReadOnlyList<PanelOverviewItem> panels, bool useDoubleClickToOpen)
    {
        InitializeComponent();
        Icon = AppIconProvider.Image;
        DoubleClickActivationCheckBox.IsChecked = useDoubleClickToOpen;
        UpdatePanels(panels);
    }

    public void UpdatePanels(IReadOnlyList<PanelOverviewItem> panels)
    {
        PanelsItemsControl.ItemsSource = panels;
        PanelCountText.Text = panels.Count switch
        {
            1 => "1 panel",
            2 or 3 or 4 => $"{panels.Count} panele",
            _ => $"{panels.Count} paneli"
        };
    }

    private void SettingsTab_Checked(object sender, RoutedEventArgs e)
    {
        if (GeneralTabContent is null || PanelsTabContent is null || ImportExportTabContent is null)
            return;

        var selectedTab = (sender as FrameworkElement)?.Tag as string ?? "General";
        GeneralTabContent.Visibility = selectedTab == "General"
            ? Visibility.Visible
            : Visibility.Collapsed;
        PanelsTabContent.Visibility = selectedTab == "Panels"
            ? Visibility.Visible
            : Visibility.Collapsed;
        ImportExportTabContent.Visibility = selectedTab == "ImportExport"
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void PanelVisibilityButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button ||
            button.Tag is not string panelKey ||
            button.DataContext is not PanelOverviewItem panel)
        {
            return;
        }

        PanelVisibilityChanged?.Invoke(
            this,
            new PanelVisibilityChangedEventArgs(panelKey, !panel.IsHidden));
    }

    private void RefreshIconsButton_Click(object sender, RoutedEventArgs e) =>
        RefreshIconsRequested?.Invoke(this, EventArgs.Empty);

    private void DoubleClickActivationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        ActivationModeChanged?.Invoke(
            this,
            new ActivationModeChangedEventArgs(DoubleClickActivationCheckBox.IsChecked == true));
    }
}

public sealed record PanelOverviewItem(
    string PanelKey,
    string Title,
    PackIconLucideKind Icon,
    string FolderPath,
    string Details,
    string Status,
    bool IsHidden);

public sealed record PanelVisibilityChangedEventArgs(string PanelKey, bool IsHidden);

public sealed record ActivationModeChangedEventArgs(bool UseDoubleClickToOpen);
