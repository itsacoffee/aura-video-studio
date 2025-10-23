# First-Run Troubleshooting FAQ

## Frequently Asked Questions

### Q: The application won't start. What should I do?

**A**: Follow these steps:

1. **Check the logs**:
   - Look in the `Logs/` directory
   - Open the most recent `aura-api-*.log` file
   - Look for ERROR or FATAL messages

2. **Common causes**:
   - Port 5005 already in use
   - Missing .NET 8 runtime
   - Antivirus blocking execution
   - Insufficient permissions

3. **Solutions**:
   - Close other applications using port 5005
   - Install .NET 8 Runtime from https://dotnet.microsoft.com/download
   - Add Aura to antivirus exclusions
   - Extract to a writable location (not Program Files)

### Q: I get "FFmpeg not found" but I already have FFmpeg installed

**A**: The application looks for FFmpeg in these locations (in order):

1. Configured path in Settings
2. `Tools/ffmpeg/` directory (portable location)
3. System PATH environment variable

**Solutions**:
- Go to Settings → Providers → FFmpeg Path
- Enter the full path to your FFmpeg executable
- Click "Test Connection" to verify
- Or, copy your FFmpeg to `Tools/ffmpeg/ffmpeg.exe`

### Q: The diagnostics say "Permission Denied" - what does this mean?

**A**: The application cannot write to required directories.

**Most common causes**:
1. Running from a CD-ROM or read-only drive
2. Extracted to Program Files (requires admin rights)
3. Antivirus blocking file creation
4. Network drive with restricted permissions

**Solutions**:
1. Extract to `C:\Aura` or `Documents\Aura`
2. Ensure you have write permissions to the location
3. Add exclusion in antivirus for the Aura folder
4. Run the self-test: Try creating a file in the folder manually

### Q: Can I use the application without internet?

**A**: Yes! The application supports full offline mode:

**What works offline**:
- ✅ RuleBased script generation (no API needed)
- ✅ Windows TTS narration (built-in)
- ✅ Local image folders as stock
- ✅ FFmpeg rendering
- ✅ All core video creation features

**What requires internet**:
- ❌ Downloading components (FFmpeg, Ollama, etc.)
- ❌ Cloud AI providers (OpenAI, ElevenLabs, etc.)
- ❌ Stock image APIs (Pexels, Unsplash, etc.)
- ❌ Stable Diffusion WebUI (if not installed)

**Offline setup**:
1. Download components on a machine with internet
2. Copy the entire Aura folder to USB drive
3. Transfer to offline machine
4. Run normally - all components are portable

### Q: Installation is stuck at "Downloading FFmpeg..."

**A**: This can happen due to:

1. **Slow internet connection**: FFmpeg is ~100-150 MB
   - Solution: Wait longer, or download manually
   
2. **Download server issues**: GitHub releases can be slow
   - Solution: Try again later, or use manual installation

3. **Antivirus blocking download**: Some AV software blocks portable executables
   - Solution: Temporarily disable AV, or download manually

4. **Network firewall**: Corporate networks may block downloads
   - Solution: Download on personal network, or request IT whitelist

**Manual installation**:
1. Download FFmpeg from https://ffmpeg.org/download.html
2. Extract `ffmpeg.exe` and `ffprobe.exe`
3. Place in `Tools/ffmpeg/` directory
4. Re-run diagnostics

### Q: I have 8 GB RAM but diagnostics say "Low RAM"

**A**: The warning appears when available RAM (not total) is low.

**Check available RAM**:
- Windows: Task Manager → Performance → Memory → Available
- Linux: `free -h` command

**Solutions**:
- Close unnecessary applications
- Restart your computer to free up memory
- Reduce browser tabs (browsers can use lots of RAM)
- The warning is advisory - you can still use the app

### Q: Can I move the application after installation?

**A**: Yes! Aura is fully portable:

1. **Close the application** completely
2. **Move the entire folder** to new location
3. **Run from new location** - it will work immediately

**What's preserved**:
- ✅ All components (FFmpeg, etc.)
- ✅ Configuration and settings
- ✅ Project files
- ✅ Downloaded models

**Note**: If you move to a different drive letter, you may need to update any absolute paths in settings.

### Q: Diagnostics pass but video rendering fails

**A**: Diagnostics check availability, not full functionality.

**Common causes**:
1. **FFmpeg version issues**: Some FFmpeg builds have bugs
2. **Codec not available**: Missing H.264 or HEVC encoder
3. **Insufficient disk space**: Rendering needs temporary space
4. **Memory exhausted**: Large videos need more RAM

**Solutions**:
1. Check render logs in `Logs/` directory for specific errors
2. Try a different FFmpeg build (official vs BtbN)
3. Reduce video resolution or length
4. Free up disk space and RAM
5. Use a different encoder (switch from NVENC to x264)

### Q: The onboarding wizard won't proceed

**A**: Check which step is failing:

**Step 1 (Welcome)**:
- No issues expected - just informational

**Step 2 (Hardware Detection)**:
- May fail on virtual machines or unusual hardware
- Solution: Use manual configuration in step 3

**Step 3 (Installation)**:
- FFmpeg installation may fail (see above)
- Ollama/SD installation optional - can skip
- Solution: Skip optional components, install FFmpeg manually

**Step 4 (Validation)**:
- Tests if components actually work
- Solution: Check logs for specific validation failures

### Q: I skipped the wizard - how do I run it again?

**A**: You can re-run onboarding anytime:

1. From Welcome page: Click "Run Onboarding" button
2. Or directly navigate to `/onboarding` in the web UI
3. Or clear the flag: Remove `hasSeenOnboarding` from localStorage

