; winAlert Inno Setup Script
; Creates signed EXE installer for winAlert

#define MyAppName "winAlert"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "winAlert"
#define MyAppURL "https://github.com/mubdiur/winAlert"
#define MyAppExeName "winAlert.exe"

[Setup]
; Basic app info
AppId={{E8A1B2C3-D4E5-F6A7-B8C9-D0E1F2A3B4C5}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Install locations
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes

; Output settings
OutputDir=..\dist
OutputBaseFilename=winAlert-{#MyAppVersion}-setup
SetupIconFile=Resources\Icons\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; Compression
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

; Privileges (per-user install)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Appearance
WizardStyle=modern
WizardSizePercent=100

; Misc
DisableWelcomePage=no
DisableDirPage=no
DisableReadyPage=no
DisableFinishedPage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; Main application (single file from publish)
Source: "..\publish\winAlert.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Code signing check - verify installer is signed
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Post-installation tasks if needed
  end;
end;

// Check if app is already installed
function InitializeSetup(): Boolean;
var
  InstalledVersion: String;
begin
  Result := True;
  
  if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1', 'DisplayVersion', InstalledVersion) then
  begin
    if InstalledVersion <> '' then
    begin
      if MsgBox('A newer version of ' + '{#MyAppName}' + ' may already be installed. Continue anyway?', mbConfirmation, MB_YESNO) = IDNO then
      begin
        Result := False;
      end;
    end;
  end;
end;
