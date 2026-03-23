# winAlert — Enterprise Alert Monitor

## 1. Project Overview

**Project Name:** winAlert  
**Type:** Native Windows GUI Application with Embedded Network Service  
**Core Feature:** Listen on a configurable TCP port for incoming alerts, intelligently dispatching visual and audio notifications based on severity level and operator responsiveness.  
**Target Users:** Enterprise operations centers, NOC teams, IT administrators, on-call personnel

---

## 2. UI/UX Specification

### 2.1 Window Model

| Window | Purpose | Modal? |
|--------|---------|--------|
| MainWindow | Primary dashboard with alert log and status | No |
| SettingsWindow | Configuration dialog | Yes |
| AlertDetailWindow | Detailed alert view with acknowledgment | Yes |

**System Tray Integration:**
- Minimize to system tray
- Tray icon indicates system status (idle/alerting/disabled)
- Right-click context menu: Show, Settings, Exit
- Double-click: Restore main window

### 2.2 Visual Design

**Color Palette:**
| Role | Color | Hex |
|------|-------|-----|
| Background Primary | Dark Charcoal | #1E1E2E |
| Background Secondary | Slate | #2D2D44 |
| Surface | Deep Purple-Gray | #363654 |
| Text Primary | Off-White | #E4E4EF |
| Text Secondary | Muted Lavender | #9999B8 |
| Accent Primary | Electric Blue | #4FC3F7 |
| Accent Secondary | Soft Cyan | #80DEEA |
| Severity Critical | Vivid Red | #FF5252 |
| Severity High | Bright Orange | #FF9800 |
| Severity Medium | Amber Yellow | #FFC107 |
| Severity Low | Cool Green | #66BB6A |
| Severity Info | Steel Blue | #42A5F5 |
| Success | Emerald | #4CAF50 |
| Error | Crimson | #F44336 |

**Typography:**
| Element | Font | Size | Weight |
|---------|------|------|--------|
| Window Title | Segoe UI | 20px | SemiBold |
| Section Headers | Segoe UI | 16px | SemiBold |
| Body Text | Segoe UI | 14px | Regular |
| Alert Title | Segoe UI | 18px | Bold |
| Alert Message | Segoe UI | 14px | Regular |
| Timestamp | Segoe UI | 12px | Light |
| Button Text | Segoe UI | 14px | SemiBold |

**Spacing System:**
- Base unit: 8px
- Margins: 16px (2 units), 24px (3 units)
- Padding: 8px (1 unit), 16px (2 units)
- Component gap: 12px
- Border radius: 6px (buttons), 8px (cards), 12px (panels)

**Visual Effects:**
- Card shadow: `0 4px 12px rgba(0,0,0,0.3)`
- Alert pulse animation for critical (red glow, 1s interval)
- Smooth fade transitions (200ms ease-out)
- Status indicator with subtle breathing animation when idle

### 2.3 Layout Structure

