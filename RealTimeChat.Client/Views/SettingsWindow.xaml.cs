using System.Windows;
using RealTimeChat.Client.Models;

namespace RealTimeChat.Client.Views
{
    public partial class SettingsWindow : Window
    {
        public AppSettings Settings { get; private set; }

        public SettingsWindow(AppSettings currentSettings)
        {
            InitializeComponent();

            Settings = new AppSettings
            {
                Username = currentSettings.Username,
                UseWebSocket = currentSettings.UseWebSocket,
                ServerUrl = currentSettings.ServerUrl
            };

            UsernameTextBox.Text = Settings.Username;
            WebSocketRadioButton.IsChecked = Settings.UseWebSocket;
            RestApiRadioButton.IsChecked = !Settings.UseWebSocket;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Username is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (username.Length > 100)
            {
                MessageBox.Show("Username must be 100 characters or less.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Settings.Username = username;
            Settings.UseWebSocket = WebSocketRadioButton.IsChecked == true;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
