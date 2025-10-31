# Ollama Model Selection Feature

## Overview

This feature allows users to select any Ollama model available on their system instead of being locked to the default `llama3.1:8b-q4_k_m` model.

## User Guide

### Selecting an Ollama Model

1. **Navigate to Settings**
   - Open Aura Video Studio
   - Go to Settings → Providers → Provider Paths

2. **Configure Ollama URL** (if not already set)
   - Ensure the "Ollama URL" field is set to where Ollama is running
   - Default: `http://127.0.0.1:11434`
   - Click "Test Connection" to verify Ollama is accessible

3. **Select a Model**
   - Find the "Ollama Model" section below the Ollama URL field
   - Click the "Refresh Models" button to load available models from your Ollama installation
   - A dropdown will populate with all available models, showing:
     - Model name (e.g., `llama3.1:8b-q4_k_m`)
     - Model size in GB (e.g., `(4.92 GB)`)
   - Select your desired model from the dropdown
   - The selection is automatically saved

4. **Use the Selected Model**
   - The next time you generate a video, the selected model will be used
   - You can change the model at any time and it will be used for subsequent generations

## Technical Details

### API Endpoints

#### Get Available Models
```
GET /api/engines/ollama/models?url={ollamaUrl}
```

**Response:**
```json
{
  "models": [
    {
      "name": "llama3.1:8b-q4_k_m",
      "size": 5278359952,
      "sizeGB": 4.92,
      "modifiedAt": "2024-10-15T10:30:00Z",
      "digest": "sha256:abc123..."
    }
  ],
  "baseUrl": "http://127.0.0.1:11434"
}
```

#### Get Current Model Setting
```
GET /api/settings/ollama/model
```

**Response:**
```json
{
  "success": true,
  "model": "llama3.1:8b-q4_k_m"
}
```

#### Set Model
```
POST /api/settings/ollama/model
Content-Type: application/json

{
  "model": "llama3.1:8b-q4_k_m"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Ollama model updated successfully",
  "model": "llama3.1:8b-q4_k_m"
}
```

### Configuration Storage

The selected model is stored in `AuraData/settings.json`:
```json
{
  "ollamaModel": "llama3.1:8b-q4_k_m"
}
```

### Default Behavior

- **Default Model**: `llama3.1:8b-q4_k_m`
- If no model is configured, the default is used automatically
- Backward compatible with existing installations

## Benefits

### Performance Optimization
- **Smaller Models**: Use lighter models like `llama3.2:3b` for faster generation on limited hardware
- **Larger Models**: Use more powerful models like `llama3.1:70b` for higher quality when you have the resources

### Use Case Flexibility
- **Specialized Models**: Use models fine-tuned for specific content types (technical, creative, educational)
- **Language Support**: Choose models optimized for different languages
- **Quality vs Speed**: Balance generation quality with speed based on your needs

### Hardware Compatibility
- Match model size to your available VRAM
- Optimize for your specific hardware configuration

## Examples

### Common Model Configurations

#### Fast Generation (Low Resource)
```
Model: llama3.2:3b
Size: ~2 GB
Use case: Quick drafts, testing, low-end hardware
```

#### Balanced (Default)
```
Model: llama3.1:8b-q4_k_m
Size: ~5 GB
Use case: General purpose, good balance of speed and quality
```

#### High Quality
```
Model: llama3.1:70b
Size: ~40 GB
Use case: Production content, best quality, requires powerful hardware
```

#### Specialized
```
Model: codellama:13b
Size: ~7 GB
Use case: Technical/coding content generation
```

## Troubleshooting

### No Models Appearing
- Ensure Ollama is running: `ollama serve`
- Check the Ollama URL is correct
- Click "Test Connection" to verify connectivity
- Pull models if none are installed: `ollama pull llama3.1`

### Model Not Loading
- Verify the model exists in Ollama: `ollama list`
- Check available VRAM/RAM for the model size
- Try a smaller model if you have resource constraints

### Generation Fails
- Confirm the selected model is still available in Ollama
- Check Ollama logs for errors
- Try switching to the default model
- Ensure Ollama service is running during generation

## Implementation Notes

### Files Modified

**Backend:**
- `Aura.Api/Controllers/EnginesController.cs` - Added models endpoint
- `Aura.Api/Controllers/SettingsController.cs` - Added get/set model endpoints
- `Aura.Core/Configuration/ProviderSettings.cs` - Added model settings methods
- `Aura.Core/Orchestrator/LlmProviderFactory.cs` - Uses configured model

**Frontend:**
- `Aura.Web/src/types/api-v1.ts` - Added Ollama model types
- `Aura.Web/src/types/settings.ts` - Added model to settings
- `Aura.Web/src/pages/SettingsPage.tsx` - Added UI for model selection

**Tests:**
- `Aura.Tests/ProviderSettingsTests.cs` - Added unit tests for model settings

### Backward Compatibility

This feature is fully backward compatible:
- Existing installations continue using `llama3.1:8b-q4_k_m` by default
- No breaking changes to existing workflows
- Settings file is optional (defaults are used if not present)

## Future Enhancements

Potential improvements for future releases:
- Show model capabilities/tags in UI (context length, specializations)
- Display recommended models based on user's hardware profile
- Add model performance metrics (speed, quality ratings)
- Support for model groups/categories
- Quick model switching during workflow without going to settings
