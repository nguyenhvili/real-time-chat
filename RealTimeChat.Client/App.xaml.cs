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

            // Set as main window to control shutdown
            MainWindow = mainWindow;
            mainWindow.Show();

            if (usernameDialog.ShowDialog() == true)
            {
                // Initialize asynchronously (fire and forget is OK here - window handles errors)
                _ = mainWindow.InitializeAsync(usernameDialog.Username);
            }
            else
            {
                // User cancelled, exit application
                Shutdown();
            }
        }
    }
}
