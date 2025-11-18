# PathSelector Component - Visual Guide

## Component Overview

The PathSelector component provides a comprehensive interface for selecting files and directories with the following visual elements:

```
┌─────────────────────────────────────────────────────────────────────┐
│ Audio File Path  [📄]  [ℹ]                                         │
├─────────────────────────────────────────────────────────────────────┤
│ e.g., C:\Users\YourName\Music\recording.wav                        │
├─────────────────────────────────────────────────────────────────────┤
│ [📄] C:\Users\John\Music\podcast.wav  [Browse...] [🔄] [📁] [❌]  │
├─────────────────────────────────────────────────────────────────────┤
│ ✓ Valid audio file (48 kHz, 16-bit, stereo)                       │
└─────────────────────────────────────────────────────────────────────┘

Legend:
📄 = Document icon (for files)
📁 = Folder icon (for directories)
ℹ = Info tooltip
🔄 = Auto-Detect button
✓ = Valid path indicator
❌ = Clear button
```

## Visual States

### 1. Empty State (File)
```
┌─────────────────────────────────────────────────────────────────────┐
│ FFmpeg Path  [📄]  [ℹ]                                             │
├─────────────────────────────────────────────────────────────────────┤
│ Default: C:\ffmpeg\bin\ffmpeg.exe                                  │
├─────────────────────────────────────────────────────────────────────┤
│ e.g., C:\ffmpeg\bin\ffmpeg.exe                                     │
├─────────────────────────────────────────────────────────────────────┤
│ [📄] Click Browse to select file                     [Browse...]   │
└─────────────────────────────────────────────────────────────────────┘

Elements:
- Label with file icon
- Info tooltip (hover for help)
- Default path hint (optional)
- Example path (optional)
- Input field with placeholder
- Browse button
```

### 2. Empty State (Directory)
```
┌─────────────────────────────────────────────────────────────────────┐
│ Output Directory  [📁]  [ℹ]                                        │
├─────────────────────────────────────────────────────────────────────┤
│ e.g., C:\Users\YourName\Videos\AuraOutput                         │
├─────────────────────────────────────────────────────────────────────┤
│ [📁] Click Browse to select folder                   [Browse...]   │
└─────────────────────────────────────────────────────────────────────┘

Elements:
- Label with folder icon (instead of file icon)
- Placeholder text says "folder" instead of "file"
- Browse button opens directory picker
```

### 3. With Value (File)
```
┌─────────────────────────────────────────────────────────────────────┐
│ Audio File Path  [📄]  [ℹ]                                         │
├─────────────────────────────────────────────────────────────────────┤
│ e.g., C:\Users\YourName\Music\recording.wav                        │
├─────────────────────────────────────────────────────────────────────┤
│ [📄] C:\Users\John\Music\podcast.wav                               │
│      [Browse...] [Open] [Clear]                                     │
└─────────────────────────────────────────────────────────────────────┘

Elements:
- Input shows selected path
- Browse button (replace selection)
- Open button (open in file explorer) - NEW
- Clear button (remove selection) - NEW
```

### 4. With Auto-Detect
```
┌─────────────────────────────────────────────────────────────────────┐
│ FFmpeg Path  [📄]  [ℹ]                                             │
├─────────────────────────────────────────────────────────────────────┤
│ [📄] C:\ffmpeg\bin\ffmpeg.exe                                      │
│      [Browse...] [Auto-Detect] [Open] [Clear]                      │
└─────────────────────────────────────────────────────────────────────┘

Elements:
- Auto-Detect button between Browse and Open
- Automatically searches common locations
```

### 5. Validating
```
┌─────────────────────────────────────────────────────────────────────┐
│ FFmpeg Path  [📄]  [ℹ]                                             │
├─────────────────────────────────────────────────────────────────────┤
│ [📄] C:\ffmpeg\bin\ffmpeg.exe                                      │
│      [Browse...] [Auto-Detect] [Open] [Clear]                      │
├─────────────────────────────────────────────────────────────────────┤
│ [⌛] Validating path...                                            │
└─────────────────────────────────────────────────────────────────────┘

Elements:
- Spinner icon
- "Validating path..." text
- Input and buttons are disabled during validation
```

### 6. Valid Path
```
┌─────────────────────────────────────────────────────────────────────┐
│ FFmpeg Path  [📄]  [ℹ]                                             │
├─────────────────────────────────────────────────────────────────────┤
│ [📄] C:\ffmpeg\bin\ffmpeg.exe                                      │
│      [Browse...] [Auto-Detect] [Open] [Clear]                      │
├─────────────────────────────────────────────────────────────────────┤
│ ✓ Valid FFmpeg installation (FFmpeg version 6.0)                   │
└─────────────────────────────────────────────────────────────────────┘

Elements:
- Green checkmark icon
- Validation message in green
- Version info (if available)
```