### Q: What's the difference between "Free" and "Pro" mode?

**A**: 

**Free Mode** (no API keys needed):
- RuleBased script generation
- Windows TTS narration
- Local image folders
- Ollama for local LLM (optional)
- Stable Diffusion for images (optional, NVIDIA only)

**Pro Mode** (API keys required):
- OpenAI / Gemini for better scripts
- ElevenLabs / PlayHT for natural voices
- Cloud Stable Diffusion / Runway for video
- Stock image APIs (Pexels, Unsplash)
- Mix and match free/pro per feature

### Q: Do I need an NVIDIA GPU?

**A**: No, but it helps:

**Without NVIDIA GPU**:
- ✅ All core features work
- ✅ CPU-based rendering
- ✅ Software encoders (x264)
- ✅ Cloud providers for images
- ❌ Local Stable Diffusion disabled
- ❌ No hardware encoding (slower renders)

**With NVIDIA GPU**:
- ✅ Hardware encoding (NVENC) for fast renders
- ✅ Local Stable Diffusion for image generation
- ✅ Faster processing overall
- ✅ Support for higher resolutions

### Q: My antivirus says the application is suspicious

**A**: This is a common false positive with portable applications.

**Why it happens**:
- Portable apps are unsigned executables
- Dynamic behavior looks suspicious to AV
- Bundled tools (FFmpeg) trigger heuristics

**What to do**:
1. **Verify the source**: Download only from official GitHub releases
2. **Check file hashes**: Compare SHA-256 checksums
3. **Add exclusion**: Add the Aura folder to AV exclusions
4. **Report false positive**: Submit to your AV vendor
5. **Alternative**: Use Windows Defender only (less false positives)

### Q: Can I use this on a server without a GUI?

**A**: Partially - the API can run headless:

**What works**:
- ✅ REST API endpoints
- ✅ Script generation
- ✅ Video rendering
- ✅ All backend services

**What doesn't work**:
- ❌ Web UI (no browser in headless mode)
- ❌ Windows TTS (needs audio devices)
- ❌ Hardware detection may be limited

**Headless usage**:
```bash
# Start API only
cd Aura.Api
dotnet Aura.Api.dll

# Use REST API from another machine
curl http://server:5005/api/health/ready
```

### Q: The application uses too much disk space

**A**: Check these locations:

1. **Tools/** - Installed components (~500 MB - 2 GB)
   - FFmpeg: ~100 MB
   - Ollama: ~500 MB
   - Stable Diffusion: ~4-7 GB (if installed)

2. **Projects/** - Your video projects
   - Each project: 10 MB - 1 GB
   - Includes assets, renders, intermediates

3. **Downloads/** - Downloaded components during install
   - Can be deleted after installation
   - Re-downloaded if needed

4. **Logs/** - Application logs
   - Rotates daily, keeps last 7 days
   - Typically < 10 MB

**Cleanup**:
- Delete old projects you don't need
- Delete `Downloads/` folder after setup
- Archived projects can be moved to external storage
- Logs rotate automatically

### Q: Can multiple users share one installation?

**A**: Yes, but with considerations:

**Shared installation**:
- Place Aura in a shared network location
- Each user needs read/write access
- Projects folder should be user-specific

**Recommended approach**:
1. Install shared components in common location
2. Each user has own `AuraData/` directory
3. Configure per-user paths in Settings
4. Or: Each user has complete copy (better)

**Conflicts to avoid**:
- Multiple users running API simultaneously
- Concurrent modifications to same project
- Shared API keys (use per-user keys)

### Q: How do I uninstall completely?

**A**: Aura is portable - no uninstaller needed:

1. **Close the application**
2. **Delete the Aura folder** - that's it!

**Optional cleanup**:
- Remove antivirus exclusions
- Remove from firewall rules (if added)
- That's all - no registry entries, no hidden files

### Q: I found a bug - where do I report it?

**A**: Thank you! Please report bugs on GitHub:

1. Go to https://github.com/Coffee285/aura-video-studio/issues
2. Click "New Issue"
3. Include:
   - Operating system and version
   - Steps to reproduce
   - Expected vs actual behavior
   - Relevant log snippets
   - Screenshots if applicable

**Before reporting**:
- Run diagnostics and include results
- Check existing issues for duplicates
- Try with latest version
- Include specific error messages

## Still Need Help?

If these FAQs don't resolve your issue:

1. **Check the logs**: `Logs/aura-api-*.log` often contains the answer
2. **Run diagnostics**: Click "Run Diagnostics" on Welcome page
3. **Review documentation**: Check other .md files in the docs folder
4. **Ask the community**: Create a GitHub discussion
5. **Report a bug**: If you've found an issue, create a GitHub issue

## Quick Reference

| Issue | Quick Fix |
|-------|-----------|
| Won't start | Check logs, verify .NET 8 installed |
| FFmpeg not found | Auto-install or set path in Settings |
| Permission denied | Extract to user directory, not Program Files |
| No internet | Works offline with free providers |
| Install stuck | Download components manually |
| Low RAM warning | Close other apps, restart computer |
| Move application | Just move folder - stays portable |
| Render fails | Check logs, try different encoder |
| Wizard stuck | Skip and configure manually |
| AV flags it | Add exclusion, verify SHA-256 |
| Too much disk | Delete old projects, clear downloads |
| Uninstall | Delete folder - done! |

---

**Last Updated**: 2025-10-19  
**Version**: 1.0  
**For more help**: See [FIRST_RUN_GUIDE.md](./FIRST_RUN_GUIDE.md)
