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

## Hardware-Specific Recommendations

Aura Video Studio provides intelligent model recommendations based on your hardware:

### RAM-Based Recommendations

**Less than 8GB RAM:**
- **Recommended:** Models 3B or smaller
- **Example:** `llama3.2:3b` or `phi3:mini`
- **Reason:** Smaller models reduce memory pressure and prevent system slowdowns

**8GB RAM:**
- **Recommended:** `llama3.1:8b-q4_k_m` (quantized 4-bit)
- **Alternative:** `mistral:7b-q4`
- **Reason:** 4-bit quantization provides good balance of quality and memory usage

**16GB+ RAM:**
- **Recommended:** `llama3.1:8b-q4_k_m` (comfortable)
- **Alternative:** `llama3.1:8b` (higher quality, more memory)
- **Reason:** Can run 8B models comfortably with room for other applications

**32GB+ RAM:**
- **Recommended:** `llama3.1:70b-q4_k_m` (if you have GPU)
- **Alternative:** `llama3.1:8b` or `mixtral:8x7b-q4`
- **Reason:** Can run larger models or multiple models simultaneously

### GPU Acceleration

If you have a GPU with 8GB+ VRAM:
- Models can run significantly faster with GPU acceleration
- Configure Ollama to use GPU: Ollama automatically detects and uses CUDA-capable GPUs
- Check GPU usage: `nvidia-smi` (NVIDIA) or task manager

**GPU Model Recommendations:**
- **8GB VRAM:** `llama3.1:8b-q4_k_m`
- **12GB VRAM:** `llama3.1:8b` or `llama2:13b-q4`
- **24GB+ VRAM:** `llama3.1:70b-q4_k_m` (for best quality)

### Quality vs. Speed Trade-offs

**Fast (Low Latency):**
- **Models:** `phi3:mini`, `llama3.2:3b`, `gemma:2b`
- **Use Case:** Quick iterations, testing, low-end hardware
- **Expected Quality:** Good for short scripts, acceptable grammar

**Balanced (Recommended):**
- **Models:** `llama3.1:8b-q4_k_m`, `mistral:7b-q4`
- **Use Case:** Production videos, most users
- **Expected Quality:** Excellent grammar, creative, engaging scripts

**High Quality (Slower):**
- **Models:** `llama3.1:70b-q4_k_m`, `mixtral:8x7b-instruct`
- **Use Case:** Professional content, maximum quality
- **Expected Quality:** Near-human writing, excellent coherence

### Keep-Alive Configuration

To reduce model loading time between requests:

1. Open Aura Video Studio Settings
2. Navigate to Provider Settings → Ollama
3. Set "Keep Alive" to a duration that fits your workflow:
   - **5 minutes** (300s): For occasional use
   - **30 minutes** (1800s): For active editing sessions
   - **Unlimited** (-1): Keep model loaded always (uses RAM continuously)

**Note:** Keeping models loaded uses RAM but eliminates load time (5-30 seconds per request).

### Checking Ollama Status

Aura Video Studio includes an Ollama status checker:

1. Open Settings → Providers → Offline Status
2. Click "Check Ollama"
3. View:
   - Whether Ollama is running
   - List of installed models
   - Hardware-specific recommendations
   - Model sizes and memory requirements

### Performance Tips

1. **First Run:** The first request to Ollama loads the model into memory (5-30s delay). Subsequent requests are fast.
2. **Concurrent Requests:** Ollama handles one request at a time per model.
3. **Memory Management:** If system becomes slow, use smaller models or increase keep-alive to prevent constant loading.
4. **SSD vs HDD:** Model loading is much faster from SSD storage.

### Advanced: Model Quantization

Quantization reduces model size and memory usage with minimal quality loss:

- **q4_k_m:** 4-bit quantization, good balance (recommended)
- **q5_k_m:** 5-bit, slightly better quality, more memory
- **q8_0:** 8-bit, near-original quality, much more memory
- **f16/f32:** Full precision, highest quality, maximum memory

**Example:**
- `llama3.1:8b` (16GB RAM required)
- `llama3.1:8b-q4_k_m` (5GB RAM required) ← Recommended default

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