### 7. Invalid Path
```
┌─────────────────────────────────────────────────────────────────────┐
│ FFmpeg Path  [📄]  [ℹ]                                             │
├─────────────────────────────────────────────────────────────────────┤
│ [📄] C:\invalid\path\ffmpeg.exe                                    │
│      [Browse...] [Auto-Detect] [Open] [Clear]                      │
├─────────────────────────────────────────────────────────────────────┤
│ ✗ File does not exist or is not executable                         │
└─────────────────────────────────────────────────────────────────────┘

Elements:
- Red X icon
- Error message in red
- Buttons still enabled for correction
```

### 8. Disabled State
```
┌─────────────────────────────────────────────────────────────────────┐
│ FFmpeg Path  [📄]  [ℹ]                                             │
├─────────────────────────────────────────────────────────────────────┤
│ [📄] C:\ffmpeg\bin\ffmpeg.exe        (grayed out)                  │
│      [Browse...] [Open] [Clear]      (all disabled)                │
└─────────────────────────────────────────────────────────────────────┘

Elements:
- All elements grayed out
- No interaction possible
- Used during processing or when setting is locked
```

## Responsive Behavior

### Desktop View (Wide)
```
┌────────────────────────────────────────────────────────────────────┐
│ Audio File Path  [📄]  [ℹ]                                        │
│ [📄] C:\Users\...\audio.wav  [Browse] [Auto-Detect] [Open] [Clear]│
└────────────────────────────────────────────────────────────────────┘
```

### Mobile View (Narrow)
```
┌──────────────────────────────┐
│ Audio File Path  [📄]  [ℹ]  │
│ [📄] C:\Users\...\audio.wav │
│ [Browse] [Auto-Detect]      │
│ [Open]   [Clear]            │
└──────────────────────────────┘
```

Buttons wrap to multiple rows on smaller screens.

## Page Implementations

### Voice Enhancement Page

#### Before
```
┌─────────────────────────────────────────────────────────────────────┐
│ Audio File Path  *                                                  │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ /path/to/audio.wav                                              │ │
│ └─────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

Users had to:
- Manually type full path
- No validation
- No easy way to browse
- No help or examples

#### After
```
┌─────────────────────────────────────────────────────────────────────┐
│ Audio File Path  [📄]  [ℹ]                                         │
│ e.g., C:\Users\YourName\Music\recording.wav                        │
│ [📄] C:\Users\John\Music\podcast.wav                               │
│      [Browse...] [Open] [Clear]                                     │
│ ✓ Valid audio file                                                 │
└─────────────────────────────────────────────────────────────────────┘
```

Users can now:
- Click Browse to select file
- See validation in real-time
- Clear selection easily
- Open file location
- See example paths

### File Locations Settings

#### Before
```
┌─────────────────────────────────────────────────────────────────────┐
│ FFmpeg Path                                                         │
│ Hint: Path to ffmpeg executable (leave empty to use system PATH)   │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ C:\path\to\ffmpeg.exe or leave empty                            │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│ [📁] [Test]                                                        │
│                                                                     │
│ (Browse button showed alert: "Please enter path manually")         │
└─────────────────────────────────────────────────────────────────────┘
```

#### After
```
┌─────────────────────────────────────────────────────────────────────┐
│ FFmpeg Path  [📄]  [ℹ]                                             │
│ e.g., C:\ffmpeg\bin\ffmpeg.exe                                     │
│ [📄] C:\ffmpeg\bin\ffmpeg.exe                                      │
│      [Browse...] [Open] [Clear]                                     │
│ ✓ Valid FFmpeg installation (version 6.0)                          │
└─────────────────────────────────────────────────────────────────────┘
```

Improved for all 6 settings:
1. FFmpeg Path - File with validation
2. FFprobe Path - File
3. Output Directory - Directory
4. Temporary Directory - Directory
5. Media Library Location - Directory
6. Projects Directory - Directory

## Color Scheme

### Light Theme
- **Valid**: Green (#107C10)
- **Invalid**: Red (#D13438)
- **Info**: Blue (#0078D4)
- **Neutral**: Gray (#605E5C)
- **Background**: White (#FFFFFF)

### Dark Theme
- **Valid**: Light Green (#92C353)
- **Invalid**: Light Red (#F85149)
- **Info**: Light Blue (#4FC3F7)
- **Neutral**: Light Gray (#C8C6C4)
- **Background**: Dark Gray (#1E1E1E)

## Interaction Flow

### Selecting a File
```
1. User clicks "Browse..." button
   ↓
2. Native file picker opens
   ↓
3. User selects file
   ↓
4. Path appears in input field
   ↓
5. Validation starts (if configured)
   ↓
6. ✓ or ✗ indicator shows result
   ↓
7. "Open" and "Clear" buttons become available
```

### Selecting a Directory
```
1. User clicks "Browse..." button (directory mode)
   ↓
2. Native directory picker opens (webkitdirectory)
   ↓
3. User selects folder
   ↓
4. Directory path appears in input field
   ↓
