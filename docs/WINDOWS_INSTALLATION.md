# Windows Installation Guide for Aura Video Studio

This guide helps you install and run Aura Video Studio on Windows.

## System Requirements

### Minimum Requirements
- **Operating System**: Windows 10 version 1809 (October 2018 Update) or later
- **Processor**: Intel Core i3 or AMD Ryzen 3 (or equivalent)
- **Memory**: 4 GB RAM
- **Storage**: 2 GB available disk space
- **Graphics**: DirectX 11 compatible graphics card
- **Display**: 1024x768 minimum resolution

### Recommended Requirements
- **Operating System**: Windows 11 (latest version)
- **Processor**: Intel Core i5/i7 or AMD Ryzen 5/7 (or better)
- **Memory**: 8 GB RAM or more
- **Storage**: 10 GB available SSD storage
- **Graphics**: Dedicated GPU with 2GB+ VRAM
- **Display**: 1920x1080 or higher resolution

### Additional Notes
- **Internet connection**: Required for AI features (script generation, image creation, voice synthesis)
- **Video rendering**: More RAM and better CPU/GPU = faster video rendering
- **Media storage**: Additional space needed for your video projects and rendered outputs

## Download

### Official Release
Download the latest Windows installer from:
- **GitHub Releases**: https://github.com/coffee285/aura-video-studio/releases/latest

### Installation Packages

Two installation options are available:

1. **Full Installer (Recommended)**
   - File: `Aura-Video-Studio-Setup-{version}.exe`
   - Size: ~300-400 MB
   - Installs to Program Files
   - Creates Start Menu shortcuts
   - Supports auto-updates
   - Includes uninstaller

2. **Portable Version**
   - File: `Aura-Video-Studio-{version}-x64.exe`
   - Size: ~300-400 MB
   - No installation required
   - Run from any location
   - Perfect for USB drives
   - No auto-updates

## Installation Instructions

### Full Installer (Standard Installation)

