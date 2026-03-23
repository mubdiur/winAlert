# winAlert

Enterprise Alert Monitor - A native Windows GUI application that listens on a network port for alerts and provides intelligent visual and audio notifications based on severity.

## Features

- **TCP Network Listener**: Listens on a configurable port for incoming JSON alerts
- **Intelligent Notifications**: Visual and audio alerts based on severity level
- **Alert Triage**: Automatic classification and escalation based on response time
- **System Tray Integration**: Minimize to system tray with status indicator
- **Enterprise Architecture**: MVVM pattern with dependency injection
- **Dark Theme UI**: Modern, professional interface

## Requirements

- Windows 10/11
- .NET 8.0 Runtime

## Building

```bash
cd src/winAlert
dotnet restore
dotnet build
```

## Running

```bash
dotnet run
```

## Alert Protocol

Send alerts as JSON over TCP:

```json
{
  "id": "uuid-string",
  "severity": "critical|high|medium|low|info",
  "source": "monitoring-system",
  "title": "Alert Title",
  "message": "Detailed alert message...",
  "timestamp": "2026-03-22T10:30:00Z",
  "metadata": {
    "key": "value"
  },
  "requireAcknowledgment": true,
  "autoCloseSeconds": 0
}
```

## Configuration

Settings are stored in `%LOCALAPPDATA%\winAlert\settings.json`

## License

MIT
