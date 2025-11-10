# General Errors and Troubleshooting

This guide covers general errors that don't fit into specific categories.

## Quick Navigation

- [Operation Cancelled](#operation-cancelled)
- [Not Implemented](#not-implemented)
- [Unexpected Errors](#unexpected-errors)
- [Database Errors](#database-errors)
- [Configuration Errors](#configuration-errors)

---

## Operation Cancelled

### Error Code E998: Operation Cancelled

**Error Message**: "The operation was cancelled by the user or system"

### Common Causes

1. **User Cancelled**:
   - Clicked "Cancel" button
   - Closed dialog mid-operation
   - Interrupted long-running process

2. **System Cancelled**:
   - Application shutdown during operation
   - Timeout exceeded
   - Resource constraints

3. **Automatic Cancellation**:
   - Parent operation cancelled
   - Dependency failed
   - Circuit breaker opened

### Solutions

#### If Accidentally Cancelled

1. **Retry the operation**
2. **Check for auto-save**:
   - Settings → Projects → Auto-save
   - May have partial progress saved

#### If Operation Taking Too Long

1. **Check progress indicator**:
   - Some operations show progress
   - Wait if close to completion

2. **Increase timeout**:
   ```json
   {
     "Timeouts": {
       "Operation": 600000  // 10 minutes
     }
   }
   ```

3. **Break into smaller operations**:
   - Process in batches
   - Save progress between batches

#### Prevent Accidental Cancellation

1. **Enable confirmation**:
   ```json
   {
     "UI": {
       "ConfirmCancellation": true,
       "WarnOnLongOperations": true
     }
   }
   ```

2. **Disable cancel button during critical operations**:
   ```json
   {
     "UI": {
       "AllowCancelDuringRender": false
     }
   }
   ```

---

## Not Implemented

### Error Code E997: Feature Not Implemented

**Error Message**: "This feature is not yet implemented"

### What This Means

- Feature is planned but not yet developed
- Feature may be platform-specific and unavailable on your OS
- Feature requires specific hardware not present

### Check Feature Status

1. **Review Roadmap**: [Roadmap Documentation](../roadmap.md)
2. **Check GitHub Issues**: May be in development
3. **Check System Requirements**: Feature may require specific hardware

### Workarounds

#### Alternative Methods

Some unimplemented features may have alternatives:
- Advanced export formats → Use standard formats
- Experimental effects → Use built-in effects
- Beta features → Use stable equivalents

#### Request Feature

1. Check if already requested: [GitHub Issues](https://github.com/Coffee285/aura-video-studio/issues)
2. Create feature request with:
   - Use case description
   - Expected behavior
   - Alternative solutions tried
   - Willingness to test beta

#### Enable Experimental Features

Some features are implemented but experimental:
```json
{
  "Features": {
    "EnableExperimental": true,
    "ExperimentalFeatures": [
      "AdvancedTransitions",
      "AIVoiceCloning",
      "RealTimePreview"
    ]
  }
}
```

**Warning**: Experimental features may:
- Be unstable
- Have bugs
- Change without notice
- Be removed in future versions

---

## Unexpected Errors

### Error Code E999: Unexpected Error

**Error Message**: "An unexpected error occurred"

### What This Means

- Unhandled exception
- Rare edge case
- Potential bug

### Immediate Actions

1. **Save Work**:
   - Save project immediately
   - Export work-in-progress if possible

2. **Note Error Context**:
   - What were you doing?
   - What settings were active?
   - Can you reproduce it?

3. **Check Logs**:
   - Windows: `%APPDATA%\Aura\logs\`
   - Linux/Mac: `~/.config/aura/logs/`

### Solutions

#### 1. Restart Application

Simple restart often resolves:
- Memory leaks
- Corrupted state
- Temporary glitches

#### 2. Clear Cache

```bash
# Via UI
Settings → Storage → Clear Cache

# Manually
# Windows
del /s "%APPDATA%\Aura\cache\*"

# Linux/Mac
rm -rf ~/.config/aura/cache/*
```

#### 3. Reset Configuration

If error persists, reset to defaults:
```bash
# Backup current config
cp appsettings.json appsettings.json.backup

# Delete config (will regenerate on next start)
rm appsettings.json
```

#### 4. Update Application

Check for updates:
```bash
git pull origin main
dotnet build
```

#### 5. Report Bug

If error persists:
1. Enable debug logging:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug"
       }
     }
   }
   ```
2. Reproduce error
3. Collect logs
4. Create GitHub issue with:
   - Steps to reproduce
   - Expected vs actual behavior
   - Error logs (sanitized of sensitive data)
   - System information
   - Screenshots if applicable

---

## Database Errors

### Database Locked

**Error**: "Database is locked"

**Causes**:
- Another Aura instance is running
- Previous instance didn't close cleanly
- Backup software accessing database

**Solutions**:

1. **Close All Aura Instances**:
   ```bash
   # Windows
   taskkill /IM Aura.Api.exe /F
   
   # Linux/Mac
   killall Aura.Api
   ```

2. **Remove Stale Lock**:
   ```bash
   # Windows
   del "%APPDATA%\Aura\.lock"
   
   # Linux/Mac
   rm ~/.config/aura/.lock
   ```

3. **Wait for Lock Release**:
   - Lock typically releases within 30 seconds
   - May be held by backup software

### Database Corrupted

**Error**: "Database file is corrupted"

**Solutions**:

1. **Restore from Backup**:
   ```bash
   # Windows
   copy "%APPDATA%\Aura\backups\aura.db.backup" "%APPDATA%\Aura\aura.db"
   
   # Linux/Mac
   cp ~/.config/aura/backups/aura.db.backup ~/.config/aura/aura.db
   ```

2. **Repair Database**:
   ```bash
   sqlite3 ~/.config/aura/aura.db ".recover" | sqlite3 aura_recovered.db
   ```

3. **Recreate Database**:
   ```bash
   # Last resort - loses all data
   rm ~/.config/aura/aura.db
   # Aura will create new database on next start
   ```

### Migration Failed

**Error**: "Database migration failed"

**Solutions**:

1. **Check Database Version**:
   ```bash
   sqlite3 ~/.config/aura/aura.db "SELECT version FROM migrations ORDER BY version DESC LIMIT 1;"
   ```

2. **Rollback Migration**:
   ```bash
   # Use migration rollback tool
   dotnet run --project Aura.Api -- migrate:rollback
   ```

3. **Fresh Migration**:
   ```bash
   # Backup data
   cp aura.db aura.db.backup
   
   # Recreate and migrate
   rm aura.db
   dotnet run --project Aura.Api -- migrate:up
   ```

---

## Configuration Errors

### Invalid Configuration

**Error**: "Configuration file is invalid"

**Causes**:
- JSON syntax error
- Invalid values
- Missing required sections

**Solutions**:

1. **Validate JSON**:
   ```bash
   # Use online validator: https://jsonlint.com/
   # Or command line:
   jq . appsettings.json
   ```

2. **Check for Common Issues**:
   - Trailing commas
   - Unescaped quotes
   - Missing brackets
   - Incorrect paths (use forward slashes or escaped backslashes)

3. **Use Example Config**:
   ```bash
   cp appsettings.example.json appsettings.json
   # Then reconfigure
   ```

### Missing Configuration

**Error**: "Required configuration section is missing"

**Solutions**:

1. **Add Missing Section**:
   ```json
   {
     "Providers": {
       "OpenAI": {
         "ApiKey": "your-key-here",
         "BaseUrl": "https://api.openai.com/v1"
       }
     }
   }
   ```

2. **Use Configuration Builder**:
   - First Run Wizard walks through configuration
   - Or Settings UI to configure visually

### Environment Variable Issues

**Error**: "Environment variable not found"

**Solutions**:

1. **Set Environment Variable**:
   ```bash
   # Windows
   setx OPENAI_KEY "sk-..."
   
   # Linux/Mac
   export OPENAI_KEY="sk-..."
   ```

2. **Use Configuration File Instead**:
   - More reliable than environment variables
   - Easier to manage

3. **Check Variable Name**:
   - Case-sensitive on Linux/Mac
   - Ensure correct spelling

---

## Application Startup Errors

### Failed to Start

**Error**: "Application failed to start"

**Common Causes**:

1. **Port Already in Use**:
   ```bash
   # Check what's using port 5005
   netstat -ano | findstr :5005  # Windows
   lsof -i :5005  # Linux/Mac
   
   # Change Aura port
   # In appsettings.json:
   {
     "Urls": "http://localhost:5006"
   }
   ```

2. **Missing Dependencies**:
   ```bash
   # Install .NET dependencies
   dotnet restore
   
   # Install Node.js dependencies (for web UI)
   cd Aura.Web
   npm install
   ```

3. **Permission Issues**:
   ```bash
   # Linux/Mac: May need permission to bind to port
   # Use port > 1024 or run with sudo (not recommended)
   ```

### White Screen on Launch

See [Troubleshooting Guide - White Screen](Troubleshooting.md#white-screen-or-application-failed-to-initialize)

---

## Performance Issues

### Slow Application

**Solutions**:

1. **Clear Cache**:
   ```bash
   Settings → Storage → Clear Cache
   ```

2. **Reduce Memory Usage**:
   ```json
   {
     "Performance": {
       "MaxMemoryMB": 2048,
       "EnableMemoryOptimization": true
     }
   }
   ```

3. **Disable Unused Features**:
   ```json
   {
     "Features": {
       "EnableLivePreview": false,
       "EnableAutoSave": false
     }
   }
   ```

4. **Close Browser Tabs**:
   - Web UI runs in browser
   - Close other tabs to free resources

### Frequent Crashes

**Solutions**:

1. **Update Application**:
   ```bash
   git pull origin main
   dotnet build
   ```

2. **Check Logs for Patterns**:
   - Look for repeated errors
   - Note what triggers crash

3. **Disable Hardware Acceleration**:
   ```json
   {
     "Rendering": {
       "UseHardwareAcceleration": false
     }
   }
   ```

4. **Increase System Resources**:
   - Close other applications
   - Add more RAM if possible
   - Use SSD for better performance

---

## Getting Help

### Before Asking for Help

1. **Search Existing Issues**:
   - [GitHub Issues](https://github.com/Coffee285/aura-video-studio/issues)
   - May already be reported and fixed

2. **Check Documentation**:
   - [Troubleshooting Guide](Troubleshooting.md)
   - Category-specific guides
   - [FAQ](../getting-started/FAQ.md)

3. **Enable Debug Logging**:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug"
       }
     }
   }
   ```

4. **Collect Information**:
   - Error message (full text)
   - Steps to reproduce
   - System information
   - Logs (sanitized)
   - Screenshots

### Create GitHub Issue

1. Go to [Issues](https://github.com/Coffee285/aura-video-studio/issues)
2. Click "New Issue"
3. Choose appropriate template
4. Fill in all sections
5. Attach logs/screenshots
6. Be responsive to questions

### Community Support

- Check GitHub Discussions
- Review closed issues for solutions
- Help others when you can

---

## Related Documentation

- [Main Troubleshooting Guide](Troubleshooting.md)
- [Provider Errors](provider-errors.md)
- [Validation Errors](validation-errors.md)
- [Resource Errors](resource-errors.md)
- [Getting Started Guide](../getting-started/INSTALLATION.md)

## Preventive Measures

### Regular Maintenance

1. **Update Regularly**:
   ```bash
   git pull origin main
   dotnet build
   ```

2. **Clear Cache Weekly**:
   ```bash
   Settings → Storage → Clear Cache
   ```

3. **Backup Projects**:
   - Export projects regularly
   - Keep database backups

4. **Monitor Logs**:
   - Check for warnings
   - Address issues early

5. **Keep Dependencies Updated**:
   ```bash
   # Update .NET packages
   dotnet outdated
   dotnet update
   
   # Update Node packages
   cd Aura.Web
   npm outdated
   npm update
   ```

### Best Practices

1. **Save Frequently**: Don't rely on auto-save alone
2. **Test Changes**: Test with small projects first
3. **Use Stable Features**: Avoid experimental features in production
4. **Monitor Resources**: Keep eye on disk space and memory
5. **Read Release Notes**: Check for breaking changes