#### Step 1: Download
- Go to the [releases page](https://github.com/coffee285/aura-video-studio/releases/latest)
- Download `Aura-Video-Studio-Setup-{version}.exe`
- File size will be approximately 300-400 MB

#### Step 2: Run Installer
1. **Double-click** the downloaded installer file
2. Windows may show a security warning (see "Security Warnings" section below)
3. If prompted, click "More info" → "Run anyway"

#### Step 3: User Account Control (UAC)
- Windows will ask for administrator permission
- Click **"Yes"** to allow the installer to make changes
- This is necessary to install to Program Files

#### Step 4: License Agreement
- Read the license agreement
- Check "I accept the agreement"
- Click **"Next"**

#### Step 5: Installation Location
- Default location: `C:\Program Files\Aura Video Studio`
- To change: Click "Browse" and select a different folder
- Recommended: Keep the default location
- Click **"Next"**

#### Step 6: Shortcuts
- Options available:
  - ✅ Create desktop shortcut (recommended)
  - ✅ Create Start Menu shortcut (recommended)
- Select your preferences
- Click **"Next"**

#### Step 7: Install
- Review your choices
- Click **"Install"**
- Installation will take 1-2 minutes
- Progress bar shows installation status

#### Step 8: Complete
- Installation finished!
- Options:
  - ✅ Launch Aura Video Studio (recommended)
  - View release notes
- Click **"Finish"**

### Portable Version (No Installation)

#### Step 1: Download
- Download `Aura-Video-Studio-{version}-x64.exe`
- This is the portable executable

#### Step 2: Choose Location
- Create a folder for Aura (e.g., `C:\Aura` or `D:\Portable Apps\Aura`)
- Move the executable to this folder
- Can also use USB drive or network location

#### Step 3: Run
- Double-click the executable
- First run may be slower (extracting files)
- App data stored in user profile

#### Step 4: (Optional) Create Shortcuts
- Right-click executable → "Create shortcut"
- Move shortcut to Desktop or Start Menu
- Rename to "Aura Video Studio"

## Security Warnings

### Windows SmartScreen Warning

If the installer is not code-signed, you may see:

```
Windows protected your PC
Microsoft Defender SmartScreen prevented an unrecognized app from starting.
Running this app might put your PC at risk.
```

**This is normal for unsigned or new applications.** To proceed:

1. Click **"More info"**
2. Click **"Run anyway"**
3. The installer will start normally

**Why does this happen?**
- The app is new and hasn't established reputation with Microsoft
- The developer doesn't have an Extended Validation (EV) code signing certificate
- This warning will disappear once the app is widely used

**Is it safe?**
- Yes, if downloaded from official GitHub releases
- Always verify the download URL: `github.com/coffee285/aura-video-studio`
- Check file checksums (see below)

### Antivirus Warnings

Some antivirus software may flag the installer:

**Windows Defender:**
- May quarantine the installer
- Go to Windows Security → Virus & threat protection → Protection history
- Find Aura Video Studio → Allow on device

**Third-party Antivirus (Norton, McAfee, etc.):**
- Add exception for the installer
- Whitelist the installation directory
- This is common for new applications

**Why does this happen?**
- New executables without established reputation
- False positives from heuristic analysis
- We're working on code signing to eliminate these warnings

## Verifying Your Download

To ensure you downloaded a legitimate file:

### Check SHA256 Checksum

1. Open PowerShell or Command Prompt
2. Navigate to download folder:
   ```
   cd Downloads
   ```
3. Calculate checksum:
   ```powershell
   Get-FileHash "Aura-Video-Studio-Setup-1.0.0.exe" -Algorithm SHA256
   ```
4. Compare with checksum in `checksums.txt` from releases page
5. Hashes must match exactly

### Check File Size

- Installer should be 300-400 MB
- Much smaller or larger = suspicious
- Compare with size listed on releases page

## First Run

### Initial Setup Wizard

When you first launch Aura Video Studio:

1. **Welcome Screen**
   - Introduction to Aura
   - Click "Get Started"

2. **System Check**
   - Verifying .NET runtime (bundled, should pass)
   - Checking FFmpeg (bundled, should pass)
   - Testing AI connectivity (requires internet)

3. **API Keys Setup**
   - Configure AI providers (OpenAI, Anthropic, etc.)
   - Add API keys for services you plan to use
   - Can skip and configure later

4. **Preferences**
   - Choose default output folder
   - Select video quality presets
   - Configure auto-save settings

5. **Sample Projects**
   - Option to load sample projects
   - Learn the interface with examples
   - Can skip for now

### What Happens on First Run?

- **Database Creation**: SQLite database created in `%APPDATA%\AuraVideoStudio\`
- **Log Files**: Created in `%APPDATA%\AuraVideoStudio\logs\`
- **Configuration**: Settings saved to `%APPDATA%\AuraVideoStudio\config\`
- **Backend Server**: Automatically starts on random port (5000-5100)
- **Internet Check**: Tests connectivity for AI features

## Using Aura Video Studio

### Launching the App

**After Installation:**
- **Start Menu**: Search for "Aura Video Studio"
- **Desktop**: Double-click "Aura Video Studio" shortcut
- **Directly**: Navigate to installation folder and run `Aura Video Studio.exe`

**Portable:**
- Double-click the portable executable

### System Tray

- Aura runs in system tray when minimized
- Look for Aura icon in notification area
- Right-click tray icon for options:
  - Show/Hide main window
  - Check for updates
  - View logs
  - Quit application

### Creating Your First Video

1. Click "New Project"
2. Choose a template or start blank
3. Enter script or generate with AI
4. Customize visuals, music, voice
5. Click "Render" to generate video
6. Wait for rendering (1-5 minutes depending on length)
7. Preview and export

See the [User Guide](USER_GUIDE.md) for detailed instructions.

## Uninstalling

### Full Installation

**Method 1: Windows Settings**
1. Open Windows Settings (Win + I)
2. Go to Apps → Apps & features
3. Search for "Aura Video Studio"
4. Click → Uninstall
5. Follow prompts

**Method 2: Control Panel**
1. Open Control Panel
2. Programs → Programs and Features
3. Find "Aura Video Studio"
4. Right-click → Uninstall
5. Follow prompts

**Method 3: Uninstaller**
1. Open Start Menu
2. Find "Aura Video Studio" folder
3. Click "Uninstall Aura Video Studio"
4. Follow prompts

**What Gets Removed:**
- ✅ Application files from Program Files
- ✅ Desktop and Start Menu shortcuts
- ✅ Registry entries
- ❌ User data (projects, settings, database)

**To Remove User Data:**
- Delete: `%APPDATA%\AuraVideoStudio\`
- Delete: `%LOCALAPPDATA%\AuraVideoStudio\`
- Delete: Your videos output folder (if you want to remove rendered videos)

### Portable Version

1. Close Aura Video Studio
2. Delete the executable file
3. Delete any shortcuts you created
4. (Optional) Delete user data from `%APPDATA%\AuraVideoStudio\`

## Troubleshooting

### App Won't Start

**Problem**: Double-clicking does nothing or app crashes immediately

**Solutions:**
1. **Check Windows version**: Must be Windows 10 1809 or later
   - Open Settings → System → About
   - Check "Version" (must be 1809+)
2. **Run as Administrator**: Right-click app → "Run as administrator"
3. **Check antivirus**: May be blocking the app
4. **View logs**: Check `%APPDATA%\AuraVideoStudio\logs\`
5. **Reinstall**: Uninstall and reinstall from fresh download

### Backend Won't Start

**Problem**: App shows "Backend failed to start" error

**Solutions:**
1. **Port conflict**: Another app using ports 5000-5100
   - Close other applications
   - Restart Windows
2. **Firewall**: Windows Firewall blocking backend
   - Allow Aura through firewall
   - Windows Security → Firewall → Allow an app
3. **Antivirus**: Blocking backend executable
   - Add exception for Aura.Api.exe
4. **Corrupted files**: Reinstall application

### FFmpeg Not Working

**Problem**: Videos won't render or FFmpeg errors

**Solutions:**
1. **Bundled FFmpeg**: Should be included in installation
2. **Check location**: Look in installation folder under `resources\ffmpeg\win-x64\bin\`
3. **Manual download**: Download FFmpeg from https://ffmpeg.org/
4. **Reinstall**: Fresh installation should fix missing FFmpeg

### Can't Connect to AI Services

**Problem**: AI features not working (script generation, image creation, etc.)

**Solutions:**
1. **Check internet**: Ensure internet connection
2. **API keys**: Verify API keys are correct in Settings
3. **Firewall**: Ensure app can access internet
4. **Proxy**: Configure proxy settings if behind corporate firewall

### Slow Performance

**Problem**: App is sluggish or video rendering is very slow

**Solutions:**
1. **Hardware**: Check if PC meets recommended requirements
2. **Disk space**: Ensure at least 10GB free space
3. **Background apps**: Close unnecessary programs
4. **Antivirus**: Real-time scanning may slow down rendering
5. **GPU acceleration**: Enable in Settings if available

### High Memory Usage

**Problem**: Aura Video Studio using too much RAM

**Solutions:**
1. **Expected**: Video editing requires significant memory
2. **Close projects**: Don't keep multiple projects open
3. **Clear cache**: Settings → Clear cache
4. **Restart app**: Close and reopen to free memory
5. **Upgrade RAM**: Consider 8GB+ for better experience

## Getting Help

### Documentation
- **User Guide**: Comprehensive feature documentation
- **FAQ**: Common questions and answers
- **Video Tutorials**: YouTube channel (coming soon)

### Support Channels
- **GitHub Issues**: Report bugs at https://github.com/coffee285/aura-video-studio/issues
- **Discussions**: Ask questions at https://github.com/coffee285/aura-video-studio/discussions
- **Email**: support@aura-video-studio.com

### Before Requesting Support

Please provide:
1. **Windows version**: Settings → System → About
2. **Aura version**: Help → About
3. **Error message**: Exact text of error
4. **Log files**: From `%APPDATA%\AuraVideoStudio\logs\`
5. **Steps to reproduce**: What you were doing when issue occurred

## Updates

### Automatic Updates (Full Installation Only)

- Aura checks for updates on startup (if enabled)
- Notification appears when update available
- Click "Download Update"
- Update installs on next restart

### Manual Updates

1. Download latest installer from releases page
2. Run installer (will update existing installation)
3. Settings and projects are preserved

### Portable Version Updates

1. Download latest portable executable
2. Replace old executable with new one
3. User data is preserved (stored separately)

## Advanced Topics

### Command-Line Options

Run from Command Prompt or PowerShell:

```bash
# Open in development mode
"Aura Video Studio.exe" --dev

# Specify custom data directory
"Aura Video Studio.exe" --data-dir="D:\Aura Data"

# Disable GPU acceleration
"Aura Video Studio.exe" --disable-gpu

# Show version and exit
"Aura Video Studio.exe" --version
```

### Configuration Files

Located in `%APPDATA%\AuraVideoStudio\`:
- `config.json` - User preferences
- `aura.db` - Project database
- `logs\` - Application logs

### Multi-User Setup

Each Windows user has separate:
- Settings and preferences
- Project database
- Rendered videos (by default)

To share projects:
- Export project files (.aura)
- Share via network drive or cloud storage
- Import in other user account

## Frequently Asked Questions

**Q: Do I need to install .NET or FFmpeg separately?**  
A: No, everything is bundled in the installer. Just install and run.

**Q: Can I install on multiple computers?**  
A: Yes, install on as many computers as you own.

**Q: Does it work offline?**  
A: Yes, but AI features require internet connection.

**Q: How do I backup my projects?**  
A: Copy `%APPDATA%\AuraVideoStudio\aura.db` and your video files.

**Q: Can I move the installation to another drive?**  
A: Yes, uninstall and reinstall to desired location. User data is preserved.

**Q: Is my data sent to any servers?**  
A: Only API requests to AI providers you configure. No telemetry by default.

## License

Aura Video Studio is open source software licensed under the MIT License.
See LICENSE file for details.

---

**Last Updated**: 2025-11-10  
**Version**: 1.0.0  
**For**: Windows 10/11
