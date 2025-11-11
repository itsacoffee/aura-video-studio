; NSIS Custom Installer Script for Aura Video Studio
; This script adds custom installation steps with Windows 11 compatibility

; Request admin rights for installation
RequestExecutionLevel admin

!macro customInstall
  ; ========================================
  ; Windows 11 Compatibility Registry
  ; ========================================
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "DisplayName" "Aura Video Studio"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "Publisher" "${COMPANY_NAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "DisplayIcon" "$INSTDIR\${APP_EXECUTABLE_FILENAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegDWord HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "NoModify" 1
  WriteRegDWord HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "NoRepair" 1
  
  ; ========================================
  ; File Associations (Both HKLM and HKCU for compatibility)
  ; ========================================
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
  ; Windows Defender Exclusions (Optional)
  ; ========================================
  ; Add installation directory to Windows Defender exclusions to prevent false positives
  nsExec::ExecToLog 'powershell -NoProfile -ExecutionPolicy Bypass -Command "Add-MpPreference -ExclusionPath \"$INSTDIR\" -ErrorAction SilentlyContinue"'
  
  ; ========================================
  ; Visual C++ Redistributable Check
  ; ========================================
  ; Check if Visual C++ 2015-2022 Redistributable is installed (needed for .NET)
  ReadRegStr $0 HKLM "SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" "Installed"
  ${If} $0 != "1"
    MessageBox MB_YESNO "This application requires Visual C++ Redistributable. Would you like to download it now?" IDYES download_vcredist IDNO skip_vcredist
    download_vcredist:
      ExecShell "open" "https://aka.ms/vs/17/release/vc_redist.x64.exe"
    skip_vcredist:
  ${EndIf}
  
  ; Refresh shell icon cache
  System::Call 'shell32.dll::SHChangeNotify(i, i, i, i) v (0x08000000, 0, 0, 0)'
!macroend

!macro customUnInstall
  ; ========================================
  ; Remove File Associations
  ; ========================================
  ; Machine-level associations
  DeleteRegKey HKLM "Software\Classes\.aura"
  DeleteRegKey HKLM "Software\Classes\.avsproj"
  DeleteRegKey HKLM "Software\Classes\AuraVideoStudio.Project"
  
  ; User-level associations
  DeleteRegKey HKCU "Software\Classes\.aura"
  DeleteRegKey HKCU "Software\Classes\.avsproj"
  DeleteRegKey HKCU "Software\Classes\AuraVideoStudio.Project"
  
  ; ========================================
  ; Remove Windows 11 Uninstall Registry
  ; ========================================
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}"
  
  ; ========================================
  ; Remove Windows Defender Exclusions
  ; ========================================
  nsExec::ExecToLog 'powershell -NoProfile -ExecutionPolicy Bypass -Command "Remove-MpPreference -ExclusionPath \"$INSTDIR\" -ErrorAction SilentlyContinue"'
  
  ; Refresh shell icon cache
  System::Call 'shell32.dll::SHChangeNotify(i, i, i, i) v (0x08000000, 0, 0, 0)'
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
