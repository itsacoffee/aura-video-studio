; Inno Setup Script for Aura Video Studio
; This script creates a traditional Windows installer (EXE) for users who prefer
; a familiar installation experience over MSIX.

#define MyAppName "Aura Video Studio"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Aura Video Studio"
#define MyAppURL "https://github.com/Coffee285/aura-video-studio"
#define MyAppExeName "AuraVideoStudio.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
AppId={{8F2D5A1C-7B3E-4D9F-A2C6-5E8B9D4F3A1C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\..\LICENSE
OutputDir=..\..\artifacts\windows\exe
OutputBaseFilename=AuraVideoStudio_Setup
SetupIconFile=..\..\assets\icon.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main application binaries (WPF shell + Aura.Api)
; Source: "..\..\Aura.Host.Win.Wpf\bin\Release\net8.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; For now, we'll package the API
Source: "..\..\Aura.Api\bin\Release\net8.0\*"; DestDir: "{app}\Api"; Flags: ignoreversion recursesubdirs createallsubdirs

; Web UI build output
Source: "..\..\Aura.Web\dist\*"; DestDir: "{app}\Web"; Flags: ignoreversion recursesubdirs createallsubdirs

; FFmpeg binaries
Source: "..\..\scripts\ffmpeg\ffmpeg.exe"; DestDir: "{app}\ffmpeg"; Flags: ignoreversion
Source: "..\..\scripts\ffmpeg\ffprobe.exe"; DestDir: "{app}\ffmpeg"; Flags: ignoreversion

; Configuration and documentation
Source: "..\..\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "..\..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\Aura"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  // Check for .NET 8 runtime
  if not RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost\8.0') then
  begin
    if MsgBox('.NET 8 Runtime is required but not installed. Would you like to download it now?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, Result);
      Result := False;
    end
    else
      Result := False;
  end;
end;
