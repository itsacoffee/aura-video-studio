# Path Expansion Examples

## How Environment Variable Expansion Works

### Windows Examples

**Input**: `%USERPROFILE%\Videos\Aura`
**Expanded**: `C:\Users\JohnDoe\Videos\Aura`
**Created**: Yes (if parent exists)

**Input**: `%TEMP%\AuraOutput`
**Expanded**: `C:\Users\JohnDoe\AppData\Local\Temp\AuraOutput`
**Created**: Yes (if parent exists)

**Input**: `%APPDATA%\Aura\Videos`
**Expanded**: `C:\Users\JohnDoe\AppData\Roaming\Aura\Videos`
**Created**: Yes (if parent exists)

**Input**: `C:\Users\%USERNAME%\Documents\Aura`
**Expanded**: `C:\Users\JohnDoe\Documents\Aura`
**Created**: Yes (if parent exists)

**Input**: `%USERPROFILE%\%COMPUTERNAME%\Videos`
**Expanded**: `C:\Users\JohnDoe\DESKTOP-ABC123\Videos`
**Created**: Yes (if parent exists)

### Unix/Linux/macOS Examples

**Input**: `~/Videos/Aura`
**Expanded**: `/home/johndoe/Videos/Aura`
**Created**: Yes (if parent exists)

**Input**: `~`
**Expanded**: `/home/johndoe`
**Created**: No (already exists)

**Input**: `$HOME/Videos/Aura`
**Expanded**: `/home/johndoe/Videos/Aura`
**Created**: Yes (if parent exists)

**Input**: `~/Movies/Aura`
**Expanded**: `/Users/johndoe/Movies/Aura` (macOS)
**Created**: Yes (if parent exists)

**Input**: `/tmp/aura-output`
**Expanded**: `/tmp/aura-output`
**Created**: Yes (if parent exists)

### Edge Cases Handled

**Input**: `%NONEXISTENT%\Videos`
**Expanded**: `%NONEXISTENT%\Videos` (not expanded)
**Error**: `Failed to create directory '%NONEXISTENT%\Videos': The filename, directory name, or volume label syntax is incorrect.`

**Input**: `` (empty string)
**Error**: `Path cannot be empty`

**Input**: `C:\Invalid<>Path` (Windows)
**Error**: `Failed to create directory 'C:\Invalid<>Path': Illegal characters in path.`

**Input**: `/invalid:path` (Unix)
**Error**: `Directory is not writable: Permission denied` (or similar based on system)

## API Response Examples

### Successful Validation (Windows)

**Request**:
```json
POST /api/setup/check-directory
{
  "path": "%USERPROFILE%\\Videos\\Aura"
}
```

**Response**:
```json
{
  "isValid": true,
  "error": null,
  "expandedPath": "C:\\Users\\JohnDoe\\Videos\\Aura",
  "correlationId": "0HN3K2L3M4N5O6P7Q8R9S0T1"
}
```

### Failed Validation (Directory Creation Error)

**Request**:
```json
POST /api/setup/check-directory
{
  "path": "C:\\Program Files\\Aura"
}
```

**Response**:
```json
{
  "isValid": false,
  "error": "Failed to create directory 'C:\\Program Files\\Aura': Access to the path 'C:\\Program Files' is denied.",
  "expandedPath": null,
  "correlationId": "0HN3K2L3M4N5O6P7Q8R9S0T2"
}
```

### Successful Setup Completion (Unix)

**Request**:
```json
POST /api/setup/complete
{
  "outputDirectory": "~/Videos/Aura",
  "ffmpegPath": null
}
```

**Response**:
```json
{
  "success": true,
  "errors": [],
  "correlationId": "0HN3K2L3M4N5O6P7Q8R9S0T3"
}
```

**Database Entry** (WizardState):
```json
{
  "outputDirectory": "/home/johndoe/Videos/Aura"
}
```

Note: The expanded path is stored in the database, not the original path with environment variables.

## Log Examples

### Successful Path Expansion and Creation

```
[2025-11-22T20:57:30.123Z] [Information] [abc123] Checking directory: %USERPROFILE%\Videos\Aura
[2025-11-22T20:57:30.124Z] [Information] [abc123] Validating directory path. Original: %USERPROFILE%\Videos\Aura, Expanded: C:\Users\JohnDoe\Videos\Aura
[2025-11-22T20:57:30.125Z] [Information] [abc123] Created directory: C:\Users\JohnDoe\Videos\Aura
[2025-11-22T20:57:30.126Z] [Information] [abc123] Directory write test successful: C:\Users\JohnDoe\Videos\Aura
[2025-11-22T20:57:30.127Z] [Information] [abc123] Directory validated successfully: C:\Users\JohnDoe\Videos\Aura
```

### Failed Path Expansion (Permission Denied)

```
[2025-11-22T20:57:35.123Z] [Information] [def456] Checking directory: C:\Program Files\Aura
[2025-11-22T20:57:35.124Z] [Information] [def456] Validating directory path. Original: C:\Program Files\Aura, Expanded: C:\Program Files\Aura
[2025-11-22T20:57:35.125Z] [Warning] [def456] Failed to create directory: C:\Program Files\Aura
   System.UnauthorizedAccessException: Access to the path 'C:\Program Files' is denied.
[2025-11-22T20:57:35.126Z] [Warning] [def456] Directory validation failed: Failed to create directory 'C:\Program Files\Aura': Access to the path 'C:\Program Files' is denied.
```

### Setup Completion with Path Expansion

```
[2025-11-22T20:58:00.123Z] [Information] [ghi789] Starting setup completion, FFmpegPath: (none), OutputDirectory: ~/Videos/Aura
[2025-11-22T20:58:00.124Z] [Information] [ghi789] Validating directory path. Original: ~/Videos/Aura, Expanded: /home/johndoe/Videos/Aura
[2025-11-22T20:58:00.125Z] [Information] [ghi789] Created directory: /home/johndoe/Videos/Aura
[2025-11-22T20:58:00.126Z] [Information] [ghi789] Directory write test successful: /home/johndoe/Videos/Aura
[2025-11-22T20:58:00.127Z] [Information] [ghi789] Output directory validated successfully: /home/johndoe/Videos/Aura
[2025-11-22T20:58:00.200Z] [Information] [ghi789] Setup completed successfully for user 'default', IsNewSetup: True, FFmpegConfigured: False, WorkspaceConfigured: True
```

## Benefits of This Implementation

1. **User-Friendly**: Users can enter familiar paths with environment variables
2. **Cross-Platform**: Works on Windows, Linux, and macOS
3. **Safe**: Validates permissions and handles errors gracefully
4. **Automatic**: Creates directories automatically if parent exists
5. **Traceable**: Every operation has correlation ID for debugging
6. **Clear Errors**: Error messages show both original and expanded paths
7. **Persistent**: Stores expanded paths to avoid re-expansion issues
