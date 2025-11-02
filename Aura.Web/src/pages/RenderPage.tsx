import { makeStyles, tokens, Title1, Text } from '@fluentui/react-components';
import { useState } from 'react';
import type { FileMetadata } from '../components/Render/FileContext';
import { FileContext } from '../components/Render/FileContext';
import { FileDropzone } from '../components/Render/FileDropzone';
import { FileSelector } from '../components/Render/FileSelector';
import { ReEncodingPresets, type ReEncodingPreset } from '../components/Render/ReEncodingPresets';
import { RenderPanel } from '../components/RenderPanel';
import { useRenderStore } from '../state/render';

const useStyles = makeStyles({
  container: {
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
  },
  dropzoneSection: {
    marginBottom: tokens.spacingVerticalL,
  },
});

export function RenderPage() {
  const styles = useStyles();
  const { selectedFile, setSelectedFile, updateSettings } = useRenderStore();
  const [showFileSelector, setShowFileSelector] = useState(false);
  const [selectedPreset, setSelectedPreset] = useState<string | null>(null);

  const handleFileSelected = async (file: File) => {
    // Simulate extracting metadata from the file
    const metadata: FileMetadata = {
      name: file.name,
      path: file.name,
      type: file.type.startsWith('video/') ? 'video' : 'audio',
      duration: 0,
      size: file.size,
    };

    // In a real implementation, we would call an API to extract full metadata
    try {
      // Mock API call to get file metadata
      const response = await fetch('/api/files/metadata', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ fileName: file.name, fileSize: file.size }),
      });

      if (response.ok) {
        const data = await response.json();
        Object.assign(metadata, data);
      }
    } catch (error) {
      console.error('Error getting file metadata:', error);
    }

    setSelectedFile(metadata);
  };

  const handleFileSelectedFromList = (file: FileMetadata) => {
    setSelectedFile(file);
  };

  const handleBrowseFiles = () => {
    // Create file input and trigger click
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.mp4,.mov,.avi,.mkv,.webm,.mp3,.wav,.aac,.m4a,.flac';
    input.onchange = (e) => {
      const target = e.target as HTMLInputElement;
      if (target.files && target.files.length > 0) {
        handleFileSelected(target.files[0]);
      }
    };
    input.click();
  };

  const handlePresetSelect = (preset: ReEncodingPreset) => {
    setSelectedPreset(preset.id);

    // Update render settings based on preset
    updateSettings({
      codec: preset.targetCodec as 'H264' | 'HEVC' | 'AV1',
      videoBitrateK: preset.targetBitrate / 1000,
      resolution: preset.targetResolution || {
        width: selectedFile?.resolution?.width || 1920,
        height: selectedFile?.resolution?.height || 1080,
      },
    });
  };

  const handleClearFile = () => {
    setSelectedFile(null);
    setSelectedPreset(null);
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Render & Export</Title1>
        <Text className={styles.subtitle}>
          Re-encode existing files or configure render settings for video export
        </Text>
      </div>

      <FileContext
        file={selectedFile}
        onSelectFile={() => setShowFileSelector(true)}
        onClearFile={handleClearFile}
      />

      {!selectedFile && (
        <div className={styles.dropzoneSection}>
          <FileDropzone onFileSelected={handleFileSelected} />
        </div>
      )}

      {selectedFile && (
        <>
          <ReEncodingPresets
            sourceFile={selectedFile}
            selectedPreset={selectedPreset}
            onPresetSelect={handlePresetSelect}
          />
        </>
      )}

      <RenderPanel />

      <FileSelector
        open={showFileSelector}
        onClose={() => setShowFileSelector(false)}
        onSelect={handleFileSelectedFromList}
        onBrowse={handleBrowseFiles}
      />
    </div>
  );
}
