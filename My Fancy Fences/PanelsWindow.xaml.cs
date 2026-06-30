using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using MahApps.Metro.IconPacks;

namespace My_Fancy_Fences;

public partial class PanelsWindow : Window
{
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;

    private SupportBannerWindow? _supportBannerWindow;
    private static readonly HttpClient UpdateClient = CreateUpdateClient();
    private bool _hasCheckedForUpdates;
    private string? _latestReleaseUrl;

    public event EventHandler<PanelVisibilityChangedEventArgs>? PanelVisibilityChanged;
    public event EventHandler? RefreshIconsRequested;
    public event EventHandler<ActivationModeChangedEventArgs>? ActivationModeChanged;

    public PanelsWindow(IReadOnlyList<PanelOverviewItem> panels, bool useDoubleClickToOpen)
    {
        InitializeComponent();
        Icon = AppIconProvider.Image;
        DoubleClickActivationCheckBox.IsChecked = useDoubleClickToOpen;
        CurrentVersionText.Text = $"v{GetCurrentVersion()}";
        UpdatePanels(panels);

        Loaded += PanelsWindow_Loaded;
        LocationChanged += (_, _) => UpdateSupportBannerPosition();
        SizeChanged += (_, _) => UpdateSupportBannerPosition();
        StateChanged += (_, _) => UpdateSupportBannerVisibility();
        Activated += (_, _) => PlaceSupportBannerBehind();
        Closed += (_, _) => CloseSupportBanner();
    }

    private void PanelsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_supportBannerWindow is not null)
            return;

        _supportBannerWindow = new SupportBannerWindow
        {
            BehindWindowHandle = new WindowInteropHelper(this).Handle
        };
        _supportBannerWindow.Clicked += SupportBannerWindow_Clicked;
        _supportBannerWindow.Show();
        UpdateSupportBannerPosition();
        PlaceSupportBannerBehind();
    }

    private void UpdateSupportBannerPosition()
    {
        if (_supportBannerWindow is null || !_supportBannerWindow.IsLoaded)
            return;

        _supportBannerWindow.Left = Left - _supportBannerWindow.ActualWidth + 1;
        _supportBannerWindow.Top = Top + ((ActualHeight - 63) / 2) - 72;
        PlaceSupportBannerBehind();
    }

    private void PlaceSupportBannerBehind()
    {
        if (_supportBannerWindow is null || !_supportBannerWindow.IsLoaded)
            return;

        var bannerHandle = new WindowInteropHelper(_supportBannerWindow).Handle;
        var settingsHandle = new WindowInteropHelper(this).Handle;
        if (bannerHandle == IntPtr.Zero || settingsHandle == IntPtr.Zero)
            return;

        SetWindowPos(
            bannerHandle,
            settingsHandle,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoActivate);
    }

    private void SupportBannerWindow_Clicked(object? sender, EventArgs e)
    {
        PlaceSupportBannerBehind();

        Process.Start(new ProcessStartInfo("https://ko-fi.com/infitisstudio#linkModal")
        {
            UseShellExecute = true
        });

        Dispatcher.BeginInvoke(
            DispatcherPriority.ApplicationIdle,
            new Action(PlaceSupportBannerBehind));
    }

    private void UpdateSupportBannerVisibility()
    {
        if (_supportBannerWindow is null)
            return;

        if (WindowState == WindowState.Minimized)
            _supportBannerWindow.Hide();
        else
        {
            _supportBannerWindow.Show();
            UpdateSupportBannerPosition();
        }
    }

    private void CloseSupportBanner()
    {
        _supportBannerWindow?.Close();
        _supportBannerWindow = null;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr windowHandle,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

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

    private async void SettingsTab_Checked(object sender, RoutedEventArgs e)
    {
        if (GeneralTabContent is null || PanelsTabContent is null ||
            ImportExportTabContent is null || UpdatesTabContent is null)
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
        UpdatesTabContent.Visibility = selectedTab == "Updates"
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (selectedTab == "Updates" && !_hasCheckedForUpdates)
            await CheckForUpdatesAsync();
    }

    private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e) =>
        await CheckForUpdatesAsync();

    private void OpenReleaseButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_latestReleaseUrl))
            return;

        Process.Start(new ProcessStartInfo(_latestReleaseUrl) { UseShellExecute = true });
    }

    private async Task CheckForUpdatesAsync()
    {
        CheckUpdatesButton.IsEnabled = false;
        OpenReleaseButton.Visibility = Visibility.Collapsed;
        UpdateStatusText.Text = "Sprawdzanie aktualizacji…";
        LatestVersionText.Text = "sprawdzanie…";

        try
        {
            using var response = await UpdateClient.GetAsync(
                "repos/infitis-studio/my-fancy-fences/releases/latest");
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);
            var root = document.RootElement;
            var tag = root.GetProperty("tag_name").GetString() ?? string.Empty;
            _latestReleaseUrl = root.GetProperty("html_url").GetString();
            LatestVersionText.Text = tag;

            var currentVersion = GetCurrentVersion();
            if (!TryParseVersion(tag, out var latestVersion))
            {
                UpdateStatusText.Text = "Nie udało się odczytać numeru najnowszej wersji";
                OpenReleaseButton.Visibility = Visibility.Visible;
            }
            else if (latestVersion > currentVersion)
            {
                UpdateStatusText.Text = "Dostępna jest nowa wersja";
                OpenReleaseButton.Visibility = Visibility.Visible;
            }
            else
            {
                UpdateStatusText.Text = "Masz najnowszą wersję";
            }

            _hasCheckedForUpdates = true;
        }
        catch (Exception)
        {
            UpdateStatusText.Text = "Nie udało się połączyć z GitHubem";
            LatestVersionText.Text = "—";
        }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
        }
    }

    private static HttpClient CreateUpdateClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://api.github.com/"),
            Timeout = TimeSpan.FromSeconds(10)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("My-Fancy-Fences-Update-Checker");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private static Version GetCurrentVersion() =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 1, 0);

    private static bool TryParseVersion(string tag, out Version version) =>
        Version.TryParse(tag.Trim().TrimStart('v', 'V'), out version!);

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
