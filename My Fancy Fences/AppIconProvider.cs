using System.Windows;
using System.Windows.Media;

namespace My_Fancy_Fences;

public static class AppIconProvider
{
    public static ImageSource Image { get; } = CreateImage();

    private static ImageSource CreateImage()
    {
        var drawing = new DrawingGroup();
        var brush = new SolidColorBrush(Color.FromRgb(0xE1, 0xD8, 0xD2));
        brush.Freeze();

        drawing.Children.Add(CreateTile(4, 4, brush));
        drawing.Children.Add(CreateTile(18, 4, brush));
        drawing.Children.Add(CreateTile(4, 18, brush));
        drawing.Children.Add(CreateTile(18, 18, brush));
        drawing.Freeze();

        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }

    private static GeometryDrawing CreateTile(double x, double y, Brush brush) =>
        new(
            brush,
            null,
            new RectangleGeometry(new Rect(x, y, 10, 10), 1.5, 1.5));
}
