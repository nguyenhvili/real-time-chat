using RealTimeChat.Client.Models;
using RealTimeChat.Client.Services;
using RealTimeChat.Client.Views;
using RealTimeChat.Models.Models;
using System.Collections.ObjectModel;
using System.Net.WebSockets;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RealTimeChat.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AppSettings _settings;
        private IChatService? _chatService;
        private ObservableCollection<Message> _messages;

        public MainWindow()
        {
            InitializeComponent();

            _settings = new AppSettings
            {
                Username = string.Empty,
                UseWebSocket = true,
                ServerUrl = "wss://localhost:7269"
            };

            _messages = [];
            MessagesItemsControl.ItemsSource = _messages;

            // Initialize menu item states
            UpdateMenuItemStates(false);
        }

        public async Task InitializeAsync(string username)
        {
            _settings.Username = username;
            UsernameStatusText.Text = username;

            await ConnectToChatAsync();
        }

        private async Task ConnectToChatAsync()
        {
            try
            {
                // Disconnect existing service if any
                if (_chatService != null)
                {
                    _chatService.MessageReceived -= OnMessageReceived;
                    _chatService.ConnectionStatusChanged -= OnConnectionStatusChanged;
                    await _chatService.DisconnectAsync();
                }
            }
            catch
            {
                // We can assume that connection is closed, since we are switching to a new type of connection.
            }

            try
            {
                // Create appropriate service based on settings
                _chatService = _settings.UseWebSocket
                    ? new WebSocketChatService(_settings.ServerUrl)
                    : new RestApiChatService(_settings.ServerUrl);

                _chatService.MessageReceived += OnMessageReceived;
                _chatService.ConnectionStatusChanged += OnConnectionStatusChanged;

                UpdateConnectionType();
                UpdateConnectionStatus("Connecting...", Colors.Orange);

                // Connect to the service
                await _chatService.ConnectAsync();

                // Load message history
                var history = await _chatService.InitializeMessages();

                Dispatcher.Invoke(() =>
                {
                    _messages.Clear();
                    foreach (var message in history)
                    {
                        _messages.Add(message);
                    }
                    ScrollToBottom();
                });

                UpdateConnectionStatus("Connected", Colors.Green);
                EnableChat(true);
                UpdateMenuItemStates(true);
            }
            catch (Exception ex)
            {
                UpdateConnectionStatus($"Connection failed: {ex.Message}", Colors.Red);
                UpdateMenuItemStates(false);
                MessageBox.Show($"Failed to connect to chat server: {ex.Message}",
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnMessageReceived(object? sender, Message message)
        {
            Dispatcher.Invoke(() =>
            {
                _messages.Add(message);
                ScrollToBottom();
            });
        }

        private void OnConnectionStatusChanged(object? sender, string status)
        {
            Dispatcher.Invoke(() =>
            {
                if (status.Contains("Connected"))
                {
                    UpdateConnectionStatus(status, Colors.Green);
                    UpdateMenuItemStates(true);
                }
                else if (status.Contains("Disconnected") || status.Contains("failed"))
                {
                    UpdateConnectionStatus(status, Colors.Red);
                    EnableChat(false);
                    UpdateMenuItemStates(false);
                }
                else
                {
                    UpdateConnectionStatus(status, Colors.Orange);
                }
            });
        }

        private void UpdateConnectionStatus(string status, Color color)
        {
            ConnectionStatusText.Text = status;
            StatusIndicator.Fill = new SolidColorBrush(color);
        }

        private void UpdateConnectionType()
        {
            ConnectionTypeText.Text = _settings.UseWebSocket ? "WebSocket" : "REST API";
        }

        private void EnableChat(bool enabled)
        {
            MessageTextBox.IsEnabled = enabled;
            SendButton.IsEnabled = enabled;

            if (enabled)
            {
                MessageTextBox.Focus();
            }
        }

        private void UpdateMenuItemStates(bool isConnected)
        {
            ReconnectMenuItem.IsEnabled = !isConnected;
            DisconnectMenuItem.IsEnabled = isConnected;
        }

        private void ScrollToBottom()
        {
            ChatScrollViewer.ScrollToBottom();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SendMessageAsync();
            }
        }

        private async Task SendMessageAsync()
        {
            var messageText = MessageTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(messageText))
            {
                return;
            }

            if (_chatService == null || !_chatService.IsConnected)
            {
                MessageBox.Show("Not connected to chat server.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _chatService.SendMessageAsync(_settings.Username, messageText);
                MessageTextBox.Clear();
                MessageTextBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send message: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_settings);

            if (settingsWindow.ShowDialog() == true)
            {
                var oldUseWebSocket = _settings.UseWebSocket;
                _settings = settingsWindow.Settings;

                UsernameStatusText.Text = _settings.Username;

                // Reconnect if connection type changed
                if (oldUseWebSocket != _settings.UseWebSocket)
                {
                    var result = MessageBox.Show(
                        "Connection type changed. Reconnect now?",
                        "Reconnect Required",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        await ConnectToChatAsync();
                    }
                }
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void ReconnectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await ConnectToChatAsync();
        }

        private async void DisconnectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_chatService != null)
            {
                try
                {
                    await _chatService.DisconnectAsync();
                    UpdateConnectionStatus("Disconnected", Colors.Gray);
                    EnableChat(false);
                    UpdateMenuItemStates(false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to disconnect: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_chatService != null)
            {
                await _chatService.DisconnectAsync();
            }
        }
    }
}