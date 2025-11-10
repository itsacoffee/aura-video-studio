# Access Errors

This guide helps you troubleshoot access denied and permission errors in Aura Video Studio.

## Quick Navigation

- [Common Access Errors](#common-access-errors)
- [File System Permissions](#file-system-permissions)
- [API Authentication](#api-authentication)
- [Feature Access Control](#feature-access-control)

---

## Common Access Errors

### Error Code E003: Access Denied

General access denied error. Check specific error message for details.

### Error Code E401: Unauthorized

Authentication required or failed.

### Error Code E403: Forbidden

Authenticated but not authorized to perform this action.

---

## File System Permissions

### Cannot Write to Output Directory

**Error**: "Output directory is not writable" or "Permission denied"

**Common Causes**:
1. Directory is read-only
2. Insufficient user permissions
3. Directory is in protected system location
4. File is locked by another process

**Solutions**:

#### Windows

1. **Check Folder Properties**:
   - Right-click output folder → Properties
   - Uncheck "Read-only" if checked
   - Click Apply

2. **Grant Write Permissions**:
   - Right-click folder → Properties → Security
   - Click Edit → Select your user
   - Check "Full control" or "Modify"
   - Click Apply

3. **Use User Directory**:
   ```
   Instead of: C:\Program Files\Aura\output
   Use: C:\Users\YourName\Videos\Aura
   ```

4. **Run as Administrator** (not recommended for regular use):
   - Right-click Aura → Run as Administrator

#### Linux/Mac

1. **Check Permissions**:
   ```bash
   ls -la /path/to/output
   # Should show write permission (w)
   ```

2. **Grant Write Permission**:
   ```bash
   chmod u+w /path/to/output
   # Or for full access:
   chmod 755 /path/to/output
   ```

3. **Change Ownership** (if needed):
   ```bash
   sudo chown $USER:$USER /path/to/output
   ```

4. **Use Home Directory**:
   ```bash
   Instead of: /usr/local/aura/output
   Use: ~/Videos/Aura
   ```

### Cannot Read Input Files

**Error**: "Cannot access input file" or "File not found"

**Solutions**:

1. **Verify File Exists**:
   ```bash
   # Windows
   dir "C:\path\to\file.mp4"
   
   # Linux/Mac
   ls -l "/path/to/file.mp4"
   ```

2. **Check Read Permissions**:
   ```bash
   # Linux/Mac
   chmod u+r /path/to/file.mp4
   ```

3. **Move File to Accessible Location**:
   - Copy files to user directory
   - Avoid network drives for better reliability

### Cannot Create Temporary Files

**Error**: "Failed to create temporary file"

**Solutions**:

1. **Check Temp Directory**:
   ```bash
   # Windows
   echo %TEMP%
   
   # Linux/Mac
   echo $TMPDIR
   ```

2. **Clear Temp Files**:
   ```bash
   # Windows
   cleanmgr
   # Or manually delete: %TEMP%\Aura\*
   
   # Linux/Mac
   rm -rf /tmp/aura_*
   ```

3. **Set Custom Temp Directory**:
   ```json
   // In appsettings.json
   {
     "FileSystem": {
       "TempDirectory": "C:\\Users\\YourName\\AppData\\Local\\Temp\\Aura"
     }
   }
   ```

4. **Ensure Sufficient Space**:
   - Temp directory needs several GB free
   - Move temp to drive with more space

---

## API Authentication

### Missing API Key

**Error**: "API key required" or "Authentication required"

**Error Codes**:
- **E401**: Unauthorized
- **MissingApiKey**: API key not configured

**Solutions**:

1. **Configure API Keys**:
   - Go to Settings → Providers
   - Enter API key for each provider
   - Click "Test Connection"
   - Save settings

2. **Verify API Key Format**:
   ```
   OpenAI: sk-...
   Anthropic: sk-ant-...
   ElevenLabs: UUID format
   Stability AI: sk-...
   ```

3. **Check Environment Variables**:
   ```bash
   # Linux/Mac
   export OPENAI_KEY="sk-..."
   export ANTHROPIC_KEY="sk-ant-..."
   
   # Windows PowerShell
   $env:OPENAI_KEY="sk-..."
   $env:ANTHROPIC_KEY="sk-ant-..."
   ```

4. **Verify Configuration File**:
   ```json
   // appsettings.json
   {
     "Providers": {
       "OpenAI": {
         "ApiKey": "sk-..."
       }
     }
   }
   ```

### Invalid API Key

**Error**: "API key is invalid or expired"

**Error Codes**:
- **E100-401**: LLM authentication failed
- **E200-401**: TTS authentication failed
- **E400-401**: Image generation authentication failed

**Solutions**:

1. **Regenerate API Key**:
   - Visit provider dashboard
   - Generate new API key
   - Update in Aura settings

2. **Check Account Status**:
   - Verify account is active
   - Check for billing issues
   - Ensure account tier has API access

3. **Test API Key Directly**:
   ```bash
   # OpenAI
   curl https://api.openai.com/v1/models \
     -H "Authorization: Bearer sk-..." \
     | jq .
   
   # Anthropic
   curl https://api.anthropic.com/v1/messages \
     -H "x-api-key: sk-ant-..." \
     -H "content-type: application/json"
   ```

### API Key Quota Exceeded

**Error**: "API key quota exceeded" or "Usage limit reached"

**Solutions**:

1. **Check Usage Dashboard**:
   - Visit provider's usage dashboard
   - View current usage and limits
   - Check quota reset time

2. **Upgrade Account Tier**:
   - Higher tiers have higher quotas
   - Pay-as-you-go options available

3. **Use Alternative Provider**:
   - Configure multiple providers
   - Enable automatic fallback
   - Distribute requests across providers

4. **Optimize Usage**:
   - Reduce request frequency
   - Use caching when possible
   - Batch similar requests

---

## Feature Access Control

### Advanced Mode Required

**Error**: "This feature requires Advanced Mode"

**Error Code**: E403

**Solutions**:

1. **Enable Advanced Mode**:
   - Go to Settings → General
   - Toggle "Advanced Mode" ON
   - Review additional settings available

2. **Understand Advanced Mode**:
   - Unlocks advanced features
   - Shows additional configuration options
   - Enables expert-level controls
   - May expose complexity

**Features Requiring Advanced Mode**:
- Custom FFmpeg commands
- Advanced timeline editing
- Direct API access
- Provider configuration overrides
- Debug logging
- Experimental features

### Feature Not Available

**Error**: "Feature not available" or "Not implemented"

**Error Code**: E997

**Solutions**:

1. **Check Feature Status**:
   - Some features are platform-specific
   - Some features are in development
   - Some features require specific hardware

2. **Verify System Requirements**:
   - Check if feature requires GPU
   - Verify OS compatibility
   - Ensure FFmpeg supports feature

3. **Update Aura**:
   ```bash
   # Check current version
   # Settings → About
   
   # Update to latest
   git pull origin main
   dotnet build
   ```

4. **Check Roadmap**:
   - See [Roadmap Documentation](../roadmap.md)
   - Feature may be planned for future release

### Regional Restrictions

**Error**: "Feature not available in your region"

**Causes**:
- Provider API not available in region
- Content restrictions
- Legal/regulatory limitations

**Solutions**:

1. **Use Alternative Provider**:
   - Some providers are region-specific
   - Configure providers available in your region

2. **Use VPN** (if permitted):
   - Connect to region where service is available
   - Check provider terms of service

3. **Contact Provider**:
   - Request regional availability
   - Check roadmap for expansion

---

## Network and Firewall Access

### Blocked by Firewall

**Error**: "Connection refused" or "Unable to connect"

**Solutions**:

1. **Check Firewall Settings**:
   ```bash
   # Windows: Check Windows Defender Firewall
   # Settings → Update & Security → Windows Security → Firewall
   
   # Linux: Check iptables/ufw
   sudo ufw status
   sudo ufw allow 5005  # Allow API port
   ```

2. **Allow Aura Through Firewall**:
   - Add exception for Aura.Api.exe
   - Allow inbound connections on port 5005
   - Allow outbound connections to provider APIs

3. **Check Corporate Firewall**:
   - Contact IT department
   - Request access to provider endpoints:
     - api.openai.com
     - api.anthropic.com
     - api.elevenlabs.io
     - api.stability.ai

### Proxy Configuration

**Error**: "Proxy authentication required"

**Solutions**:

1. **Configure Proxy**:
   ```json
   // In appsettings.json
   {
     "Network": {
       "ProxyUrl": "http://proxy.company.com:8080",
       "ProxyUsername": "username",
       "ProxyPassword": "password"
     }
   }
   ```

2. **Use System Proxy**:
   ```json
   {
     "Network": {
       "UseSystemProxy": true
     }
   }
   ```

3. **Bypass Proxy for Local**:
   ```json
   {
     "Network": {
       "ProxyBypass": ["localhost", "127.0.0.1"]
     }
   }
   ```

---

## Database Access

### Database Permission Errors

**Error**: "Database access denied" or "Cannot open database"

**Solutions**:

1. **Check Database File Permissions**:
   ```bash
   # Linux/Mac
   ls -la ~/. config/aura/aura.db
   chmod 644 ~/.config/aura/aura.db
   ```

2. **Recreate Database**:
   ```bash
   # Backup existing database
   cp ~/.config/aura/aura.db ~/.config/aura/aura.db.backup
   
   # Delete and recreate
   rm ~/.config/aura/aura.db
   # Aura will recreate on next start
   ```

3. **Check Disk Space**:
   - Database needs space to grow
   - Ensure sufficient free space on drive

### Database Lock Errors

**Error**: "Database is locked"

**Solutions**:

1. **Close Other Aura Instances**:
   - Only one Aura instance can access database
   - Close all Aura windows
   - Check task manager for lingering processes

2. **Wait for Lock Release**:
   - Lock typically releases within seconds
   - Wait and retry

3. **Force Unlock** (last resort):
   ```bash
   # Stop Aura completely
   # Delete lock file
   rm ~/.config/aura/.lock
   ```

---

## Permission Escalation

### When to Use Elevated Permissions

**Recommended**: Run Aura as regular user

**May Need Elevation For**:
- Installing FFmpeg system-wide
- Writing to system directories
- Installing as system service

### How to Run with Elevated Permissions

#### Windows
```bash
# Run as Administrator
# Right-click Aura → Run as Administrator
```

#### Linux/Mac
```bash
# Use sudo only when necessary
sudo ./Aura.Api

# Better: Change ownership
sudo chown -R $USER:$USER /opt/aura
```

### Security Best Practices

1. **Don't Run as Root/Administrator** regularly
2. **Use User Directories** for output
3. **Grant Minimal Permissions** needed
4. **Review Permissions** regularly
5. **Use Separate API Keys** for development/production

---

## Related Documentation

- [Validation Errors](validation-errors.md)
- [Provider Errors](provider-errors.md#authentication)
- [Setup Guide](../setup/installation.md)
- [General Troubleshooting](Troubleshooting.md)

## Need More Help?

If access errors persist:
1. Check system event logs for detailed errors
2. Enable debug logging:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Aura.Core.Security": "Debug"
       }
     }
   }
   ```
3. Review file system permissions
4. Check [GitHub Issues](https://github.com/Coffee285/aura-video-studio/issues)
5. Create a new issue with:
   - Exact error message
   - Operating system and version
   - User permissions
   - Steps to reproduce
