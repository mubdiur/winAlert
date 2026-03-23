# winAlert - Requirements for Code Cleanup

## Project Overview

**Purpose**: Native Windows GUI application that listens on a TCP port for JSON alerts and provides visual/audio notifications based on severity.

**Tech Stack**: .NET 8 WPF, MVVM architecture, MaterialDesignInXAML v5.0.0, NAudio for audio, Serilog for logging.

---

## Current State: BROKEN

The application builds but the UI is broken:
- MaterialDesign dark theme is NOT being applied
- DynamicResource references like `SurfaceBrush`, `PrimaryButtonStyle`, `AlertTitleTextStyle` do not exist
- Scrollbars look like default Windows (not Material Design)
- Window is not properly styled
- AlertCard uses styles that don't exist in any resource dictionary

---

## What Needs to Be Fixed

### 1. THEME/STYLING (CRITICAL - Everything below depends on this)

**Problem**: App.xaml is empty (no ResourceDictionary for MaterialDesign). AlertCard.xaml and MainWindow.xaml reference dynamic resources that don't exist.

**Files needing fixes**:
- `App.xaml` - Must include MaterialDesign theme resources
- `App.xaml.cs` - Must configure MaterialDesign with proper dark theme
- `MainWindow.xaml` - Remove broken DynamicResource references, use standard WPF styling or proper MaterialDesign bindings
- `AlertCard.xaml` - Remove broken DynamicResource references, use standard WPF styling or proper MaterialDesign bindings

