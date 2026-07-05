using System.Windows;
using System.Windows.Input;

namespace RealTimeChat.Client.Views
{
    public partial class UsernameDialog : Window
    {
        public string Username { get; private set; } = string.Empty;

        public UsernameDialog()
        {
            InitializeComponent();
            UsernameTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateUsername())
            {
                Username = UsernameTextBox.Text.Trim();
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void UsernameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkButton_Click(sender, e);
            }
        }

        private bool ValidateUsername()
        {
            var username = UsernameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                ErrorTextBlock.Text = "Username is required.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                return false;
            }

            if (username.Length > 100)
            {
                ErrorTextBlock.Text = "Username must be 100 characters or less.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                return false;
            }

            ErrorTextBlock.Visibility = Visibility.Collapsed;
            return true;
        }
    }
}
