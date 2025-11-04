# Export Presets, Validation, and Post-Export Integrity Checks - Implementation Guide

## Overview

This document provides a comprehensive guide for implementing the export presets feature with preflight validation and post-export integrity checks. The backend implementation is complete, and this guide focuses on frontend integration and testing.

## Backend Implementation (Completed)

### Services Implemented

1. **ExportPreflightValidator** (`Aura.Core/Services/Export/ExportPreflightValidator.cs`)
   - Validates export settings before starting an export
   - Checks disk space availability
   - Validates aspect ratio conformity
   - Validates resolution (detects upscaling)
   - Validates bitrate ceilings against platform limits
   - Checks codec compatibility
   - Validates platform-specific requirements (duration, file size)
   - Estimates encoding duration based on hardware tier
   - Recommends hardware or software encoder

2. **HardwareEncoderSelection** (`Aura.Core/Models/Export/HardwareEncoderSelection.cs`)
   - Hardware-tier-aware encoder selection
   - Supports NVENC (NVIDIA), AMF (AMD), QSV (Intel)
   - Falls back to software encoders for lower tiers
   - Provides encoder quality presets based on tier and quality level
   - Returns encoder-specific parameters (CRF for software, RC for hardware)

3. **ExportMetadata** (`Aura.Core/Models/Export/ExportMetadata.cs`)
   - Captures post-export metadata
   - Computes SHA256 file hash
   - Stores actual video properties (resolution, codec, bitrate, duration)
   - Records validation results
   - Tracks hardware acceleration usage

4. **Enhanced ExportValidator** (`Aura.Core/Services/Render/ExportValidator.cs`)
   - Now includes `CaptureMetadataAsync` method
   - Computes file hash using SHA256
   - Integrates with ffprobe for metadata extraction
   - Validates export against expected preset parameters

### API Endpoints

#### Preflight Validation

```http
POST /api/export/preflight
Content-Type: application/json

{
  "presetName": "YouTube 1080p",
  "videoDuration": "00:02:00",
  "outputDirectory": "/path/to/output",
  "sourceResolution": "1920x1080",
  "sourceAspectRatio": "16:9"
}
```

Response:
```json
{
  "canProceed": true,
  "errors": [],
  "warnings": [
    "Source aspect ratio (9:16) differs from preset (16:9). Video will be letterboxed or pillarboxed."
  ],
  "recommendations": [
    "Consider using 'Fix automatically' to adjust project aspect ratio"
  ],
  "estimates": {
    "estimatedFileSizeMB": 120.5,
    "estimatedDurationMinutes": 2.5,
    "requiredDiskSpaceMB": 301.25,
    "availableDiskSpaceGB": 50.0,
    "recommendedEncoder": "h264_nvenc",
    "hardwareAccelerationAvailable": true
  }
}
```

#### Export Presets

```http
GET /api/export/presets
```

Returns list of all available export presets with their specifications.

### Hardware Tier Integration

The system automatically detects hardware capabilities and selects the appropriate encoder:

- **Tier A** (High-end): Hardware encoders with high-quality presets
- **Tier B** (Upper-mid): Hardware encoders with balanced presets
- **Tier C** (Mid): Hardware or software encoders based on capabilities
- **Tier D** (Entry): Software encoders with faster presets

## Frontend Implementation Guide

### 1. Export Dialog Component

Create `ExportDialog.tsx` in `Aura.Web/src/components/export/`:

```typescript
import React, { useState, useEffect } from 'react';
import { Dialog, Button, Select, Text } from '@fluentui/react-components';
import { useExportStore } from '@/state/export';
import { apiClient } from '@/services/api/apiClient';
import type { ExportPreset, PreflightResult } from '@/types/api-v1';

interface ExportDialogProps {
  open: boolean;
  onClose: () => void;
  onExport: (presetName: string) => void;
  videoDuration: number; // in seconds
}

export const ExportDialog: React.FC<ExportDialogProps> = ({
  open,
  onClose,
  onExport,
  videoDuration
}) => {
  const [selectedPreset, setSelectedPreset] = useState<string>('');
  const [preflightResult, setPreflightResult] = useState<PreflightResult | null>(null);
  const [loading, setLoading] = useState(false);

  // Fetch presets on mount
  const { presets, fetchPresets } = useExportStore();
  
  useEffect(() => {
    if (open) {
      fetchPresets();
    }
  }, [open, fetchPresets]);

  // Run preflight validation when preset changes
  useEffect(() => {
    if (selectedPreset && open) {
      runPreflightValidation();
    }
  }, [selectedPreset, open]);

  const runPreflightValidation = async () => {
    setLoading(true);
    try {
      const response = await apiClient.post('/api/export/preflight', {
        presetName: selectedPreset,
        videoDuration: `00:${Math.floor(videoDuration / 60)}:${videoDuration % 60}`,
        outputDirectory: '', // Use default
      });
      setPreflightResult(response.data);
    } catch (error) {
      console.error('Preflight validation failed:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleExport = () => {
    if (preflightResult?.canProceed || confirm('There are issues. Proceed anyway?')) {
      onExport(selectedPreset);
      onClose();
    }
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <DialogTitle>Export Video</DialogTitle>
        <DialogBody>
          <Select
            label="Export Preset"
            value={selectedPreset}
            onChange={(_, data) => setSelectedPreset(data.value)}
          >
            {presets.map(preset => (
              <option key={preset.name} value={preset.name}>
                {preset.name} - {preset.description}
              </option>
            ))}
          </Select>

          {loading && <Spinner label="Validating..." />}

          {preflightResult && (
            <PreflightSummary result={preflightResult} />
          )}
        </DialogBody>
        <DialogActions>
          <Button appearance="secondary" onClick={onClose}>Cancel</Button>
          <Button 
            appearance="primary" 
            onClick={handleExport}
            disabled={!selectedPreset || (!preflightResult?.canProceed && preflightResult?.errors.length > 0)}
          >
            Export
          </Button>
        </DialogActions>
      </DialogSurface>
    </Dialog>
  );
};
```

### 2. Preflight Summary Component

Create `PreflightSummary.tsx`:

```typescript
interface PreflightSummaryProps {
  result: PreflightResult;
}

export const PreflightSummary: React.FC<PreflightSummaryProps> = ({ result }) => {
  return (
    <div className="preflight-summary">
      {result.errors.length > 0 && (
        <MessageBar intent="error">
          <strong>Errors:</strong>
          <ul>
            {result.errors.map((error, i) => <li key={i}>{error}</li>)}
          </ul>
        </MessageBar>
      )}

      {result.warnings.length > 0 && (
        <MessageBar intent="warning">
          <strong>Warnings:</strong>
          <ul>
            {result.warnings.map((warning, i) => <li key={i}>{warning}</li>)}
          </ul>
        </MessageBar>
      )}

      {result.recommendations.length > 0 && (
        <MessageBar intent="info">
          <strong>Recommendations:</strong>
          <ul>
            {result.recommendations.map((rec, i) => <li key={i}>{rec}</li>)}
          </ul>
        </MessageBar>
      )}

      <div className="estimates">
        <Text weight="semibold">Estimates:</Text>
        <dl>
          <dt>File Size:</dt>
          <dd>{result.estimates.estimatedFileSizeMB.toFixed(2)} MB</dd>
          
          <dt>Encoding Time:</dt>
          <dd>{result.estimates.estimatedDurationMinutes.toFixed(1)} minutes</dd>
          
          <dt>Required Space:</dt>
          <dd>{result.estimates.requiredDiskSpaceMB.toFixed(2)} MB</dd>
          
          <dt>Encoder:</dt>
          <dd>
            {result.estimates.recommendedEncoder}
            {result.estimates.hardwareAccelerationAvailable && ' (Hardware Accelerated)'}
          </dd>
        </dl>
      </div>
    </div>
  );
};
```