5. Validation starts (if configured)
   ↓
6. ✓ or ✗ indicator shows result
   ↓
7. "Open" and "Clear" buttons become available
```

### Auto-Detect
```
1. User clicks "Auto-Detect" button
   ↓
2. Button shows spinner
   ↓
3. System searches common locations
   ↓
4. If found: Path populated, validation runs
   ↓
5. If not found: Error message or empty result
```

### Opening in Explorer
```
1. User clicks "Open" button
   ↓
2. API request to /api/system/open-folder
   ↓
3. Backend opens file explorer at path
   ↓
4. Explorer window appears to user
   (or silent failure if not supported)
```

## Accessibility Features

### Keyboard Navigation
```
Tab           → Move to next element
Shift+Tab     → Move to previous element
Space/Enter   → Activate button
Escape        → Close file picker
```

### Screen Reader Announcements
```
"Audio File Path, button, Browse for file"
"FFmpeg Path, edit, C:\ffmpeg\bin\ffmpeg.exe"
"Valid FFmpeg installation, version 6.0"
"Open in file explorer, button"
"Clear selection, button"
```

### Visual Indicators
- High contrast icons
- Color-blind friendly (icons + text)
- Focus rings on all interactive elements
- Disabled state clearly indicated

## Error Handling

### Validation Errors
```
File not found:
  ✗ The specified file does not exist

Permission denied:
  ✗ Access denied: Check file permissions

Invalid format:
  ✗ Invalid file format: Expected .exe file

Network path unavailable:
  ✗ Network path is not accessible
```

### Browse Errors
```
User cancels:
  (No change, no error shown)

Permission denied:
  (File picker handles this)

Path too long:
  ✗ Path exceeds maximum length (260 characters)
```

## Performance Notes

- **Debounced Validation**: 500ms delay to avoid excessive API calls
- **Lazy Validation**: Only validates when value changes
- **Async Operations**: All I/O operations are async
- **No Memory Leaks**: Proper cleanup in useEffect hooks
- **Fast Rendering**: Minimal re-renders with proper memoization

## Browser-Specific Behavior

### Chrome/Edge
- ✅ Full support for webkitdirectory
- ✅ File path available in Electron
- ✅ Smooth animations

### Firefox
- ✅ webkitdirectory supported
- ⚠️ Slightly different picker UI
- ✅ Full functionality

### Safari
- ✅ webkitdirectory supported
- ⚠️ macOS-style picker
- ✅ Full functionality

### Web (Non-Electron)
- ✅ File name available
- ❌ Full path not available (security)
- ℹ️ Users can still type paths manually

## Examples in Application

### 1. Voice Enhancement - Audio File
```typescript
<PathSelector
  label="Audio File Path"
  value={inputPath}
  onChange={setInputPath}
  type="file"
  fileTypes=".wav,.mp3,.flac,.aac,.ogg,.m4a"
  placeholder="Select audio file to enhance"
  helpText="Select the audio file you want to enhance with noise reduction and equalization"
  examplePath="C:\Users\YourName\Music\recording.wav"
  showOpenFolder={true}
  showClearButton={true}
/>
```

Result: User can easily browse for audio files, see validation, and open the file location.

### 2. Settings - Output Directory
```typescript
<PathSelector
  label="Output Directory"
  value={settings.outputDirectory}
  onChange={(value) => updateSetting('outputDirectory', value)}
  type="directory"
  placeholder="Leave empty to use default location"
  helpText="Default directory for rendered videos (leave empty for Documents\AuraVideoStudio)"
  examplePath="C:\Users\YourName\Videos\AuraOutput"
  showOpenFolder={true}
  showClearButton={true}
/>
```

Result: User can browse for output directory and open it to verify location.

### 3. Dependency Check - FFmpeg Path
```typescript
<PathSelector
  label="FFmpeg Installation Path"
  placeholder="Click Browse to select ffmpeg.exe"
  value={manualPaths.ffmpeg}
  onChange={(path) => handlePathChange('ffmpeg', path)}
  onValidate={async (path) => {
    return await validateDependencyPath('ffmpeg', path);
  }}
  helpText="Select the ffmpeg.exe file location"
  defaultPath="C:\ffmpeg\bin\ffmpeg.exe"
  dependencyId="ffmpeg"
  autoDetect={async () => {
    return await autoDetectDependency('ffmpeg');
  }}
/>
```

Result: User has multiple options: browse, auto-detect, or type path manually.

## Summary

The PathSelector component transformation:

**Before:**
- Plain text input
- Manual path typing
- No validation
- No help
- Inconsistent UI

**After:**
- Browse button with native picker
- Auto-detect for common paths
- Real-time validation
- Clear and Open buttons
- Help text and examples
- Consistent UI across app
- Visual file/folder icons
- Responsive layout
- Accessibility support

This creates a professional, user-friendly experience for casual users while maintaining flexibility for advanced users who prefer typing paths.
