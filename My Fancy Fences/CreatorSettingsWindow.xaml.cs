using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace My_Fancy_Fences;

public partial class CreatorSettingsWindow : Window
{
    public bool HideHeader { get; private set; }
    public Color BackgroundColor { get; private set; }
    public double BackgroundOpacity { get; private set; }
    public bool ApplyHeaderToAll => true;
    public bool ApplyColorToAll => true;

    public event EventHandler<CreatorSettingsPreviewEventArgs>? PreviewChanged;

    public CreatorSettingsWindow(bool hideHeader, Color backgroundColor, double backgroundOpacity)
    {
        InitializeComponent();
        Icon = AppIconProvider.Image;
        HideHeader = hideHeader;
        BackgroundColor = backgroundColor;
        BackgroundOpacity = backgroundOpacity;
        HideHeaderCheckBox.IsChecked = hideHeader;
        UpdateColorPreview();
    }

    private void AppearanceOption_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            RaisePreviewChanged();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        HideHeader = HideHeaderCheckBox.IsChecked == true;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void BackgroundColorPreview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var originalColor = BackgroundColor;
        var originalOpacity = BackgroundOpacity;
        var picker = new ColorPickerWindow(BackgroundColor, BackgroundOpacity);

        picker.PreviewChanged += (_, preview) =>
        {
            BackgroundColor = preview.Color;
            BackgroundOpacity = preview.Opacity;
            UpdateColorPreview();
            RaisePreviewChanged();
        };

        if (picker.ShowDialog() != true)
        {
            BackgroundColor = originalColor;
            BackgroundOpacity = originalOpacity;
            UpdateColorPreview();
            RaisePreviewChanged();
            return;
        }

        BackgroundColor = picker.SelectedColor;
        BackgroundOpacity = picker.SelectedOpacity;
        UpdateColorPreview();
        RaisePreviewChanged();
    }

    private void UpdateColorPreview()
    {
        BackgroundColorTextBox.Text =
            $"#{BackgroundColor.R:X2}{BackgroundColor.G:X2}{BackgroundColor.B:X2}";
        BackgroundColorPreview.Background = new SolidColorBrush(Color.FromArgb(
            (byte)Math.Round(BackgroundOpacity * 255),
            BackgroundColor.R,
            BackgroundColor.G,
            BackgroundColor.B));
    }

    private void RaisePreviewChanged() =>
        PreviewChanged?.Invoke(this, new CreatorSettingsPreviewEventArgs(
            HideHeaderCheckBox.IsChecked == true,
            BackgroundColor,
            BackgroundOpacity,
            ApplyHeaderToAll,
            ApplyColorToAll));
}

public sealed record CreatorSettingsPreviewEventArgs(
    bool HideHeader,
    Color BackgroundColor,
    double BackgroundOpacity,
    bool ApplyHeaderToAll,
    bool ApplyColorToAll);