### 3. Export State Management

Create `Aura.Web/src/state/export.ts`:

```typescript
import { create } from 'zustand';
import { apiClient } from '@/services/api/apiClient';
import type { ExportPreset, ExportJob } from '@/types/api-v1';

interface ExportState {
  presets: ExportPreset[];
  currentJob: ExportJob | null;
  
  fetchPresets: () => Promise<void>;
  startExport: (presetName: string, outputFile: string) => Promise<string>;
  getJobStatus: (jobId: string) => Promise<ExportJob>;
  cancelJob: (jobId: string) => Promise<void>;
}

export const useExportStore = create<ExportState>((set, get) => ({
  presets: [],
  currentJob: null,

  fetchPresets: async () => {
    try {
      const response = await apiClient.get('/api/export/presets');
      set({ presets: response.data });
    } catch (error) {
      console.error('Failed to fetch presets:', error);
    }
  },

  startExport: async (presetName: string, outputFile: string) => {
    try {
      const response = await apiClient.post('/api/export/start', {
        presetName,
        outputFile
      });
      return response.data.jobId;
    } catch (error) {
      console.error('Failed to start export:', error);
      throw error;
    }
  },

  getJobStatus: async (jobId: string) => {
    try {
      const response = await apiClient.get(`/api/export/status/${jobId}`);
      set({ currentJob: response.data });
      return response.data;
    } catch (error) {
      console.error('Failed to get job status:', error);
      throw error;
    }
  },

  cancelJob: async (jobId: string) => {
    try {
      await apiClient.post(`/api/export/cancel/${jobId}`);
      set({ currentJob: null });
    } catch (error) {
      console.error('Failed to cancel job:', error);
      throw error;
    }
  }
}));
```

### 4. Job Completion Panel

Create `ExportCompletionPanel.tsx`:

```typescript
interface ExportCompletionPanelProps {
  job: ExportJob;
  metadata?: ExportMetadata;
}

export const ExportCompletionPanel: React.FC<ExportCompletionPanelProps> = ({ 
  job, 
  metadata 
}) => {
  const openFolder = () => {
    if (job.outputFile) {
      // Use Electron shell to open folder
      const folder = path.dirname(job.outputFile);
      shell.openPath(folder);
    }
  };

  return (
    <Card className="export-completion">
      <CardHeader>
        <Text weight="semibold" size={500}>Export Completed</Text>
        <Status appearance="success">Success</Status>
      </CardHeader>

      <CardBody>
        {metadata && (
          <div className="export-metadata">
            <dl>
              <dt>Resolution:</dt>
              <dd>{metadata.resolution.width}x{metadata.resolution.height}</dd>
              
              <dt>Duration:</dt>
              <dd>{formatDuration(metadata.duration)}</dd>
              
              <dt>Codec:</dt>
              <dd>{metadata.videoCodec} / {metadata.audioCodec}</dd>
              
              <dt>File Size:</dt>
              <dd>{(metadata.fileSizeBytes / 1024 / 1024).toFixed(2)} MB</dd>
              
              <dt>Encoder:</dt>
              <dd>
                {metadata.encoderUsed}
                {metadata.hardwareAccelerationUsed && ' (HW Accelerated)'}
              </dd>
            </dl>

            {metadata.validationIssues.length > 0 && (
              <MessageBar intent="warning">
                <strong>Validation Issues:</strong>
                <ul>
                  {metadata.validationIssues.map((issue, i) => 
                    <li key={i}>{issue}</li>
                  )}
                </ul>
              </MessageBar>
            )}
          </div>
        )}
      </CardBody>

      <CardFooter>
        <Button appearance="primary" onClick={openFolder}>
          Open Folder
        </Button>
      </CardFooter>
    </Card>
  );
};
```

## Testing Plan

### Unit Tests