```
┌─────────────────────────────────────────────────────────────────┐
│ [Title Bar] winAlert          [─] [□] [×]                       │
├─────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ HEADER: Status Indicator | Port Status | Connection Count   │ │
│ │         [●] Listening on :8080 | 0 connections | [Settings]   │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ ┌───────────────────────┐ ┌───────────────────────────────────┐ │
│ │                       │ │                                   │ │
│ │   ACTIVE ALERTS       │ │   ALERT LOG                       │ │
│ │   PANEL               │ │   PANEL                           │ │
│ │                       │ │                                   │ │
│ │   (Scrollable list    │ │   (Filterable history of          │ │
│ │    of unacknowledged  │ │    all received alerts)           │ │
│ │    alerts sorted by   │ │                                   │ │
│ │    severity)          │ │   Filter: [All ▼] Search: [____]  │ │
│ │                       │ │                                   │ │
│ │                       │ │   [Alert Card]                    │ │
│ │                       │ │   [Alert Card]                    │ │
│ │                       │ │   [Alert Card]                    │ │
│ │                       │ │                                   │ │
│ └───────────────────────┘ └───────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ FOOTER: Audio [🔊 On] | Acknowledge All | Clear Log | Info   │ │
│ └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### 2.4 Components

**Alert Card:**
- Severity color bar on left edge (4px width)
- Alert ID, timestamp, source, message preview
- Severity badge (Critical/High/Medium/Low/Info)
- Acknowledge button (changes to "Acknowledged" state)
- States: New (pulsing border), Acknowledged (muted), Expired (grayed)

**Status Indicator:**
- Circle icon with dynamic color
- States:
  - Idle (gray, static)
  - Listening (green, subtle pulse)
  - Alert (severity color, urgent pulse for critical)
  - Error (red, static)
  - Disabled (gray, static)

**Buttons:**
- Primary: Accent blue background, white text
- Secondary: Transparent with border
- Danger: Red background for destructive actions
- States: Default, Hover (+10% brightness), Active (-10% brightness), Disabled (50% opacity)

**Toggle Switch:**
- For audio on/off, auto-acknowledge settings
- Smooth sliding animation (150ms)

---

## 3. Functional Specification

### 3.1 Core Features

#### 3.1.1 Network Alert Receiver Service

**Protocol:** TCP JSON over raw sockets

**Message Format (Incoming JSON):**
```json
{
  "id": "uuid-string",
  "severity": "critical|high|medium|low|info",
  "source": "string (max 100 chars)",
  "title": "string (max 200 chars)",
  "message": "string (max 4000 chars)",
  "timestamp": "ISO8601 datetime string",
  "metadata": {
    "key": "value pairs"
  },
  "requireAcknowledgment": true,
  "autoCloseSeconds": 0
}
```

**Server Behavior:**
- Default port: 8888 (configurable)
- Accept multiple concurrent connections
- Parse JSON messages
- Validate message schema
- Support connection keep-alive
- Handle malformed messages gracefully (log and disconnect)
- Broadcast to UI layer via event aggregator

#### 3.1.2 Alert Triage Engine

**Severity Classification:**
| Severity | Visual | Audio | Behavior |
|----------|--------|-------|----------|
| Critical | Full-screen flash, red pulse overlay | Alarm sound (pulsing) | Requires explicit acknowledgment, blocks until ack'd |
| High | Notification banner, orange glow | Alert tone (3 beeps) | Requires acknowledgment within 60s or escalates |
| Medium | System tray notification, yellow | Single beep | Auto-dismiss after 30s if not acknowledged |
| Low | System tray notification, green | Silent | Auto-dismiss after 15s |
| Info | Subtle toast, blue | Silent | Auto-dismiss after 10s |

**Responsiveness Logic:**
- Track operator acknowledgment response time
- If critical alert not acknowledged within 90 seconds:
  - Escalate audio to more urgent pattern
  - Visual alert intensifies (larger overlay, faster pulse)
  - Log escalation event
- Calculate average response time for reporting

#### 3.1.3 Audio Alert System

**Sound Profiles:**
| Severity | Sound File | Duration | Volume |
|----------|------------|----------|--------|
| Critical | alarm_critical.wav | Looping | 100% |
| High | alert_high.wav | 3 cycles | 80% |
| Medium | alert_medium.wav | 1 cycle | 60% |
| Low | notification.wav | 1 cycle | 40% |
| Info | notification_info.wav | 1 cycle | 30% |

**Audio Behavior:**
- Configurable master volume
- Audio can be muted globally
- Each severity can have independent mute setting
- Audio stops on acknowledgment
- Stagger sounds if multiple alerts arrive simultaneously (200ms delay)

#### 3.1.4 Visual Alert System

**Notification Types:**
1. **Alert Overlay** (Critical/High): Full-screen semi-transparent overlay with alert details
2. **Notification Banner** (Medium): Slides down from top-right
3. **System Tray Notification** (Low/Info): Standard Windows toast
4. **Taskbar Flash** (All): Flash window in taskbar when minimized

**Animation Specifications:**
- Critical: Red border pulse, 0.5s interval, 3 cycles
- High: Orange glow, 1s interval, 2 cycles
- Entry animation: Fade + slide from right (200ms)
- Exit animation: Fade out (150ms)

#### 3.1.5 Alert Management

**Features:**
- Acknowledge single alert (button click)
- Acknowledge all alerts (footer button)
- Snooze alert (5/15/30 minutes)
- Clear acknowledged alerts from view
- Search/filter alert log
- Export alert log to CSV

### 3.2 User Interactions and Flows

**Flow 1: Receiving an Alert**
1. TCP message received → Parse and validate
2. Alert Triage Engine classifies
3. Audio notification triggered (if enabled for severity)
4. Visual notification displayed (based on severity)
5. Alert appears in Active Alerts panel
6. Log entry created
7. If Critical → Overlay displayed, audio loops until acknowledged

**Flow 2: Acknowledging an Alert**
1. User clicks "Acknowledge" on alert card OR clicks overlay
2. Audio stops immediately
3. Alert card transitions to "Acknowledged" state
4. Response time logged
5. If all critical/high acknowledged → Overlay dismissed

**Flow 3: Changing Settings**
1. User clicks Settings button
2. Modal dialog opens with tabs: General, Audio, Network, About
3. Changes apply immediately (live preview where applicable)
4. Settings persisted to JSON config file

### 3.3 Data Flow & Processing

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  TCP Listener   │────▶│  Message Parser  │────▶│ Alert Validator │
│  (Async Server) │     │  (JSON Deserialize)│   │ (Schema Check)  │
└─────────────────┘     └──────────────────┘     └────────┬────────┘
                                                           │
                                                           ▼
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  Alert Store    │◀────│  Event Aggregator│◀────│ Alert Objects   │
│  (Observable)   │     │  (Pub/Sub)       │     │ (Domain Models) │
└────────┬────────┘     └──────────────────┘     └─────────────────┘
         │                      │
         ▼                      ▼
┌─────────────────┐     ┌──────────────────┐
│  UI Layer      │     │ Notification     │
│  (WPF Views)   │     │ Service          │
│                │     │ (Audio/Visual)   │
└─────────────────┘     └──────────────────┘
```

