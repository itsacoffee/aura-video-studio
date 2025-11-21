; NSIS Custom Installer Script for Aura Video Studio
; This script adds custom installation steps with Windows 11 compatibility

; Request admin rights for installation
RequestExecutionLevel admin

!macro customInstall
  ; ========================================
  ; .NET 8 Runtime Detection
  ; ========================================
  DetailPrint "Checking for .NET 8 Runtime..."
  nsExec::ExecToStack 'powershell -NoProfile -ExecutionPolicy Bypass -Command "try { $runtimes = dotnet --list-runtimes 2>&1; if ($runtimes -match \"Microsoft\\.NETCore\\.App 8\\.\") { exit 0 } else { exit 1 } } catch { exit 1 }"'
  Pop $0 ; Return code
  Pop $1 ; Output (if any)
  
  ${If} $0 != "0"
    DetailPrint ".NET 8 Runtime not detected"
    MessageBox MB_YESNO|MB_ICONEXCLAMATION ".NET 8 Runtime Required$\n$\nAura Video Studio requires .NET 8 Runtime to function properly.$\n$\nWithout it, the application will not start.$\n$\nWould you like to download .NET 8 Runtime now?$\n$\n(Recommended: Download and install before continuing)" IDYES download_dotnet IDNO skip_dotnet
    download_dotnet:
      DetailPrint "Opening .NET 8 download page..."
      ExecShell "open" "https://dotnet.microsoft.com/download/dotnet/8.0/runtime"
      MessageBox MB_OK|MB_ICONINFORMATION "Please download and install .NET 8 Runtime (or ASP.NET Core 8 Runtime)$\n$\nAfter installation, you can continue using Aura Video Studio.$\n$\nClick OK to continue installation."
    skip_dotnet:
      DetailPrint "User chose to skip .NET 8 Runtime installation"
  ${Else}
    DetailPrint ".NET 8 Runtime detected - OK"
  ${EndIf}
  
  ; ========================================
  ; Windows 11 Compatibility Registry
  ; ========================================
  DetailPrint "Configuring Windows registry..."
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "DisplayName" "Aura Video Studio"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "Publisher" "${COMPANY_NAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "DisplayIcon" "$INSTDIR\${APP_EXECUTABLE_FILENAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "HelpLink" "https://github.com/coffee285/aura-video-studio"
  WriteRegDWord HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "NoModify" 1
  WriteRegDWord HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "NoRepair" 1
  
  ; Store installation date
  System::Call 'kernel32::GetSystemTime(i)i.r0'
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "InstallDate" "$0"
  
  ; ========================================
  ; File Associations (Both HKLM and HKCU for compatibility)
  ; ========================================
  DetailPrint "Registering file associations..."
  ; Machine-level associations (requires admin)
  WriteRegStr HKLM "Software\Classes\.aura" "" "AuraVideoStudio.Project"
  WriteRegStr HKLM "Software\Classes\.avsproj" "" "AuraVideoStudio.Project"
  WriteRegStr HKLM "Software\Classes\AuraVideoStudio.Project" "" "Aura Video Studio Project"
  WriteRegStr HKLM "Software\Classes\AuraVideoStudio.Project\DefaultIcon" "" "$INSTDIR\${APP_EXECUTABLE_FILENAME},0"
  WriteRegStr HKLM "Software\Classes\AuraVideoStudio.Project\shell\open\command" "" '"$INSTDIR\${APP_EXECUTABLE_FILENAME}" "%1"'
  
  ; User-level associations (fallback)
  WriteRegStr HKCU "Software\Classes\.aura" "" "AuraVideoStudio.Project"
  WriteRegStr HKCU "Software\Classes\.avsproj" "" "AuraVideoStudio.Project"
  WriteRegStr HKCU "Software\Classes\AuraVideoStudio.Project" "" "Aura Video Studio Project"
  WriteRegStr HKCU "Software\Classes\AuraVideoStudio.Project\DefaultIcon" "" "$INSTDIR\${APP_EXECUTABLE_FILENAME},0"
  WriteRegStr HKCU "Software\Classes\AuraVideoStudio.Project\shell\open\command" "" '"$INSTDIR\${APP_EXECUTABLE_FILENAME}" "%1"'
  
  ; ========================================
  ; Windows Firewall Rule
  ; ========================================
  DetailPrint "Configuring Windows Firewall..."
  
  ; Add firewall rule for main Electron executable
  nsExec::ExecToLog 'netsh advfirewall firewall add rule name="Aura Video Studio" dir=in action=allow program="$INSTDIR\${APP_EXECUTABLE_FILENAME}" enable=yes profile=any'
  Pop $0
  ${If} $0 == "0"
    DetailPrint "Windows Firewall rule added for main app successfully"
  ${Else}
    DetailPrint "Warning: Could not add Windows Firewall rule for main app (may require manual configuration)"
  ${EndIf}
  
  ; Add firewall rule for backend executable (Aura.Api.exe)
  DetailPrint "Adding Windows Firewall exception for backend service..."
  nsExec::ExecToLog 'netsh advfirewall firewall add rule name="Aura Video Studio Backend" dir=in action=allow program="$INSTDIR\resources\backend\win-x64\Aura.Api.exe" enable=yes profile=private,domain'
  Pop $0
  ${If} $0 == "0"
    DetailPrint "Windows Firewall rule added for backend successfully"
  ${Else}
    DetailPrint "Warning: Could not add Windows Firewall rule for backend (may require manual configuration)"
  ${EndIf}
  
  ; Also add for public profile (optional, requires admin)
  nsExec::ExecToLog 'netsh advfirewall firewall add rule name="Aura Video Studio Backend (Public)" dir=in action=allow program="$INSTDIR\resources\backend\win-x64\Aura.Api.exe" enable=yes profile=public'
  Pop $0
  ${If} $0 == "0"
    DetailPrint "Windows Firewall rule added for backend (public profile)"
  ${EndIf}
  
  ; ========================================
  ; Windows Defender Exclusions (Optional)
  ; ========================================
  DetailPrint "Adding Windows Defender exclusion..."
  nsExec::ExecToLog 'powershell -NoProfile -ExecutionPolicy Bypass -Command "Add-MpPreference -ExclusionPath \"$INSTDIR\" -ErrorAction SilentlyContinue"'
  
  ; ========================================
  ; Visual C++ Redistributable Check
  ; ========================================
  DetailPrint "Checking for Visual C++ Redistributable..."
  ; Check if Visual C++ 2015-2022 Redistributable is installed (needed for .NET)
  ReadRegStr $0 HKLM "SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" "Installed"
  ${If} $0 != "1"
    DetailPrint "Visual C++ Redistributable not detected"
    MessageBox MB_YESNO "This application requires Visual C++ Redistributable. Would you like to download it now?" IDYES download_vcredist IDNO skip_vcredist
    download_vcredist:
      ExecShell "open" "https://aka.ms/vs/17/release/vc_redist.x64.exe"
    skip_vcredist:
  ${Else}
    DetailPrint "Visual C++ Redistributable detected - OK"
  ${EndIf}
  
  ; ========================================
  ; Create AppData Directories
  ; ========================================
  DetailPrint "Creating user data directories..."
  SetShellVarContext current
  CreateDirectory "$LOCALAPPDATA\aura-video-studio"
  CreateDirectory "$LOCALAPPDATA\aura-video-studio\logs"
  CreateDirectory "$LOCALAPPDATA\aura-video-studio\cache"
  DetailPrint "User data directory: $LOCALAPPDATA\aura-video-studio"
  
  ; Refresh shell icon cache
  DetailPrint "Refreshing shell..."
  System::Call 'shell32.dll::SHChangeNotify(i, i, i, i) v (0x08000000, 0, 0, 0)'
  
  DetailPrint "Installation completed successfully!"
!macroend

!macro customUnInstall
  DetailPrint "Starting uninstallation cleanup..."
  
  ; ========================================
  ; Stop any running instances
  ; ========================================
  DetailPrint "Stopping any running instances..."
  nsExec::ExecToLog 'taskkill /F /IM "Aura Video Studio.exe" /T'
  Sleep 2000
  
  ; ========================================
  ; Remove File Associations
  ; ========================================
  DetailPrint "Removing file associations..."
  ; Machine-level associations
  DeleteRegKey HKLM "Software\Classes\.aura"
  DeleteRegKey HKLM "Software\Classes\.avsproj"
  DeleteRegKey HKLM "Software\Classes\AuraVideoStudio.Project"
  
  ; User-level associations
  DeleteRegKey HKCU "Software\Classes\.aura"
  DeleteRegKey HKCU "Software\Classes\.avsproj"
  DeleteRegKey HKCU "Software\Classes\AuraVideoStudio.Project"
  
  ; ========================================
  ; Remove Windows Firewall Rule
  ; ========================================
  DetailPrint "Removing Windows Firewall rules..."
  nsExec::ExecToLog 'netsh advfirewall firewall delete rule name="Aura Video Studio"'
  nsExec::ExecToLog 'netsh advfirewall firewall delete rule name="Aura Video Studio Backend"'
  nsExec::ExecToLog 'netsh advfirewall firewall delete rule name="Aura Video Studio Backend (Public)"'
  
  ; ========================================
  ; Remove Windows 11 Uninstall Registry
  ; ========================================
  DetailPrint "Removing registry entries..."
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}"
  
  ; ========================================
  ; Remove Windows Defender Exclusions
  ; ========================================
  DetailPrint "Removing Windows Defender exclusion..."
  nsExec::ExecToLog 'powershell -NoProfile -ExecutionPolicy Bypass -Command "Remove-MpPreference -ExclusionPath \"$INSTDIR\" -ErrorAction SilentlyContinue"'
  
  ; ========================================
  ; Clean AppData (Optional - Ask User)
  ; ========================================
  SetShellVarContext current
  MessageBox MB_YESNO "Would you like to remove all user data, settings, and cached files?$\n$\nThis will delete:$\n- Application settings$\n- Cached files$\n- Logs$\n$\nLocation: $LOCALAPPDATA\aura-video-studio$\n$\n(Your video projects in Documents will NOT be deleted)" IDYES remove_appdata IDNO keep_appdata
  
  remove_appdata:
    DetailPrint "Removing user data from $LOCALAPPDATA\aura-video-studio..."
    RMDir /r "$LOCALAPPDATA\aura-video-studio"
    DetailPrint "User data removed"
    Goto appdata_done
  
  keep_appdata:
    DetailPrint "Keeping user data at $LOCALAPPDATA\aura-video-studio"
  
  appdata_done:
  
  ; ========================================
  ; Clean Temp Files
  ; ========================================
  DetailPrint "Cleaning temporary files..."
  RMDir /r "$TEMP\aura-video-studio"
  
  ; ========================================
  ; Remove Desktop and Start Menu Shortcuts
  ; ========================================
  DetailPrint "Removing shortcuts..."
  Delete "$DESKTOP\Aura Video Studio.lnk"
  Delete "$SMPROGRAMS\Aura Video Studio.lnk"
  RMDir "$SMPROGRAMS\Aura Video Studio"
  
  ; Refresh shell icon cache
  DetailPrint "Refreshing shell..."
  System::Call 'shell32.dll::SHChangeNotify(i, i, i, i) v (0x08000000, 0, 0, 0)'
  
  DetailPrint "Uninstallation cleanup completed!"
!macroend

; Custom page for setup options
!macro customPageAfterLicense
  PageEx directory
    PageCallbacks "" "" ""
  PageExEnd
!macroend

; Custom finish page
!macro customFinishPage
  !define MUI_FINISHPAGE_RUN "$INSTDIR\${APP_EXECUTABLE_FILENAME}"
  !define MUI_FINISHPAGE_RUN_TEXT "Launch Aura Video Studio"
  !define MUI_FINISHPAGE_LINK "Visit the Aura Video Studio website"
  !define MUI_FINISHPAGE_LINK_LOCATION "https://github.com/coffee285/aura-video-studio"
!macroend
