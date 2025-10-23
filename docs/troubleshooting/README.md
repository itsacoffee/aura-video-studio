# Troubleshooting

Common issues and solutions for Aura Video Studio.

## 📑 Quick Links

- **[Detailed Troubleshooting Guide](./Troubleshooting.md)** - Comprehensive troubleshooting documentation
- **[First Run FAQ](../getting-started/FIRST_RUN_FAQ.md)** - Common questions from new users
- **[Installation Issues](../getting-started/INSTALLATION.md)** - Installation-specific problems

## 🔍 Quick Diagnostics

### Health Check

Run the built-in health check:

```bash
# Via CLI
aura health

# Via API
curl http://localhost:5005/healthz
```

### System Information

Check your system configuration:

```bash
# .NET version
dotnet --version

# FFmpeg availability
ffmpeg -version

# System capabilities
aura capabilities
```

### Log Files

Check logs for detailed error information:

```
logs/
├── aura-YYYY-MM-DD.log          # Application logs
├── render/                       # Render logs
│   └── render-YYYYMMDD-HHMMSS.log
└── tools/                        # Tool download logs
```

## ⚠️ Common Issues

### Installation & Setup

#### Issue: "The application failed to start"

**Symptoms**: Error on launch, crash immediately

**Solutions**:
1. Verify .NET 8.0 Runtime is installed:
   ```bash
   dotnet --version
   ```
   Should show 8.0.x or 9.0.x

2. Check for missing dependencies:
   ```bash
   aura check-dependencies
   ```

3. Review logs in `logs/` directory

4. Try portable mode (no installation required)

#### Issue: "First run wizard doesn't appear"

**Symptoms**: Application starts but no setup wizard

**Solutions**:
1. Delete settings file to trigger first run:
   ```bash
   # Windows
   del %APPDATA%\AuraVideoStudio\settings.json
   
   # Linux
   rm ~/.config/aura-video-studio/settings.json
   ```

2. Launch application again

3. If still not showing, check logs for errors

#### Issue: "FFmpeg not found"

**Symptoms**: Rendering fails with "FFmpeg not detected"

**Solutions**:
1. Download FFmpeg via Download Center in app

2. Or manually install:
   - Windows: Download from ffmpeg.org, add to PATH
   - Linux: `sudo apt install ffmpeg` or equivalent

3. Verify installation:
   ```bash
   ffmpeg -version
   ```

4. Restart application

### Video Generation

#### Issue: "Script generation fails"

**Symptoms**: Error when generating script from brief

**Solutions**:
1. Check provider configuration:
   - Verify API keys are correct
   - Test API key directly with provider
   - Check API quota/limits

2. Try fallback provider:
   - Switch to RuleBasedLlmProvider (no API key)
   - Test with simple brief

3. Check network connectivity:
   ```bash
   ping api.openai.com
   ```

4. Review logs for specific error

#### Issue: "TTS synthesis fails"

**Symptoms**: Script generates but narration creation fails

**Solutions**:
1. For Windows SAPI:
   - Verify voices are installed
   - Check Windows Settings → Time & Language → Speech

2. For cloud TTS:
   - Verify API key and quota
   - Check network connectivity
   - Try different voice

3. Check audio output:
   - Ensure audio drivers are working
   - Test with different output device

#### Issue: "Visual assets not found"

**Symptoms**: Timeline shows missing images/videos

**Solutions**:
1. Check internet connectivity (for stock providers)

2. Verify search queries are relevant:
   - Simplify query
   - Try different keywords
   - Check provider status

3. Use alternative providers:
   - Switch between Pexels, Pixabay, Unsplash
   - Try Stable Diffusion if available

4. Upload custom assets manually

#### Issue: "Rendering fails or produces corrupt video"

**Symptoms**: Render process fails or video won't play

**Solutions**:
1. Verify all assets exist:
   ```bash
   # Check project directory
   dir /s *.jpg *.png *.mp3 *.wav
   ```

2. Check disk space:
   - Need 2-3x final video size
   - Clean temporary files

3. Try different encoder:
   - H.264 (most compatible)
   - H.265 (better compression)
   - VP9 (for web)

4. Reduce complexity:
   - Lower resolution (1080p → 720p)
   - Simpler transitions
   - Fewer effects

5. Check FFmpeg logs in `logs/render/`

### Performance Issues

#### Issue: "Video generation is very slow"

**Symptoms**: Takes much longer than expected

**Causes & Solutions**:

1. **Slow script generation**:
   - Use faster LLM model (gpt-3.5-turbo instead of gpt-4)
   - Reduce script length
   - Use template-based provider

2. **Slow asset download**:
   - Pre-download and cache assets
   - Use local asset library
   - Check internet speed

3. **Slow rendering**:
   - Enable GPU acceleration
   - Lower resolution/quality
   - Use draft preset for testing
   - Close other applications

4. **System resources**:
   - Check CPU/RAM usage
   - Close unnecessary programs
   - Upgrade hardware if needed

#### Issue: "High memory usage"

**Symptoms**: Application uses excessive RAM

**Solutions**:
1. Clear cache:
   ```bash
   aura clear-cache
   ```

2. Reduce concurrent operations

