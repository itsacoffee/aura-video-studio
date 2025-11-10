# Resource Errors

This guide helps you troubleshoot resource-related errors (disk space, memory, permissions) in Aura Video Studio.

## Quick Navigation

- [Disk Space Issues](#disk-space-issues)
- [Memory Issues](#memory-issues)
- [File Permission Errors](#file-permission-errors)
- [CPU and Performance](#cpu-and-performance)

---

## Disk Space Issues

### Insufficient Disk Space

**Error**: "Insufficient disk space" or "Out of disk space"

**Error Code**: **OutOfDiskSpace**

**Symptoms**:
- Export fails mid-process
- Cannot save projects
- Temporary file creation fails
- Application becomes unstable

### How Much Space Do You Need?

**Minimum Requirements**:
- Application: 500 MB
- FFmpeg: 100-200 MB
- Cache: 1-5 GB
- Each video project: 100 MB - 10 GB

**Estimated Space by Video Length**:

| Resolution | 5 min | 10 min | 30 min |
|-----------|-------|--------|--------|
| 720p 30fps | 200 MB | 400 MB | 1.2 GB |
| 1080p 30fps | 500 MB | 1 GB | 3 GB |
| 1080p 60fps | 800 MB | 1.6 GB | 4.8 GB |
| 4K 30fps | 2 GB | 4 GB | 12 GB |
| 4K 60fps | 4 GB | 8 GB | 24 GB |

**Plus**:
- Source media (images, audio): 100-500 MB per video
- Temporary files: 2-3x output size
- Cache: 1-5 GB

### Check Available Disk Space

**Windows**:
```powershell
# Check all drives
Get-PSDrive -PSProvider FileSystem

# Check specific drive
(Get-PSDrive C).Free / 1GB
# Shows free space in GB
```

**Linux/Mac**:
```bash
# Check all drives
df -h

# Check specific directory
df -h /home/user/Videos
```

### Solutions

#### 1. Free Up Disk Space

**Delete Temporary Files**:

**Windows**:
```powershell
# Clean Windows temp
cleanmgr

# Clean Aura temp
Remove-Item -Recurse "$env:TEMP\Aura\*"
Remove-Item -Recurse "$env:APPDATA\Aura\temp\*"
```

**Linux/Mac**:
```bash
# Clean system temp
rm -rf /tmp/aura_*

# Clean Aura cache
rm -rf ~/.config/aura/cache/*
rm -rf ~/.config/aura/temp/*
```

**Delete Old Projects**:
1. Go to Projects view
2. Select old/unused projects
3. Delete (this moves to Recycle Bin)
4. Empty Recycle Bin

**Delete Old Exports**:
```bash
# Remove old output files
rm ~/Videos/Aura/output_*.mp4
```

#### 2. Clear Aura Cache

**Via UI**:
1. Settings → Storage
2. Click "Clear Cache"
3. Confirm

**Manually**:

**Windows**:
```powershell
Remove-Item -Recurse "$env:APPDATA\Aura\cache\*"
```

**Linux/Mac**:
```bash
rm -rf ~/.config/aura/cache/*
```

**Safe to delete**:
- Thumbnails cache
- Preview cache
- Provider response cache
- Temporary renders

**Do NOT delete**:
- Projects database (`aura.db`)
- Configuration files (`appsettings.json`)
- API keys

#### 3. Change Output Location

**Move to Drive with More Space**:

1. **Settings → Export → Output Directory**
2. **Choose drive with more space**
   ```
   Windows: D:\Videos\Aura
   Linux: /mnt/storage/aura
   Mac: /Volumes/External/Aura
   ```

3. **Configure in appsettings.json**:
   ```json
   {
     "Rendering": {
       "OutputDirectory": "D:\\Videos\\Aura",
       "TempDirectory": "D:\\Temp\\Aura"
     }
   }
   ```

#### 4. Reduce Export Quality (Temporarily)

Lower settings = smaller files:

```json
{
  "Rendering": {
    "Resolution": "1280x720",  // Instead of 1920x1080
    "VideoBitrate": "2M",      // Instead of 5M
    "CRF": 28                  // Instead of 23 (higher = more compression)
  }
}
```

#### 5. Clean Up After Each Project

**Enable Automatic Cleanup**:
```json
{
  "FileSystem": {
    "AutoCleanup": true,
    "CleanupAfterDays": 7,  // Delete temp files after 7 days
    "KeepOutputFiles": true  // But keep final exports
  }
}
```

**Manual Cleanup**:
```bash
# After project completion, delete temp files
rm -rf ~/.config/aura/temp/project-*
```

---

## Memory Issues

### Out of Memory

**Error**: "Out of memory" or "Cannot allocate memory"

**Symptoms**:
- Application crashes
- Slow performance
- Rendering fails
- System becomes unresponsive

### Check Memory Usage

**Windows**:
```powershell
# Task Manager: Ctrl+Shift+Esc
# Or PowerShell:
Get-Process -Name Aura.Api | Select-Object Name, @{Name="Memory (MB)";Expression={$_.WS / 1MB}}
```

**Linux**:
```bash
# Check process memory
ps aux | grep Aura

# Check system memory
free -h
```

**Mac**:
```bash
# Activity Monitor or:
top -l 1 | grep -E "^CPU|^Phys"
```

### Memory Requirements

**Minimum**: 4 GB RAM
**Recommended**: 8 GB RAM
**For 4K**: 16 GB RAM

**Memory Usage by Operation**:
- Application baseline: 200-500 MB
- Video generation: 1-2 GB
- Image generation (local): 2-8 GB (GPU VRAM)
- Video rendering: 2-4 GB
- 4K rendering: 4-8 GB

### Solutions

#### 1. Close Unnecessary Applications

Close memory-intensive applications:
- Web browsers (especially with many tabs)
- Other video editors
- Games
- Virtual machines
- Large applications

#### 2. Reduce Aura Memory Usage

**Configure memory limits**:
```json
{
  "Performance": {
    "MaxMemoryMB": 2048,  // Limit Aura to 2GB
    "EnableMemoryOptimization": true
  },
  "Rendering": {
    "MaxCacheSizeMB": 512,  // Reduce cache
    "ProcessChunkSize": 10   // Process fewer frames at once
  }
}
```

#### 3. Process in Smaller Chunks

**For long videos**:
1. Split into smaller segments (5-10 minutes each)
2. Render each segment separately
3. Concatenate using FFmpeg:
   ```bash
   # Create list file
   echo "file 'segment1.mp4'" > list.txt
   echo "file 'segment2.mp4'" >> list.txt
   echo "file 'segment3.mp4'" >> list.txt
   
   # Concatenate
   ffmpeg -f concat -safe 0 -i list.txt -c copy final.mp4
   ```

#### 4. Use Lower Resolution for Preview

**Preview at lower resolution**:
```json
{
  "Preview": {
    "Resolution": "1280x720",  // Preview at 720p
    "Quality": "medium"
  },
  "Rendering": {
    "Resolution": "1920x1080"  // Final render at 1080p
  }
}
```

#### 5. Disable Memory-Intensive Features

**Temporarily disable**:
```json
{
  "Features": {
    "EnablePreviewCache": false,  // Disable preview caching
    "EnableLivePreview": false,   // Disable live preview
    "EnableAutoSave": false       // Reduce autosave frequency
  }
}
```

#### 6. Increase System Virtual Memory (Swap)

**Windows**:
1. System Properties → Advanced → Performance Settings
2. Advanced → Virtual Memory → Change
3. Increase page file size

**Linux**:
```bash
# Check current swap
free -h

# Add swap file (4GB example)
sudo fallocate -l 4G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile

# Make permanent
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

**Mac**:
Mac manages swap automatically, but ensure SSD has free space.

---

## File Permission Errors

### Output Directory Not Writable

**Error**: "Output directory is not writable" or "Permission denied"

**Error Code**: **OutputDirectoryNotWritable**

See detailed solutions in [Access Errors - File System Permissions](access-errors.md#file-system-permissions)

### Quick Solutions

#### Windows

1. **Change output to user directory**:
   ```
   C:\Users\YourName\Videos\Aura
   ```

2. **Grant permissions**:
   - Right-click folder → Properties → Security
   - Edit → Select your user → Check "Full control"

#### Linux/Mac

1. **Change output to home directory**:
   ```bash
   ~/Videos/Aura
   ```

2. **Grant permissions**:
   ```bash
   chmod 755 ~/Videos/Aura
   chown $USER:$USER ~/Videos/Aura
   ```

### Cannot Access Temp Directory

**Error**: "Cannot create temporary files"

**Solutions**:

1. **Set custom temp directory**:
   ```json
   {
     "FileSystem": {
       "TempDirectory": "C:\\Users\\YourName\\AppData\\Local\\Temp\\Aura"
     }
   }
   ```

2. **Clear temp directory**:
   ```bash
   # Windows
   del /q "%TEMP%\Aura\*"
   
   # Linux/Mac
   rm -rf /tmp/aura_*
   ```

3. **Ensure sufficient temp space**:
   - Temp drive needs 5-10 GB free
   - Consider moving temp to larger drive

---

## CPU and Performance

### High CPU Usage

**Expected CPU Usage**:
- Idle: 1-5%
- Video generation: 20-50%
- Rendering: 80-100%

**If constantly high**:

#### 1. Check for Background Tasks

```bash
# Windows: Task Manager
# Linux: top or htop
# Mac: Activity Monitor

# Find CPU-intensive processes
```

#### 2. Limit Thread Usage

```json
{
  "Performance": {
    "MaxThreads": 4,  // Instead of using all cores
    "ThreadPriority": "Normal"  // Instead of "High"
  },
  "Rendering": {
    "Threads": 4
  }
}
```

#### 3. Use Hardware Acceleration

Offload work to GPU:
```json
{
  "Rendering": {
    "UseHardwareAcceleration": true,
    "HardwareEncoder": "h264_nvenc"
  }
}
```

#### 4. Adjust Encoding Settings

```json
{
  "Rendering": {
    "Preset": "faster",  // Less CPU-intensive
    "Priority": "normal"  // Don't monopolize CPU
  }
}
```

### System Overheating

**Symptoms**:
- Loud fan noise
- Performance throttling
- Unexpected shutdowns

**Solutions**:

1. **Monitor temperatures**:
   ```bash
   # Linux
   sensors
   
   # Windows: Use HWMonitor or similar
   # Mac: Use iStat Menus or similar
   ```

2. **Reduce load**:
   ```json
   {
     "Performance": {
       "ThermalProtection": true,
       "MaxCPUUsage": 80  // Limit to 80% to reduce heat
     }
   }
   ```

3. **Improve cooling**:
   - Clean dust from vents
   - Ensure proper ventilation
   - Use laptop cooling pad
   - Check thermal paste (desktops)

4. **Render during cooler times**:
   - Schedule renders overnight
   - Avoid rendering in hot environments

---

## Storage Performance

### Slow Disk Performance

**Symptoms**:
- Slow exports
- Lag when previewing
- Long save times

**Solutions**:

#### 1. Use SSD Instead of HDD

**Check disk type**:
```bash
# Windows
Get-PhysicalDisk | Select-Object MediaType, FriendlyName

# Linux
lsblk -d -o name,rota
# 1 = HDD, 0 = SSD
```

**Move Aura to SSD**:
```json
{
  "FileSystem": {
    "ProjectDirectory": "C:\\SSD\\Aura\\Projects",  // SSD
    "OutputDirectory": "D:\\HDD\\Aura\\Output",     // HDD is OK for final output
    "TempDirectory": "C:\\SSD\\Aura\\Temp"          // SSD for temp
  }
}
```

#### 2. Optimize Disk

**Windows**:
```powershell
# Optimize drives
Optimize-Volume -DriveLetter C -Defrag  # HDD
Optimize-Volume -DriveLetter C -ReTrim  # SSD
```

**Linux**:
```bash
# Enable TRIM for SSD
sudo fstrim -v /

# Or enable automatic TRIM
sudo systemctl enable fstrim.timer
```

#### 3. Disable Antivirus Scanning

Add Aura directories to antivirus exclusions:
- Aura installation directory
- Project directory
- Output directory
- Temp directory

**Windows Defender**:
1. Settings → Update & Security → Windows Security
2. Virus & Threat Protection → Manage Settings
3. Add or remove exclusions

---

## Network Resource Issues

### Slow Provider API Responses

**Not technically a local resource issue, but affects performance**

**Solutions**:

1. **Enable caching**:
   ```json
   {
     "Providers": {
       "EnableResponseCache": true,
       "CacheDurationMinutes": 60
     }
   }
   ```

2. **Use multiple providers**:
   - Distribute load
   - Faster response times

3. **Check network speed**:
   ```bash
   # Test internet speed
   # Use speedtest.net or:
   curl -s https://raw.githubusercontent.com/sivel/speedtest-cli/master/speedtest.py | python -
   ```

---

## Monitoring and Alerts

### Enable Resource Monitoring

```json
{
  "Monitoring": {
    "EnableResourceMonitoring": true,
    "AlertOnLowDiskSpace": true,
    "LowDiskSpaceThresholdGB": 5,
    "AlertOnHighMemory": true,
    "HighMemoryThresholdPercent": 90
  }
}
```

### Resource Dashboard

View resource usage:
1. Go to Settings → Diagnostics
2. View "Resource Usage" tab
3. See real-time:
   - Disk space
   - Memory usage
   - CPU usage
   - Network usage

---

## Related Documentation

- [System Requirements](../setup/system-requirements.md)
- [FFmpeg Errors](ffmpeg-errors.md)
- [Access Errors](access-errors.md#file-system-permissions)
- [General Troubleshooting](Troubleshooting.md)

## Need More Help?

If resource errors persist:
1. Check system resources using OS tools
2. Enable resource monitoring in Aura
3. Review logs for specific resource errors
4. Check [GitHub Issues](https://github.com/Coffee285/aura-video-studio/issues)
5. Create new issue with:
   - System specifications
   - Available resources (disk, RAM, CPU)
   - Error message
   - What operation was being performed
   - Resource usage at time of error
