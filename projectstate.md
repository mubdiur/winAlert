# winAlert - Project State

**Last Updated**: 2026-03-22  
**Build Status**: ✅ SUCCESS (0 errors, 0 warnings)  
**Run Status**: App launches but UI styling is broken

---

## Quick Start

```powershell
# Navigate to project
cd C:\Users\mubdiur\Documents\GitHub\winAlert

# Build
dotnet build

# Run
dotnet run --project src/winAlert

# Or run the built exe directly
.\src\winAlert\bin\Debug\net8.0-windows\winAlert.exe
```

### Test Alerts

```powershell
# From project root (use absolute path if running from elsewhere)
.\test-alerts.ps1

# Manual test
$msg = '{"id":"test-1","severity":"critical","source":"test","title":"Critical Alert","message":"Test message","timestamp":"2026-03-22T10:00:00Z"}'
$stream = [System.Net.Sockets.TcpClient]::new("localhost", 8888).GetStream()
$writer = [System.IO.StreamWriter]::new($stream)
$writer.Write($msg)
$writer.Flush()
```

---

## Project Structure

```
winAlert/
├── winAlert.sln
├── README.md
├── SPEC.md                    # Full specification document
├── requirements.md            # Cleanup requirements for AI
├── projectstate.md           # This file
├── test-alerts.ps1           # Test script for sending alerts
├── .gitignore
└── src/winAlert/
    ├── winAlert.csproj       # .NET 8 WPF, MaterialDesignThemes v5.0.0
    ├── App.xaml              # EMPTY - needs MaterialDesign resources
    ├── App.xaml.cs           # DI setup, logging, NO MaterialDesign config
    │
    ├── Domain/
    │   ├── Models/
    │   │   ├── Alert.cs              ✅ WORKING
    │   │   ├── AlertSeverity.cs      ✅ WORKING
    │   │   ├── NotificationPlan.cs   ✅ WORKING
    │   │   └── AppSettings.cs        ✅ WORKING
    │   └── Events/
    │       ├── AlertReceivedEvent.cs
    │       ├── AlertAcknowledgedEvent.cs
    │       └── ListenerStatusChangedEvent.cs
    │
    ├── Services/
    │   ├── Core/
    │   │   └── EventAggregator.cs    ✅ WORKING
    │   ├── Network/
    │   │   ├── AlertListenerService.cs   ✅ WORKING (TCP listener)
    │   │   └── AlertParser.cs            ✅ WORKING
    │   ├── Notification/
    │   │   ├── AlertTriageEngine.cs      ✅ WORKING
    │   │   ├── AudioNotificationService.cs ✅ WORKING (NAudio)
    │   │   └── VisualNotificationService.cs ✅ WORKING
    │   └── Data/
    │       ├── AlertRepository.cs        ✅ WORKING
    │       └── SettingsManager.cs        ✅ WORKING
    │
    ├── ViewModels/
    │   ├── ViewModelBase.cs              ✅ WORKING
    │   ├── MainViewModel.cs              ✅ WORKING
    │   ├── AlertCardViewModel.cs         ✅ WORKING
    │   ├── SettingsViewModel.cs          ✅ WORKING
    │   └── RelayCommand.cs               ✅ WORKING
    │
    ├── Views/
    │   ├── MainWindow.xaml               ❌ BROKEN (missing styles)
    │   ├── AlertDetailWindow.xaml        ❌ BROKEN (missing styles)
    │   └── SettingsWindow.xaml            ❌ BROKEN (missing styles)
    │
    ├── Controls/
    │   ├── AlertCard.xaml                ❌ BROKEN (missing styles)
    │   └── StatusIndicator.xaml          ⚠️ PARTIAL (uses SystemColors)
    │
    └── Converters/
        └── Converters.cs                 ✅ WORKING (7 converters)
```

---

## What's Working

| Component | Status | Notes |
|-----------|--------|-------|
| **Build** | ✅ | `dotnet build` succeeds |
| **Domain Models** | ✅ | Alert, AlertSeverity, NotificationPlan, AppSettings |
| **Services** | ✅ | All services (Listener, Parser, Audio, Visual, Repository, Settings) |
| **ViewModels** | ✅ | All VMs with commands and event handling |
| **Converters** | ✅ | All 7 converters work correctly |
| **Logging** | ✅ | Serilog configured, logs to `%LOCALAPPDATA%\winAlert\logs\` |
| **DI Container** | ✅ | Microsoft.Extensions.DependencyInjection |

---

## What's Broken (THE PROBLEM)

### 1. **MaterialDesign Theme NOT Applied**
- `MaterialDesignThemes v5.0.0` is installed in csproj
- **BUT** there is NO theme configuration anywhere
- App.xaml is empty (no ResourceDictionary, no MergedDictionaries)
- App.xaml.cs has NO PaletteHelper, no theme setup

### 2. **53 Missing DynamicResource References**

These styles/brushes are referenced but **NEVER DEFINED**:

| Missing Resource | Used In | Problem |
|-----------------|---------|---------|
| `SurfaceBrush` | AlertCard.xaml | Background color not defined |
| `PrimaryButtonStyle` | AlertCard.xaml, AlertDetailWindow.xaml, SettingsWindow.xaml | Button style not defined |
| `SecondaryTextStyle` | AlertCard.xaml, AlertDetailWindow.xaml, SettingsWindow.xaml | Text style not defined |
| `AlertTitleTextStyle` | AlertCard.xaml | Title style not defined |
| `BodyTextStyle` | AlertCard.xaml, AlertDetailWindow.xaml, SettingsWindow.xaml | Text style not defined |
| `SuccessBrush` | AlertCard.xaml | Color not defined |
| `TextSecondaryBrush` | AlertCard.xaml | Color not defined |
| `BaseWindowStyle` | AlertDetailWindow.xaml, SettingsWindow.xaml | Window style not defined |
| `TitleTextStyle` | AlertDetailWindow.xaml, SettingsWindow.xaml | Text style not defined |
| `CardStyle` | AlertDetailWindow.xaml, SettingsWindow.xaml | Border style not defined |
| `HeaderTextStyle` | SettingsWindow.xaml | Text style not defined |
| `BaseTextBoxStyle` | SettingsWindow.xaml | TextBox style not defined |
| `ToggleSwitchStyle` | SettingsWindow.xaml | Toggle style not defined |
| `SecondaryButtonStyle` | AlertDetailWindow.xaml, SettingsWindow.xaml | Button style not defined |

### 3. **Files with Broken Styles**

| File | Issue Count |
|------|-------------|
| `AlertCard.xaml` | 8 missing resources |
| `AlertDetailWindow.xaml` | 13 missing resources |
| `SettingsWindow.xaml` | 23 missing resources |
| `MainWindow.xaml` | Uses SystemColors (valid) |

---

## How to Run the App

```powershell
# Option 1: dotnet run
cd C:\Users\mubdiur\Documents\GitHub\winAlert
dotnet run --project src/winAlert

