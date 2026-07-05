# Real-Time Chat Client

A WPF-based chat client for the Real-Time Chat Server with support for both WebSocket and REST API communication.

## Features

### 1. **Username Dialog on Startup**
- When the app starts, you'll be prompted to enter a username (required, max 100 characters)
- Username validation ensures it's not empty and within the character limit

### 2. **Real-Time Chat Interface**
- Chat interface with message history
- Messages display sender name, timestamp, and content

### 3. **Dual Communication Modes**

#### WebSocket Mode (Default)
- Real-time bidirectional communication
- Instant message delivery
- Efficient for continuous chat sessions

#### REST API Mode
- HTTP-based polling (checks for new messages every 1 second)
- Useful for testing or when WebSocket is not available
- Simpler fallback option

### 4. **Settings Window**
Access via **File → Settings** menu to:
- Change your username
- Switch between WebSocket and REST API modes

### 5. **Status Bar**
Displays:
- Current username
- Connection status with color indicator (🟢 Green = Connected, 🔴 Red = Disconnected, 🟠 Orange = Connecting)
- Current connection type (WebSocket or REST API)

## Usage

### Starting the Application
1. Launch the application
2. Enter your username in the dialog
3. Click **OK** or press **Enter**
4. The app will automatically connect to the chat server and load message history

### Sending Messages
- Type your message in the text box at the bottom
- Click **Send** or press **Enter**
- Messages are limited to 1000 characters

### Changing Settings
1. Go to **File → Settings**
2. Update your username or connection type
3. Click **Save**
4. If you changed the connection type, you'll be prompted to reconnect

### Reconnecting
- Use **Connection → Reconnect** menu if connection is lost
- The app will attempt to reconnect and reload message history

## Configuration

Default server URL: `https://localhost:7253`

To change the server URL, modify the `ServerUrl` property in `MainWindow.xaml.cs`:

```csharp
_settings = new AppSettings
{
	Username = string.Empty,
	UseWebSocket = true,
	ServerUrl = "https://localhost:7253" // Change this
};
```

## Project Structure

```
RealTimeChat.Client/
├── Models/
│   └── AppSettings.cs              # Application settings model
├── Services/
│   ├── IChatService.cs             # Chat service interface
│   ├── WebSocketChatService.cs     # WebSocket implementation
│   └── RestApiChatService.cs       # REST API implementation
├── Views/
│   ├── UsernameDialog.xaml         # Username input dialog
│   ├── UsernameDialog.xaml.cs
│   ├── SettingsWindow.xaml         # Settings window
│   └── SettingsWindow.xaml.cs
├── MainWindow.xaml                 # Main chat window
├── MainWindow.xaml.cs
├── App.xaml
└── App.xaml.cs                     # Application startup logic
```

## Server Endpoints

The client connects to the following server endpoints:

- **WebSocket**: `wss://localhost:7253/ws`
- **REST API**:
  - GET `/Messages` - Retrieve message history
  - POST `/Messages` - Send a new message

## Requirements

- .NET 10
- WPF
- RealTimeChat.Models project reference
- Running instance of RealTimeChat.Server

## Troubleshooting

### Connection Failed
- Ensure the chat server is running
- Verify the server URL is correct
- Check if firewall is blocking the connection

### Messages Not Appearing
- Check connection status in the status bar
- Try reconnecting via **Connection → Reconnect**
- Verify username is set correctly in settings

### WebSocket Not Working
- Switch to REST API mode in settings
- REST API mode uses polling and is more compatible with restrictive networks

## Future Enhancements

Potential improvements:
- Persistent settings storage (save to file/registry)
- User avatars
- Message notifications
- Private messaging
- Emoji support
- File sharing
- Markdown formatting