3. Use lower resolution assets

4. Restart application periodically

5. Increase system RAM if persistently high

### API & Integration

#### Issue: "API endpoints return errors"

**Symptoms**: REST API calls fail

**Solutions**:
1. Verify API is running:
   ```bash
   curl http://localhost:5005/healthz
   ```

2. Check port conflicts:
   - Default port 5005 may be in use
   - Change port in appsettings.json

3. Review API logs

4. Test with simple request:
   ```bash
   curl -X POST http://localhost:5005/api/v1/script \
     -H "Content-Type: application/json" \
     -d '{"brief": {"title": "Test", "description": "Test"}}'
   ```

#### Issue: "CORS errors in web UI"

**Symptoms**: Browser console shows CORS errors

**Solutions**:
1. Check proxy configuration in `vite.config.ts`

2. Verify API CORS settings in `Aura.Api/Program.cs`

3. Use same origin for API and Web UI

4. Clear browser cache

### Provider Issues

#### Issue: "OpenAI API errors"

**Common Errors**:

**401 Unauthorized**:
- Invalid API key
- Check key in settings
- Verify key at platform.openai.com

**429 Rate Limit**:
- Too many requests
- Wait and retry
- Upgrade API plan
- Use exponential backoff

**500 Internal Error**:
- OpenAI service issue
- Check status.openai.com
- Retry after delay
- Use fallback provider

#### Issue: "ElevenLabs quota exceeded"

**Symptoms**: TTS fails with quota error

**Solutions**:
1. Check usage at elevenlabs.io

2. Upgrade plan if needed

3. Switch to free provider temporarily:
   - Windows SAPI
   - Azure TTS (has free tier)

4. Optimize text:
   - Remove unnecessary words
   - Use shorter narration

#### Issue: "Stable Diffusion not working"

**Symptoms**: Image generation fails

**GPU Requirements**:
- NVIDIA GPU required
- 6GB+ VRAM recommended
- Updated drivers

**Solutions**:
1. Check GPU detection:
   ```bash
   aura capabilities
   ```

2. Update GPU drivers

3. Reduce image resolution

4. Use cloud API instead:
   - Stability AI API
   - Replicate.com

5. Fall back to stock images

## 🔧 Advanced Troubleshooting

### Debugging Mode

Enable verbose logging:

```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Aura": "Trace"
    }
  }
}
```

### Network Diagnostics

Test provider connectivity:

```bash
# Test OpenAI
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer YOUR_API_KEY"

# Test ElevenLabs
curl https://api.elevenlabs.io/v1/voices \
  -H "xi-api-key: YOUR_API_KEY"
```

### Database Issues

Reset database (loses settings):

```bash
# Backup first
cp aura.db aura.db.backup

# Reset
rm aura.db

# Restart app to recreate
```

### Clean Reinstall

Complete reset:

```bash
# 1. Backup projects
cp -r Renders/ Renders.backup/

# 2. Uninstall/delete app folder

# 3. Remove settings
rm -rf ~/.config/aura-video-studio/  # Linux
# or
rmdir /s %APPDATA%\AuraVideoStudio  # Windows

# 4. Reinstall

# 5. Restore projects
```

## 📊 Diagnostic Tools

### Built-in Diagnostics

```bash
# System check
aura diagnose

# Provider test
aura test-providers

# Dependency check
aura check-dependencies

# Performance benchmark
aura benchmark
```

### External Tools

- **Process Monitor** (Windows): Track file/registry access
- **Wireshark**: Analyze network traffic
- **htop/Task Manager**: Monitor resource usage
- **GPU-Z**: Check GPU status

## 📞 Getting Help

If you can't resolve the issue:

### 1. Check Documentation

- [Getting Started](../getting-started/README.md)
- [Features](../features/README.md)
- [API Reference](../api/README.md)
- [FAQ](../getting-started/FIRST_RUN_FAQ.md)

### 2. Search Existing Issues

Search [GitHub Issues](https://github.com/Saiyan9001/aura-video-studio/issues) for similar problems.

### 3. Report New Issue

If not found, create new issue with:

**System Information**:
```bash
aura system-info > system.txt
```

**Logs**:
- Recent logs from `logs/` directory
- Specific error messages
- Stack traces if available

**Steps to Reproduce**:
1. Exact steps taken
2. Expected behavior
3. Actual behavior
4. Screenshots if applicable

**Configuration**:
- Provider settings (redact API keys!)
- Relevant settings
- Any customizations

### 4. Community Support

Ask in [GitHub Discussions](https://github.com/Saiyan9001/aura-video-studio/discussions):
- General questions
- How-to requests
- Feature discussions
- Share solutions

## 📚 Related Documentation

- **[Installation Guide](../getting-started/INSTALLATION.md)** - Setup and installation
- **[First Run FAQ](../getting-started/FIRST_RUN_FAQ.md)** - Common beginner questions
- **[Detailed Troubleshooting](./Troubleshooting.md)** - In-depth troubleshooting guide
- **[API Errors](../api/errors.md)** - API error reference

---

**Still stuck?** Don't hesitate to ask for help in [GitHub Discussions](https://github.com/Saiyan9001/aura-video-studio/discussions)!