# Option 2: Run compiled exe
.\src\winAlert\bin\Debug\net8.0-windows\winAlert.exe

# Option 3: From Windows Explorer
# Double-click: winAlert.exe in bin\Debug\net8.0-windows\
```

**Port**: 8888 (default, configurable in UI)  
**Logs**: `%LOCALAPPDATA%\winAlert\logs\`  
**Settings**: `%LOCALAPPDATA%\winAlert\settings.json`

---

## How to Test Alerts

### Using test-alerts.ps1:
```powershell
cd C:\Users\mubdiur\Documents\GitHub\winAlert
.\test-alerts.ps1
```

### Manual TCP test:
```powershell
$msg = '{"id":"test-1","severity":"critical","source":"test","title":"Critical Alert","message":"Server down!","timestamp":"2026-03-22T10:00:00Z"}'
$client = [System.Net.Sockets.TcpClient]::new("localhost", 8888)
$stream = $client.GetStream()
$writer = [System.IO.StreamWriter]::new($stream)
$writer.Write($msg)
$writer.Flush()
$writer.Dispose()
$client.Dispose()
```

### Alert JSON format:
```json
{
  "id": "uuid-string",
  "severity": "critical|high|medium|low|info",
  "source": "monitoring-system",
  "title": "Alert Title",
  "message": "Detailed alert message...",
  "timestamp": "2026-03-22T10:30:00Z",
  "metadata": { "key": "value" },
  "requireAcknowledgment": true,
  "autoCloseSeconds": 0
}
```

---

## Fix Requirements (from requirements.md)

### High Priority (must fix):

1. **App.xaml** - Add MaterialDesign BundledTheme with Dark BaseTheme:
```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <MaterialDesignThemes.Wpf.BundledTheme BaseTheme="Dark" PrimaryColor="Teal" SecondaryColor="Amber" />
            <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application>
```

2. **App.xaml.cs** - Optionally add programmatic theme setup after line 30:
```csharp
var paletteHelper = new MaterialDesignThemes.Wpf.PaletteHelper();
var theme = paletteHelper.GetTheme();
theme.SetBaseTheme(MaterialDesignThemes.Wpf.BaseTheme.Dark);
paletteHelper.SetTheme(theme);
```

3. **Fix AlertCard.xaml** - Replace missing DynamicResources with:
   - `SurfaceBrush` → Use `MaterialDesign.Brush.Card.Background` or solid color `#2D2D2D`
   - `PrimaryButtonStyle` → Use MaterialDesign button style
   - All text styles → Use explicit Foreground colors instead

4. **Fix AlertDetailWindow.xaml** - Same approach as AlertCard

5. **Fix SettingsWindow.xaml** - Same approach

### Low Priority (nice to have):
- Custom ResourceDictionary.xaml to define all missing styles in one place

---

## MaterialDesign v5.0.0 Documentation

**NuGet**: https://www.nuget.org/packages/MaterialDesignThemes/5.0.0

**Setup via XAML** (App.xaml):
```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <MaterialDesignThemes.Wpf.BundledTheme
                BaseTheme="Dark"
                PrimaryColor="Teal"
                SecondaryColor="Amber" />
            <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application>
```

**Setup via Code** (App.xaml.cs):
```csharp
var paletteHelper = new MaterialDesignThemes.Wpf.PaletteHelper();
var theme = paletteHelper.GetTheme();
theme.SetBaseTheme(MaterialDesignThemes.Wpf.BaseTheme.Dark);
theme.PrimaryMid = new MaterialDesignColors.ColorPair(Colors.Teal, Brushes.White);
paletteHelper.SetTheme(theme);
```

---

## Known Issues

1. **UI shows light theme or unstyled elements** - Expected, MaterialDesign not configured
2. **Alert cards may not display correctly** - Missing styles
3. **Settings window may look broken** - Missing styles
4. **Scrollbars are default Windows style** - MaterialDesign scrollbar styles not loaded

---

## Files to Read for Context

| File | Purpose |
|------|---------|
| `SPEC.md` | Full feature specification |
| `requirements.md` | Detailed cleanup requirements |
| `App.xaml` | Needs MaterialDesign setup |
| `App.xaml.cs` | DI configuration |
| `AlertCard.xaml` | Most broken UI component |
| `src/winAlert/Views/MainWindow.xaml` | Main layout |