**Key Classes/Modules:**

| Module | Responsibility | Public API |
|--------|---------------|------------|
| `AlertListenerService` | TCP socket management, async accept | `StartAsync()`, `StopAsync()`, `OnAlertReceived` event |
| `AlertParser` | JSON deserialization, validation | `Parse(string json)` → `Alert?` |
| `AlertTriageEngine` | Severity classification, escalation | `Triangulate(Alert)` → `NotificationPlan` |
| `AudioNotificationService` | Sound playback, volume control | `Play(Severity)`, `Stop()`, `SetMute(bool)` |
| `VisualNotificationService` | Overlay, banners, tray notifications | `ShowAlert(Alert)`, `Dismiss(int alertId)` |
| `AlertRepository` | In-memory alert storage, querying | `Add()`, `Acknowledge()`, `GetActive()`, `GetHistory()` |
| `SettingsManager` | Persist/load configuration | `Load()`, `Save()`, `CurrentSettings` |
| `EventAggregator` | Cross-component communication | `Publish<T>()`, `Subscribe<T>()` |

### 3.4 Edge Cases

| Scenario | Handling |
|----------|----------|
| Malformed JSON received | Log error, send NACK response, disconnect client |
| Connection flood (100+ simultaneous) | Queue connections, process serially, show warning |
| Audio device unavailable | Fall back to system beep, log warning |
| Alert with missing severity | Default to "Info", log validation warning |
| Duplicate alert ID | Ignore duplicate, log info |
| Port already in use | Show error in UI, suggest alternative port |
| Application crash during alert | Persist alerts to disk every 5s, recover on restart |
| Very long alert message | Truncate display to 500 chars, show full in detail view |
| Network disconnected | Continue local operation, show disconnected status |

---

## 4. Technical Specification

### 4.1 Technology Stack

- **Framework:** .NET 8.0 (Windows)
- **UI:** WPF (Windows Presentation Foundation)
- **Architecture:** MVVM with Dependency Injection
- **DI Container:** Microsoft.Extensions.DependencyInjection
- **Logging:** Serilog with file sink
- **Audio:** NAudio (for WAV playback with looping)
- **JSON:** System.Text.Json (built-in)
- **Configuration:** JSON file (appsettings.json)
- **Threading:** Async/await with TaskScheduler

### 4.2 Project Structure

```
winAlert/
├── winAlert.sln
├── src/
│   └── winAlert/
│       ├── winAlert.csproj
│       ├── App.xaml
│       ├── App.xaml.cs
│       ├── AssemblyInfo.cs
│       ├── Domain/
│       │   ├── Models/
│       │   │   ├── Alert.cs
│       │   │   ├── AlertSeverity.cs
│       │   │   ├── NotificationPlan.cs
│       │   │   └── AppSettings.cs
│       │   └── Events/
│       │       ├── AlertReceivedEvent.cs
│       │       ├── AlertAcknowledgedEvent.cs
│       │       └── SettingsChangedEvent.cs
│       ├── Services/
│       │   ├── Network/
│       │   │   ├── AlertListenerService.cs
│       │   │   └── AlertParser.cs
│       │   ├── Notification/
│       │   │   ├── AlertTriageEngine.cs
│       │   │   ├── AudioNotificationService.cs
│       │   │   └── VisualNotificationService.cs
│       │   ├── Data/
│       │   │   ├── AlertRepository.cs
│       │   │   └── SettingsManager.cs
│       │   └── Core/
│       │       └── EventAggregator.cs
│       ├── ViewModels/
│       │   ├── MainViewModel.cs
│       │   ├── AlertCardViewModel.cs
│       │   ├── SettingsViewModel.cs
│       │   └── ViewModelBase.cs
│       ├── Views/
│       │   ├── MainWindow.xaml
│       │   ├── MainWindow.xaml.cs
│       │   ├── SettingsWindow.xaml
│       │   ├── SettingsWindow.xaml.cs
│       │   ├── AlertDetailWindow.xaml
│       │   └── AlertDetailWindow.xaml.cs
│       ├── Controls/
│       │   ├── AlertCard.xaml
│       │   ├── AlertCard.xaml.cs
│       │   ├── StatusIndicator.xaml
│       │   └── StatusIndicator.xaml.cs
│       ├── Resources/
│       │   ├── Styles/
│       │   │   ├── Colors.xaml
│       │   │   ├── Typography.xaml
│       │   │   ├── Buttons.xaml
│       │   │   └── BaseStyles.xaml
│       │   ├── Sounds/
│       │   │   ├── alarm_critical.wav
│       │   │   ├── alert_high.wav
│       │   │   ├── alert_medium.wav
│       │   │   ├── notification.wav
│       │   │   └── notification_info.wav
│       │   └── Icons/
│       │       └── app.ico
│       └── Converters/
│           ├── SeverityToColorConverter.cs
│           ├── BoolToVisibilityConverter.cs
│           └── TimeAgoConverter.cs
└── README.md
```

