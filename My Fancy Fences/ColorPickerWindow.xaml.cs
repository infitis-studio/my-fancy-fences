using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace My_Fancy_Fences;

public partial class ColorPickerWindow : Window
{
    private double _hue;
    private double _saturation;
    private double _value;
    private bool _isUpdating;

    public Color SelectedColor { get; private set; }
    public double SelectedOpacity { get; private set; }
    public event EventHandler<ColorPickerPreviewEventArgs>? PreviewChanged;

    public ColorPickerWindow(Color initialColor, double initialOpacity)
    {
        InitializeComponent();
        Icon = AppIconProvider.Image;
        RgbToHsv(initialColor, out _hue, out _saturation, out _value);

        _isUpdating = true;
        HueSlider.Value = _hue;
        SaturationSlider.Value = _saturation * 100;
        BrightnessSlider.Value = _value * 100;
        OpacitySlider.Value = initialOpacity * 100;
        _isUpdating = false;
        UpdateColor();

        Loaded += (_, _) =>
        {
            Activate();
            Focus();
        };
    }

    private void ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdating || !IsInitialized)
            return;

        _hue = HueSlider.Value;
        _saturation = SaturationSlider.Value / 100;
        _value = BrightnessSlider.Value / 100;
        UpdateColor();
    }

    private void UpdateColor()
    {
        SelectedColor = HsvToColor(_hue, _saturation, _value);
        SelectedOpacity = OpacitySlider.Value / 100;

        var opaqueBrush = new SolidColorBrush(SelectedColor);
        var alphaColor = Color.FromArgb(
            (byte)Math.Round(SelectedOpacity * 255),
            SelectedColor.R,
            SelectedColor.G,
            SelectedColor.B);

        LargeColorPreview.Background = new SolidColorBrush(alphaColor);
        SaturationTrack.Background = new LinearGradientBrush(
            Colors.White,
            HsvToColor(_hue, 1, Math.Max(_value, .5)),
            0);
        BrightnessTrack.Background = new LinearGradientBrush(
            Colors.Black,
            HsvToColor(_hue, _saturation, 1),
            0);
        OpacityTrack.Background = new LinearGradientBrush(
            Color.FromArgb(0, SelectedColor.R, SelectedColor.G, SelectedColor.B),
            SelectedColor,
            0);

        _isUpdating = true;
        HexTextBox.Text = $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
        _isUpdating = false;

        ValuesText.Text =
            $"H {Math.Round(_hue)}  S {Math.Round(_saturation * 100)}  B {Math.Round(_value * 100)}  A {Math.Round(SelectedOpacity * 100)}%";

        if (IsLoaded)
            PreviewChanged?.Invoke(this, new ColorPickerPreviewEventArgs(SelectedColor, SelectedOpacity));
    }

    private void HexTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isUpdating || !IsLoaded)
            return;

        try
        {
            var value = HexTextBox.Text.Trim();
            if (!value.StartsWith('#'))
                value = $"#{value}";
            if (value.Length != 7)
                return;

            var color = (Color)ColorConverter.ConvertFromString(value);
            RgbToHsv(color, out _hue, out _saturation, out _value);

            _isUpdating = true;
            HueSlider.Value = _hue;
            SaturationSlider.Value = _saturation * 100;
            BrightnessSlider.Value = _value * 100;
            _isUpdating = false;
            UpdateColor();
        }
        catch
        {
            // Partial HEX input is allowed while typing.
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private static Color HsvToColor(double hue, double saturation, double value)
    {
        var chroma = value * saturation;
        var x = chroma * (1 - Math.Abs(hue / 60 % 2 - 1));
        var m = value - chroma;
        var (r, g, b) = hue switch
        {
            < 60 => (chroma, x, 0d),
            < 120 => (x, chroma, 0d),
            < 180 => (0d, chroma, x),
            < 240 => (0d, x, chroma),
            < 300 => (x, 0d, chroma),
            _ => (chroma, 0d, x)
        };

        return Color.FromRgb(
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255));
    }

    private static void RgbToHsv(Color color, out double hue, out double saturation, out double value)
    {
        var r = color.R / 255d;
        var g = color.G / 255d;
        var b = color.B / 255d;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        hue = delta == 0
            ? 0
            : max == r
                ? 60 * (((g - b) / delta) % 6)
                : max == g
                    ? 60 * ((b - r) / delta + 2)
                    : 60 * ((r - g) / delta + 4);
        if (hue < 0)
            hue += 360;

        saturation = max == 0 ? 0 : delta / max;
        value = max;
    }
}

public sealed record ColorPickerPreviewEventArgs(Color Color, double Opacity);
