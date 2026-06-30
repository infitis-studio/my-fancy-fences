using System.Windows;
using System.Windows.Input;

namespace My_Fancy_Fences;

public partial class ConfirmationWindow : Window
{
    public ConfirmationWindow(string title, string message, string confirmText)
    {
        InitializeComponent();
        Icon = AppIconProvider.Image;
        Title = title;
        DialogTitleText.Text = title;
        DialogMessageText.Text = message;
        ConfirmButton.Content = confirmText;
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }
}
