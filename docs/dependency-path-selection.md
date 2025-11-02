# Dependency Path Selection Guide

## Overview

The Aura Video Studio now provides an enhanced user experience for configuring dependency paths, particularly for Ollama and other required tools. This guide explains how to use the improved path selection features.

## Features

### 1. Auto-Detection

The application automatically detects installed dependencies using multiple methods:

#### Ollama Detection
- **Running Process Check**: Detects if Ollama is currently running and retrieves its executable path
- **Common Paths**: Checks standard installation locations:
  - `C:\Users\{username}\AppData\Local\Programs\Ollama\ollama.exe`
  - `C:\Program Files\Ollama\ollama.exe`
- **PATH Variable**: Searches directories listed in the system PATH environment variable
- **HTTP Check**: Tests if Ollama server is responding at `http://localhost:11434`

### 2. Manual Path Selection

If auto-detection doesn't find your installation, you can manually specify the path:

1. Click the **"Browse..."** button next to the path field
2. Navigate to the executable file location
3. Select the appropriate file (e.g., `ollama.exe`)
4. The path will be validated automatically

### 3. Path Validation

When you enter or select a path, the system validates it in real-time:

- ✓ **Valid**: Green checkmark indicates the path is correct and the tool is functional
- ✗ **Invalid**: Red X indicates an issue with the path
- **Version Display**: Shows the detected version when available

### 4. Status Indicators

Dependencies now show clearer status messages:

- **✓ Ollama detected and running at {path}**: Installed and server is responding
- **⚠ Ollama installed at {path} but not running**: Executable found but server not started
- **✗ Ollama not found**: Not installed or path not configured

## Usage

### First-Run Wizard

During the first-run wizard, dependency validation occurs automatically at Step 3:

1. **Auto-Detection**: The system scans for installed dependencies
2. **Review Status**: Check the status of each dependency
3. **Manual Configuration**: For missing dependencies, use the path selector:
   - Click "Browse..." to select the executable
   - Or click "Auto-Detect" to re-scan
   - Or enter the path manually
4. **Apply Path**: Click "Apply Path" to save your selection
5. **Verify**: The system validates the path and updates the status

### Settings Page

You can also configure paths later in the Settings → Dependencies section:

1. Navigate to Settings
2. Select the Dependencies tab
3. Use the same path selection interface as in the wizard

## Troubleshooting

### Ollama Shows "Not Found" Despite Being Installed

**Solutions**:

1. **Use Auto-Detect**: Click the "Auto-Detect" button to re-scan for Ollama
2. **Manual Selection**: Click "Browse..." and navigate to:
   - Default: `C:\Users\{YourUsername}\AppData\Local\Programs\Ollama\ollama.exe`
   - Or wherever you installed Ollama
3. **Start Ollama**: If the status shows "installed but not running", start the Ollama server:
   - Open Command Prompt
   - Run: `ollama serve`
   - Or start from the Start menu

### Validation Fails Even Though File Exists

**Check**:

1. **Correct File**: Ensure you selected `ollama.exe` (not just the folder)
2. **File Permissions**: Verify you have execute permissions
3. **Corrupted Installation**: Try reinstalling Ollama
4. **Path Spaces**: If the path contains spaces, try using quotes or a path without spaces

### Path Not Saved After Selection

**Solutions**:

1. Click **"Apply Path"** after selecting the file
2. Wait for validation (green checkmark) before clicking Apply
3. Check error messages for specific issues

## API Endpoints

For developers integrating with the API:

### Validate Path
```http
POST /api/dependencies/{dependencyId}/validate-path
Content-Type: application/json

{
  "path": "C:\\path\\to\\ollama.exe"
}
```

Response:
```json
{
  "isValid": true,
  "message": "Valid Ollama executable",
  "version": "0.1.26"
}
```

### Auto-Detect Path
```http
POST /api/dependencies/{dependencyId}/detect
```

Response:
```json
{
  "success": true,
  "path": "C:\\Users\\username\\AppData\\Local\\Programs\\Ollama\\ollama.exe"
}
```

## Supported Dependencies

The path selection feature supports:

- **Ollama**: Local AI model server
- **FFmpeg**: Video rendering engine
- **Stable Diffusion WebUI**: Image generation (URL or path)
- **Piper TTS**: Text-to-speech synthesis

## Benefits

1. **User-Friendly**: No need to remember complex paths
2. **Reliable**: Multiple detection methods ensure high success rate
3. **Validation**: Real-time feedback prevents configuration errors
4. **Flexible**: Support for both auto-detection and manual selection
5. **Clear Status**: Visual indicators show exactly what's installed and running

## Future Enhancements

Planned improvements:

- Support for more dependencies
- Installation recommendations based on detected hardware
- One-click installation for missing dependencies
- Import/export of dependency configurations
- Automatic path updates when dependencies are reinstalled