**Requirements**:
- Dark theme MUST be applied (BaseTheme="Dark" in MaterialDesign)
- All UI elements must have consistent dark styling
- ScrollViewer scrollbars must be Material Design styled (use `ScrollBarThickness` and `ScrollViewer.NoIndicator` style or similar)
- Color palette should be:
  - Background: Dark gray/charcoal (#1E1E1E or similar)
  - Surface/Cards: Slightly lighter (#2D2D2D or similar)
  - Primary accent: Teal or Blue (Material Design teal)
  - Text: White/Light gray
  - Severity colors: Critical=Red, High=Orange, Medium=Yellow, Low=Blue, Info=Gray

### 2. RESOURCE DICTIONARY APPROACH

**Option A (Recommended)**: Remove all custom DynamicResource references and use:
- Standard WPF properties (Foreground, Background, etc.)
- OR MaterialDesign built-in brushes (e.g., `MaterialDesign.Brush.Primary`, `MaterialDesign.Brush.Card.Background`)

**Option B**: Create a custom `ResourceDictionary.xaml` that defines all the custom brushes and styles referenced in the XAML files.

If Option B, the resource dictionary must be merged into App.xaml and must define:
- `SurfaceBrush` (#2D2D2D)
- `PrimaryButtonStyle` (Material Design button style)
- `SecondaryTextStyle` (Gray text style)
- `AlertTitleTextStyle` (Bold white text style)
- `BodyTextStyle` (Regular text style)
- `SuccessBrush` (Green color)
- `TextSecondaryBrush` (Gray color)

### 3. SPECIFIC UI REQUIREMENTS

#### MainWindow.xaml Layout
```
┌─────────────────────────────────────────────────────────────────┐
│ HEADER: [StatusIndicator] [StatusText] [Port: ___] [Start/Stop] [Settings] │
├──────────────────────────────┬──────────────────────────────────┤
│ ACTIVE ALERTS (380px wide)    │ ALERT HISTORY                     │
│ ┌──────────────────────────┐  │ ┌────────────────────────────────┐│
│ │ [Critical: 0] [High: 0]  │  │ │ [Filter TextBox] [Clear]      ││
│ │   [Medium: 0]            │  │ ├────────────────────────────────┤│
│ ├──────────────────────────┤  │ │                                ││
│ │ AlertCard (sorted by     │  │ │  ScrollViewer with AlertCards   ││
│ │ severity, critical first)│  │ │  sorted by time (newest first) ││
│ │ AlertCard                │  │ │                                ││
│ │ AlertCard                │  │ │                                ││
│ │ ...                      │  │ │                                ││
│ └──────────────────────────┘  │ └────────────────────────────────┘│
├──────────────────────────────┴──────────────────────────────────┤
│ FOOTER: [Audio: ☑]                    [Acknowledge All] [Clear] │
└─────────────────────────────────────────────────────────────────┘
```

#### AlertCard Control
- Left edge: 4px colored bar indicating severity
- Background: Surface color (#2D2D2D)
- Corner radius: 8px
- Subtle shadow
- Content:
  - Header: Severity badge (colored pill) + Source text
  - Acknowledge button (top right) - visible when NOT acknowledged
  - "ACKNOWLEDGED" text (top right) - visible when acknowledged
  - Title (bold, white)
  - Message preview (truncated, gray text)
- Pulse animation: Unacknowledged alerts have a subtle border pulse animation

#### StatusIndicator Control
- 12px circle that pulses when listening
- Green when listening, gray when stopped
- Glow ring animation when listening

### 4. VISUAL POLISH

- All borders/cards should have proper corner radius (8px for cards, 4px for buttons)
- Consistent padding (12-16px inside cards, 8px for borders)
- Drop shadows on cards (subtle, 8px blur, 20% opacity)
- Proper spacing between elements (8px margins, 12px between sections)
- Window minimum size: 800x500, default: 1100x700

---

## File Structure (DO NOT CHANGE)

```
src/winAlert/
├── winAlert.csproj          (DO NOT MODIFY - already has MaterialDesignThemes v5.0.0)
├── App.xaml                 (FIX: Add MaterialDesign resources)
├── App.xaml.cs              (FIX: Configure MaterialDesign dark theme)
├── Domain/
│   ├── Models/
│   │   ├── Alert.cs
│   │   ├── AlertSeverity.cs
│   │   ├── NotificationPlan.cs
│   │   └── AppSettings.cs
│   └── Events/
│       └── [Event classes]
├── Services/
│   ├── Core/EventAggregator.cs
│   ├── Network/
│   │   ├── AlertListenerService.cs
│   │   └── AlertParser.cs
│   ├── Notification/
│   │   ├── AlertTriageEngine.cs
│   │   ├── AudioNotificationService.cs
│   │   └── VisualNotificationService.cs
│   └── Data/
│       ├── AlertRepository.cs
│       └── SettingsManager.cs
├── ViewModels/
│   ├── MainViewModel.cs      (DO NOT MODIFY - working correctly)
│   ├── AlertCardViewModel.cs
│   └── SettingsViewModel.cs
├── Views/
│   ├── MainWindow.xaml       (FIX: Remove broken resources, use proper styling)
│   ├── AlertDetailWindow.xaml
│   └── SettingsWindow.xaml
├── Controls/
│   ├── AlertCard.xaml       (FIX: Remove broken resources, use proper styling)
│   └── StatusIndicator.xaml (may be OK - check DynamicResource usage)
└── Converters/
    └── Converters.cs        (DO NOT MODIFY - working correctly)
```

---

## What NOT to Change

1. **Domain Models** - Alert.cs, AlertSeverity.cs, NotificationPlan.cs, AppSettings.cs are working
2. **Services** - All services (AlertListenerService, AlertParser, AudioNotificationService, etc.) are working
3. **ViewModels** - MainViewModel and logic are working correctly
4. **Converters** - All converters in Converters.cs work correctly
5. **winAlert.csproj** - Package references are correct

---

## MaterialDesign v5.0.0 Setup (DO THIS IN App.xaml.cs)

```csharp
// In OnStartup, after Log.Logger configuration:
var paletteHelper = new MaterialDesignThemes.Wpf.PaletteHelper();
var theme = paletteHelper.GetTheme();
theme.SetBaseTheme(MaterialDesignThemes.Wpf.BaseTheme.Dark);
theme.PrimaryLight = new MaterialDesignColors.ColorPair(Colors.Teal, Black);
theme.PrimaryMid = new MaterialDesignColors.ColorPair(Colors.Teal, White);
theme.PrimaryDark = new MaterialDesignColors.ColorPair(Colors.Teal, Black);
paletteHelper.SetTheme(theme);
```

Or in App.xaml:
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

---

## Testing Checklist

After fixes, verify:

1. **Build**: `dotnet build` succeeds with no errors
2. **Startup**: App launches with dark theme applied
3. **Visual**:
   - Window background is dark
   - Cards have dark surface background
   - Text is white/light colored
   - Scrollbars are styled (not default Windows)
   - Severity colors are correct (Critical=Red, etc.)
4. **Functionality**:
   - Start button starts listener
   - Alerts appear in Active Alerts list when sent
   - Acknowledge button works
   - Audio toggle works
5. **Alerts can be tested with**:
   ```powershell
   $msg = '{"id":"test-1","severity":"critical","source":"test","title":"Critical Alert","message":"Test message","timestamp":"2026-03-22T10:00:00Z"}'
   $stream = [System.Net.Sockets.TcpClient]::new("localhost", 8888).GetStream()
   $writer = [System.IO.StreamWriter]::new($stream)
   $writer.Write($msg)
   $writer.Flush()
   ```

---

## Success Criteria

- [ ] Application builds without errors
- [ ] Dark theme is properly applied (not light theme)
- [ ] All UI elements visible against dark background
- [ ] Scrollbars are properly styled
- [ ] AlertCards display with proper severity coloring
- [ ] StatusIndicator animates when listening
- [ ] Start/Stop listener works
- [ ] Alerts received and displayed correctly
- [ ] Acknowledge functionality works
