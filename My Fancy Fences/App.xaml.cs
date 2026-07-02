using System.Configuration;
using System.Data;
using System.Windows;

namespace My_Fancy_Fences
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            LocalizationService.Initialize();
            EventManager.RegisterClassHandler(
                typeof(Window),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler((sender, _) =>
                {
                    if (sender is Window window)
                        LocalizationService.Apply(window);
                }));
            base.OnStartup(e);
            _ = UpdateService.CheckAsync();
        }
    }

}