### 4.3 Configuration File Schema (appsettings.json)

```json
{
  "network": {
    "port": 8888,
    "maxConcurrentConnections": 50,
    "receiveBufferSize": 8192,
    "connectionTimeoutSeconds": 30
  },
  "audio": {
    "masterEnabled": true,
    "masterVolume": 0.8,
    "severityMutes": {
      "critical": false,
      "high": false,
      "medium": false,
      "low": true,
      "info": true
    }
  },
  "notifications": {
    "criticalRequireAck": true,
    "highRequireAck": true,
    "highAckTimeoutSeconds": 60,
    "mediumAutoCloseSeconds": 30,
    "lowAutoCloseSeconds": 15,
    "infoAutoCloseSeconds": 10,
    "escalationThresholdSeconds": 90,
    "showOverlayForCritical": true,
    "showOverlayForHigh": false,
    "flashTaskbar": true
  },
  "behavior": {
    "startMinimized": false,
    "minimizeToTray": true,
    "startWithWindows": false,
    "alwaysOnTop": false
  },
  "logging": {
    "logLevel": "Information",
    "maxLogFileSizeMB": 10,
    "maxLogFiles": 5
  }
}
```

---

## 5. Acceptance Criteria

### 5.1 Functional Acceptance

| ID | Criterion | Verification Method |
|----|-----------|---------------------|
| F01 | Application starts and displays main window within 3 seconds | Manual timing |
| F02 | TCP listener binds to configured port and accepts connections | Telnet/netcat test |
| F03 | Valid JSON alert message creates visible alert card | Send test message |
| F04 | Invalid JSON is logged and connection closed gracefully | Send malformed data |
| F05 | Critical alert displays full-screen overlay | Trigger critical alert |
| F06 | Critical alert audio loops until acknowledged | Trigger, don't ack |
| F07 | All severity levels play appropriate sounds | Trigger each severity |
| F08 | Acknowledge button stops audio and updates alert state | Click acknowledge |
| F09 | Settings changes persist after restart | Modify and restart |
| F10 | Minimize to tray works correctly | Minimize app |
| F11 | Application handles 50+ simultaneous connections | Load test |
| F12 | Alert log filtering and search works | Use filter controls |

### 5.2 Visual Checkpoints

| ID | Checkpoint |
|----|------------|
| V01 | Dark theme renders correctly with specified colors |
| V02 | Severity colors clearly distinguish alert levels |
| V03 | Alert cards have proper shadows and rounded corners |
| V04 | Pulse animations are smooth for critical alerts |
| V05 | Status indicator reflects actual system state |
| V06 | Settings dialog layout is clean and organized |
| V07 | System tray icon is visible and context menu works |
| V08 | Window can be resized and maintains proper layout |

### 5.3 Error Handling Acceptance

| ID | Criterion |
|----|-----------|
| E01 | Port in use shows user-friendly error with suggestion |
| E02 | Audio device missing falls back to system beep |
| E03 | Application recovers alerts from disk after crash |
| E04 | Network errors are logged with full context |
| E05 | Unhandled exceptions are caught and logged, UI remains stable |

---

## 6. Non-Functional Requirements

### 6.1 Performance

- Memory usage < 150MB under normal operation
- CPU usage < 5% when idle (listening only)
- Alert latency < 100ms from receipt to display
- Support 1000+ alert history entries without degradation

### 6.2 Reliability

- 99.9% uptime target
- Graceful degradation on non-critical failures
- Auto-recovery from transient network issues

### 6.3 Security

- No hardcoded credentials
- Config file uses secure storage for sensitive settings
- Input validation on all incoming network data
- Run with least privileges required

---

*Document Version: 1.0*  
*Created: 2026-03-22*
