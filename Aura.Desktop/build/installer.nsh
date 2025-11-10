; NSIS Custom Installer Script for Aura Video Studio
; This script adds custom installation steps

!macro customInstall
  ; Create registry entries for file associations
  WriteRegStr HKCU "Software\Classes\.aura" "" "AuraVideoStudio.Project"
  WriteRegStr HKCU "Software\Classes\AuraVideoStudio.Project" "" "Aura Video Studio Project"
  WriteRegStr HKCU "Software\Classes\AuraVideoStudio.Project\DefaultIcon" "" "$INSTDIR\${APP_EXECUTABLE_FILENAME},0"
  WriteRegStr HKCU "Software\Classes\AuraVideoStudio.Project\shell\open\command" "" '"$INSTDIR\${APP_EXECUTABLE_FILENAME}" "%1"'
  
  ; Refresh shell icon cache
  System::Call 'shell32.dll::SHChangeNotify(i, i, i, i) v (0x08000000, 0, 0, 0)'
!macroend

!macro customUnInstall
  ; Remove registry entries for file associations
  DeleteRegKey HKCU "Software\Classes\.aura"
  DeleteRegKey HKCU "Software\Classes\AuraVideoStudio.Project"
  
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
