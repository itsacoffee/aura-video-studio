# Desktop App Startup Troubleshooting Guide

This guide helps diagnose and fix startup issues with the Aura Video Studio desktop application.

## Common Error Codes

### Exit Code 1
**Cause:** Configuration validation failed or critical service initialization error.

**Solutions:**
1. Check database file permissions in the application data directory
2. Ensure port 5005 (or your configured port) is available
3. Verify sufficient disk space
4. Check antivirus/firewall settings

### Exit Code 3762504530 (0xE0434352)
**Cause:** .NET CLR unhandled exception during startup.

**This error code has been fixed in the latest version.** The application now:
- Handles exceptions gracefully instead of crashing
- Provides detailed error messages in logs
- Continues startup even if non-critical services fail

**Solutions:**
1. Check application logs (location shown in error dialog)
2. Ensure all dependencies are installed
3. Try restarting the application
4. If persistent, delete the database file (will recreate on next start)

## Log Locations

### Windows
```
%APPDATA%\aura-video-studio\logs\
```

### macOS
```
~/Library/Application Support/aura-video-studio/logs/
```

### Linux
```
~/.config/aura-video-studio/logs/
```

## Common Issues and Solutions

### Port Already in Use

**Symptom:** Error message about port 5005 being in use.

**Solutions:**
1. Close any other instances of Aura Video Studio
2. Check for other applications using port 5005:
   - Windows: `netstat -ano | findstr :5005`
   - macOS/Linux: `lsof -i :5005`
3. Change the port in settings (if available)

### Database Corruption

**Symptom:** Database integrity check failures in logs.

**Solutions:**
1. Locate the database file (`aura.db`) in your application data directory
2. Backup the file if it contains important data
3. Delete the database file (it will be recreated on next start)
4. Restart the application

### Missing Dependencies

**Symptom:** Errors about missing FFmpeg or other dependencies.

**Solutions:**
1. Use the built-in dependency installer in the app (if available)
2. Manually install FFmpeg:
   - Windows: Download from ffmpeg.org and add to PATH
   - macOS: `brew install ffmpeg`
   - Linux: `sudo apt install ffmpeg`

### Firewall/Antivirus Blocking

**Symptom:** Backend server fails to start or respond to health checks.

**Solutions:**
1. Add Aura Video Studio to your antivirus exceptions
2. Allow the application through Windows Firewall
3. Temporarily disable antivirus to test (re-enable after)

## Diagnostic Steps

### 1. Check Logs
Always start by checking the application logs. They contain detailed information about what went wrong.

### 2. Clean Start
Try deleting the application data directory (backup first if needed):
- Windows: `%APPDATA%\aura-video-studio`
- macOS: `~/Library/Application Support/aura-video-studio`
- Linux: `~/.config/aura-video-studio`

### 3. Reinstall
If all else fails, uninstall and reinstall the application.

## Recent Fixes

The following improvements have been made to prevent startup failures:

1. **Graceful Error Handling:** The app no longer crashes with cryptic exit codes. Errors are logged with detailed information.

2. **Port Conflict Resolution:** Port conflicts are now warnings instead of critical errors.

3. **Degraded Mode Operation:** The app can start even if some non-critical services fail, allowing users to troubleshoot via the UI.

4. **Better Error Messages:** The Electron wrapper now provides detailed, actionable error messages instead of just exit codes.

5. **Improved Logging:** All startup errors are logged to files with full stack traces for debugging.

## Getting Help

If you continue to experience issues:

1. Collect the log files from the logs directory
2. Note the exact error message or exit code
3. Open an issue on GitHub with:
   - Your operating system and version
   - The error message or exit code
   - Relevant log excerpts (sanitize any sensitive information)
   - Steps to reproduce

## Developer Notes

### Architecture Changes

The startup sequence now follows this order:

1. **Service Registration** (Program.cs lines 1-1650)
   - All services are registered with DI container
   - No execution happens yet

2. **App Build** (line 1650)
   - `var app = builder.Build()`
   - DI container is finalized

3. **Database Initialization** (lines 1655-1694)
   - Synchronous database setup
   - Wrapped in try-catch to prevent crashes

4. **Configuration Validation** (lines 1700-1727)
   - Validates settings
   - Now wrapped in try-catch
   - Port conflicts are warnings, not critical errors

5. **Middleware Configuration** (lines 1728-4504)
   - All middleware and endpoints configured
   - SignalR hubs mapped
   - Job queue events wired

6. **App Run** (lines 4752-4775)
   - Hosted services start automatically
   - All errors caught and logged
   - Application continues even with failures

### Key Changes

1. **ConfigurationValidator.cs:**
   - Port conflicts changed from `Critical` to `Warning`

2. **Program.cs:**
   - Configuration validation wrapped in try-catch
   - Removed `Environment.Exit(1)` calls
   - Added comprehensive error logging around `app.Run()`

3. **StartupInitializationService.cs:**
   - Removed `Environment.Exit(1)` call
   - Service failures no longer terminate the process
   - Failed critical services logged but app continues

4. **electron.js:**
   - Enhanced error messages with exit code explanations
   - Added startup log collection
   - Provides actionable troubleshooting steps in error dialogs