1. **Export Dialog Tests** (`ExportDialog.test.tsx`)
   - Renders with preset list
   - Triggers preflight validation on preset selection
   - Shows errors/warnings from preflight
   - Enables/disables export button based on validation

2. **Preflight Summary Tests** (`PreflightSummary.test.tsx`)
   - Displays errors with correct styling
   - Displays warnings with correct styling
   - Shows estimates in readable format

3. **Export State Tests** (`export.test.ts`)
   - Fetches presets from API
   - Starts export job
   - Polls job status
   - Cancels job

### Integration Tests

1. **Export Flow Test**
   - User selects preset
   - Preflight validation runs automatically
   - Warnings displayed
   - Export starts
   - Progress updates via SSE
   - Completion panel shows metadata

### E2E Tests (Playwright)

1. **Happy Path Export** (`export-flow.spec.ts`)
   - Open export dialog
   - Select "YouTube 1080p" preset
   - Verify preflight summary appears
   - Click export
   - Wait for completion
   - Verify file exists

2. **Error Handling** (`export-errors.spec.ts`)
   - Select TikTok preset with long video
   - Verify error message
   - Verify export button disabled
   - Try "Proceed Anyway" option

3. **Hardware Acceleration** (`hw-accel.spec.ts`)
   - Mock system with NVIDIA GPU
   - Verify NVENC recommendation
   - Compare encoding time with CPU

## API Type Definitions

Add to `Aura.Web/src/types/api-v1.ts`:

```typescript
export interface ExportPreset {
  name: string;
  description: string;
  platform: string;
  resolution: string;
  videoCodec: string;
  audioCodec: string;
  frameRate: number;
  videoBitrate: number;
  audioBitrate: number;
  aspectRatio: string;
  quality: string;
}

export interface PreflightResult {
  canProceed: boolean;
  errors: string[];
  warnings: string[];
  recommendations: string[];
  estimates: {
    estimatedFileSizeMB: number;
    estimatedDurationMinutes: number;
    requiredDiskSpaceMB: number;
    availableDiskSpaceGB: number;
    recommendedEncoder: string;
    hardwareAccelerationAvailable: boolean;
  };
}

export interface ExportJob {
  id: string;
  status: string;
  progress: number;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  errorMessage?: string;
  outputFile?: string;
  estimatedTimeRemaining?: string;
}

export interface ExportMetadata {
  fileHash: string;
  fileSizeBytes: number;
  duration: string;
  videoCodec: string;
  audioCodec: string;
  resolution: {
    width: number;
    height: number;
  };
  frameRate: number;
  videoBitrate: number;
  audioBitrate: number;
  container: string;
  encoderUsed: string;
  encodingDuration: string;
  presetName: string;
  platform: string;
  validationIssues: string[];
  validationPassed: boolean;
  hardwareAccelerationUsed: boolean;
}
```

## Acceptance Criteria

- [ ] Users can select from curated export presets
- [ ] Preflight validation runs automatically
- [ ] Errors prevent export, warnings allow "Proceed anyway"
- [ ] Disk space estimates shown before export
- [ ] Hardware acceleration automatically detected and used
- [ ] Progress updates during export
- [ ] Post-export metadata displayed
- [ ] "Open folder" button works
- [ ] Validation issues flagged in completion panel
- [ ] All tests pass

## Known Limitations

1. Platform profile validation uses generic limits (may need per-account customization)
2. Disk space estimation includes 150% buffer (conservative)
3. Encoding time estimation is approximate (actual may vary ±30%)
4. Hardware acceleration detection requires FFmpeg 4.0+
5. Hash computation may take time for large files (10+ GB)

## Future Enhancements

1. Custom preset creation/editing
2. Batch export with multiple presets
3. Auto-retry failed exports
4. Export queue management
5. Cloud storage integration
6. Format conversion post-export
7. Advanced codec options (HDR, 10-bit)
8. Export profiles per project
