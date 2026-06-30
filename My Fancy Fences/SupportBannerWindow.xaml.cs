using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace My_Fancy_Fences;

public partial class SupportBannerWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExNoActivate = 0x08000000;
    private const int WmMouseActivate = 0x0021;
    private const int MaNoActivate = 3;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;

    public event EventHandler? Clicked;
    public IntPtr BehindWindowHandle { get; init; }

    public SupportBannerWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => DisableWindowActivation();
    }

    private void DisableWindowActivation()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var extendedStyle = GetWindowLong(handle, GwlExStyle);
        SetWindowLong(handle, GwlExStyle, extendedStyle | WsExNoActivate);

        if (HwndSource.FromHwnd(handle) is { } source)
            source.AddHook(WindowMessageHook);
    }

    private IntPtr WindowMessageHook(
        IntPtr windowHandle,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (message != WmMouseActivate)
            return IntPtr.Zero;

        if (BehindWindowHandle != IntPtr.Zero)
        {
            SetWindowPos(
                windowHandle,
                BehindWindowHandle,
                0,
                0,
                0,
                0,
                SwpNoMove | SwpNoSize | SwpNoActivate);
        }

        handled = true;
        return new IntPtr(MaNoActivate);
    }

    private void SupportBanner_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr windowHandle, int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr windowHandle, int index, int newStyle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr windowHandle,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
