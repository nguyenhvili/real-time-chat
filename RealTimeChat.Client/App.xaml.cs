using System.Windows;
using RealTimeChat.Client.Views;

namespace RealTimeChat.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Show username dialog
            var usernameDialog = new UsernameDialog();

            // Create and show main window with username
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();

            if (usernameDialog.ShowDialog() == true)
            {
                _ = mainWindow.InitializeAsync(usernameDialog.Username);
            }
            else
            {
                Shutdown();
            }
        }
    }
}
