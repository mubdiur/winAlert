# Plan: Debug Alert Pipeline

## TL;DR

> **Quick Summary**: Fix alert pipeline by adding console window for debug output, then identify and fix where alerts are being lost.
> 
> **Deliverables**:
> - Console window visible for debug output
> - Working alert reception and display in UI
> 
> **Estimated Effort**: Quick
> **Parallel Execution**: NO - sequential debugging
> **Critical Path**: Add console → Run app → See debug output → Fix issue

---

## Context

### Original Request
User reports alerts not showing in UI and no audio playing. Test alerts are sent successfully via TCP but not appearing in application.

### Interview Summary
**Key Discussions**:
- Comprehensive logging was added to AlertParser, AlertListenerService, EventAggregator, MainViewModel, AudioNotificationService
- Debug.WriteLine statements added to trace pipeline
- Problem: Debug.WriteLine output not visible without console window

**Research Findings**:
- AlertParser had ZERO logging (silently swallowing parse errors)
- AudioNotificationService has silent early returns
- No .wav audio files in project (falls back to system beep)
- EventAggregator was using weak references (FIXED - now uses strong references)

### Metis Review
**Identified Gaps** (addressed):
- No way to see debug output in WPF app without debugger attached
- Need AllocConsole to make Debug.WriteLine visible

---

## Work Objectives

### Core Objective
Make alerts appear in UI and play audio notifications when received via TCP.

### Concrete Deliverables
- Console window showing debug output when app runs
- Visible debug trace of alert pipeline
- Working alert reception in UI

### Definition of Done
- [ ] Console window opens with app
- [ ] Debug output visible in console
- [ ] Alerts appear in Active Alerts panel when sent
- [ ] Audio plays (or falls back to system beep)

---

## TODOs

- [ ] 1. **Add Console Window for Debug Output**

  **What to do**:
  - Add `AllocConsole` P/Invoke to App.xaml.cs
  - Call `AllocConsole()` in OnStartup before ConfigureLogging()
  - Add early Console.WriteLine to verify console works

  **Files**: `src/winAlert/App.xaml.cs`

  **Code to add**:
  ```csharp
  using System.Runtime.InteropServices;
  
  // Inside App class:
  [DllImport("kernel32.dll")]
  private static extern bool AllocConsole();
  
  // In OnStartup, after base.OnStartup(e):
  AllocConsole();
  Console.WriteLine("[STARTUP] Console allocated for debug output");
  Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
  ```

- [ ] 2. **Run App and Observe Debug Output**

  **What to do**:
  - Kill any running winAlert processes
  - Build and run app
  - Send test alerts via test-alerts.ps1
  - Watch console for debug output
  - Note exactly where pipeline stops

- [ ] 3. **Fix Identified Pipeline Break**

  **What to do**:
  - Based on debug output, identify where alerts are lost
  - Fix the specific issue (could be: parser, event aggregator, main thread dispatch, etc.)
  - Re-test until alerts appear in UI

- [ ] 4. **Add Console Output Redirect for Debug.WriteLine**

  **What to do**:
  - Create a TextWriter that redirects Debug.WriteLine to Console.Out
  - Add Debug.Listeners.Add(new ConsoleTraceListener()) in App.xaml.cs

---

## Success Criteria

### Verification Commands
```powershell
# Run app
dotnet run --project src/winAlert

# In another terminal, send test alerts
.\test-alerts.ps1

# Console should show:
# [STARTUP] Console allocated for debug output
# [LISTENER] Received X bytes...
# [PARSER] Attempting to parse JSON...
# [PARSER] Alert parsed...
# [EVENTAGG] Publishing AlertReceivedEvent...
# [MAINVM] Processing alert...
# [MAINVM] Added to ActiveAlerts...
```

### Final Checklist
- [ ] Console window opens with app
- [ ] Debug output shows alert pipeline progress
- [ ] Alerts visible in Active Alerts panel
- [ ] Audio notification plays (or falls back to beep)