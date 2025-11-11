# Local Storage Architecture

## ⚠️ CRITICAL: Local-Only Application

**Aura Video Studio is a LOCAL desktop application for home computers.**

- ✅ All storage is LOCAL to the user's machine
- ✅ Can connect to external APIs (LLM, TTS, Image providers)
- ❌ NO cloud storage (Azure, AWS, Google Cloud) should be implemented
- ❌ NO server-side data storage beyond the local API server

## Storage Architecture

### What IS Implemented (Correct)

1. **Local File Storage** (`LocalStorageService.cs`)
   - Stores all media files on local disk
   - Default location: `%APPDATA%/Aura/Media` (Windows) or `~/.local/share/Aura/Media` (Linux/Mac)
   - Handles video outputs, audio files, images, project files

2. **Enhanced Local Storage** (`EnhancedLocalStorageService.cs`)
   - Advanced local storage with caching
   - Local thumbnails and previews
   - Metadata stored in local SQLite database

3. **Secure Local Storage** (`SecureStorageService.cs`)
   - Stores API keys and sensitive data using OS-level encryption
   - Windows: DPAPI
   - Linux/Mac: keyring/keychain

4. **SQLite Database** (Entity Framework)
   - All project state, configuration, and metadata stored locally
   - Database file: `%APPDATA%/Aura/aura.db`

### What Should NOT Be Implemented (Remove/Deprecate)

The following cloud storage implementations exist but should be removed or marked as deprecated:

1. ❌ `AzureBlobStorageService.cs` - Azure cloud storage (REMOVE)
2. ❌ `AzureBlobStorageProvider.cs` - Azure provider (REMOVE)
3. ❌ `AwsS3StorageProvider.cs` - AWS S3 storage (REMOVE)
4. ❌ `GoogleCloudStorageProvider.cs` - Google Cloud storage (REMOVE)
5. ❌ `CloudStorageProviderFactory.cs` - Cloud provider factory (REMOVE)
6. ❌ `ICloudStorageProvider.cs` - Cloud provider interface (REMOVE)
7. ❌ `CloudStorageSettings.cs` - Cloud settings model (REMOVE)

## External API Connections (Allowed)

The application CAN connect to external APIs for AI services:

### LLM Providers
- OpenAI (GPT-4, GPT-3.5)
- Anthropic (Claude)
- Google Gemini
- Ollama (local or remote server)
- Azure OpenAI (API only, not storage)

### TTS Providers
- ElevenLabs API
- PlayHT API
- Windows SAPI (local)
- Piper (local)
- Mimic3 (local)

### Image Providers
- Stable Diffusion WebUI (local or remote)
- Replicate API
- Stock image APIs

## Data Flow

```
User Input (Brief, Settings)
    ↓
Local API Server (ASP.NET Core)
    ↓
External APIs (LLM, TTS, Images) ← API calls only, no storage
    ↓
Generated Assets (audio, images, video)
    ↓
Local File System Storage
    ↓
SQLite Database (metadata)
```

## Storage Locations

### Windows
- **Media Files**: `%APPDATA%\Aura\Media`
- **Database**: `%APPDATA%\Aura\aura.db`
- **Temp Files**: `%TEMP%\Aura`
- **Logs**: `%APPDATA%\Aura\Logs`
- **Config**: `%APPDATA%\Aura\appsettings.json`

### Linux
- **Media Files**: `~/.local/share/Aura/Media`
- **Database**: `~/.local/share/Aura/aura.db`
- **Temp Files**: `/tmp/Aura`
- **Logs**: `~/.local/share/Aura/Logs`
- **Config**: `~/.config/Aura/appsettings.json`

### macOS
- **Media Files**: `~/Library/Application Support/Aura/Media`
- **Database**: `~/Library/Application Support/Aura/aura.db`
- **Temp Files**: `/tmp/Aura`
- **Logs**: `~/Library/Logs/Aura`
- **Config**: `~/Library/Application Support/Aura/appsettings.json`

## Implementation Guidelines

### DO:
- ✅ Use `LocalStorageService` for all file operations
- ✅ Use `SecureStorageService` for sensitive data (API keys)
- ✅ Use SQLite for metadata and project state
- ✅ Store everything on the local file system
- ✅ Provide export/import functionality for user data portability
- ✅ Clean up temporary files after processing
- ✅ Respect user's disk space limitations

### DON'T:
- ❌ Implement any cloud storage uploads
- ❌ Store user data on remote servers (except temporary API processing)
- ❌ Require internet for core functionality (except API providers)
- ❌ Add dependencies on cloud storage SDKs
- ❌ Create accounts or server-side user profiles
- ❌ Track or collect user data remotely

## Migration Path

For existing cloud storage code:

1. **Mark as Obsolete**: Add `[Obsolete]` attributes with messages
2. **Document Removal**: Update this file and PR descriptions
3. **Create Issues**: Track removal of cloud storage features
4. **Test Without**: Ensure app works without cloud dependencies
5. **Remove Code**: Delete cloud storage implementations
6. **Update Tests**: Remove cloud storage test dependencies

## Privacy & Security

As a local application:
- User data never leaves their machine (except API requests)
- No telemetry or analytics sent to remote servers
- API keys stored securely using OS-level encryption
- User has full control over their data
- No vendor lock-in
- Fully functional offline (with local providers)

## Future Considerations

If cloud sync is ever needed (optional feature):
- Must be OPT-IN only
- User controls what gets synced
- Encryption at rest and in transit
- Support for self-hosted sync servers
- Clear documentation about data location
- Easy to disable/remove
