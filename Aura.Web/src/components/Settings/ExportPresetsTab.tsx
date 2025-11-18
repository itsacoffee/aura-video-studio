import {
  Button,
  Card,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  DialogTrigger,
  Field,
  Input,
  Select,
  Spinner,
  Text,
  Title2,
  Title3,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { Add24Regular, Delete24Regular, Edit24Regular, Star24Filled } from '@fluentui/react-icons';
import { useEffect, useState } from 'react';
import { apiUrl } from '../../config/api';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  section: {
    padding: tokens.spacingVerticalXL,
  },
  presetCard: {
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground2,
    cursor: 'pointer',
    transition: 'all 0.2s cubic-bezier(0.4, 0, 0.2, 1)',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3,
      transform: 'translateY(-2px)',
      boxShadow: '0 4px 12px rgba(0, 0, 0, 0.1)',
    },
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
  },
  badge: {
    padding: '4px 8px',
    borderRadius: tokens.borderRadiusSmall,
    fontSize: '12px',
    fontWeight: 600,
  },
  builtInBadge: {
    backgroundColor: tokens.colorPaletteBlueBorderActive,
    color: tokens.colorNeutralForegroundOnBrand,
  },
  customBadge: {
    backgroundColor: tokens.colorPaletteGreenBorderActive,
    color: tokens.colorNeutralForegroundOnBrand,
  },
});

interface ExportPreset {
  id: string;
  name: string;
  description: string;
  resolution: string;
  frameRate: number;
  codec: string;
  preset?: string;
  bitrate: string;
  audioCodec: string;
  audioBitrate: string;
  audioSampleRate: number;
  format: string;
  isBuiltIn: boolean;
  category: string;
}

export function ExportPresetsTab() {
  const styles = useStyles();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [presets, setPresets] = useState<ExportPreset[]>([]);
  const [editingPreset, setEditingPreset] = useState<ExportPreset | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  useEffect(() => {
    loadPresets();
  }, []);

  const loadPresets = async () => {
    setLoading(true);
    try {
      const response = await fetch(apiUrl('/api/export-presets'));
      if (response.ok) {
        const data = await response.json();
        const allPresets = [...(data.builtIn || []), ...(data.custom || [])];
        setPresets(allPresets);
      }
    } catch (error) {
      console.error('Error loading presets:', error);
    } finally {
      setLoading(false);
    }
  };

  const openNewPresetDialog = () => {
    setEditingPreset({
      id: '',
      name: '',
      description: '',
      resolution: '1920x1080',
      frameRate: 30,
      codec: 'libx264',
      bitrate: '5M',
      audioCodec: 'aac',
      audioBitrate: '192k',
      audioSampleRate: 44100,
      format: 'mp4',
      isBuiltIn: false,
      category: 'custom',
    });
    setIsDialogOpen(true);
  };

  const openEditPresetDialog = (preset: ExportPreset) => {
    if (preset.isBuiltIn) {
      alert('Cannot edit built-in presets');
      return;
    }
    setEditingPreset({ ...preset });
    setIsDialogOpen(true);
  };

  const savePreset = async () => {
    if (!editingPreset) return;

    if (!editingPreset.name.trim()) {
      alert('Preset name is required');
      return;
    }

    setSaving(true);
    try {
      const isNew = !editingPreset.id;
      const url = isNew
        ? apiUrl('/api/export-presets')
        : apiUrl(`/api/export-presets/${editingPreset.id}`);
      const method = isNew ? 'POST' : 'PUT';

      const response = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(editingPreset),
      });

      if (response.ok) {
        setIsDialogOpen(false);
        setEditingPreset(null);
        await loadPresets();
      } else {
        alert('Failed to save preset');
      }
    } catch (error) {
      console.error('Error saving preset:', error);
      alert('Error saving preset');
    } finally {
      setSaving(false);
    }
  };

  const deletePreset = async (presetId: string) => {
    const preset = presets.find((p) => p.id === presetId);
    if (!preset) return;

    if (preset.isBuiltIn) {
      alert('Cannot delete built-in presets');
      return;
    }

    if (!confirm(`Delete preset "${preset.name}"?`)) {
      return;
    }

    try {
      const response = await fetch(apiUrl(`/api/export-presets/${presetId}`), {
        method: 'DELETE',
      });

      if (response.ok) {
        await loadPresets();
      } else {
        alert('Failed to delete preset');
      }
    } catch (error) {
      console.error('Error deleting preset:', error);
      alert('Error deleting preset');
    }
  };

  if (loading) {
    return (
      <Card className={styles.section}>
        <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
          <Spinner size="small" />
          <Text>Loading export presets...</Text>
        </div>
      </Card>
    );
  }

  const builtInPresets = presets.filter((p) => p.isBuiltIn);
  const customPresets = presets.filter((p) => !p.isBuiltIn);

  return (
    <div className={styles.container}>
      <Card className={styles.section}>
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            marginBottom: tokens.spacingVerticalL,
          }}
        >
          <div>
            <Title2>Export Presets</Title2>
            <Text size={200} style={{ marginTop: tokens.spacingVerticalS }}>
              Manage video export presets for different platforms and use cases
            </Text>
          </div>
          <Button appearance="primary" icon={<Add24Regular />} onClick={openNewPresetDialog}>
            Create Custom Preset
          </Button>
        </div>

        <Title3>Built-in Presets</Title3>
        <Text
          size={200}
          style={{ marginBottom: tokens.spacingVerticalM, color: tokens.colorNeutralForeground3 }}
        >
          Optimized presets for popular platforms
        </Text>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
            gap: tokens.spacingVerticalM,
          }}
        >
          {builtInPresets.map((preset) => (
            <Card key={preset.id} className={styles.presetCard}>
              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'start',
                  marginBottom: tokens.spacingVerticalS,
                }}
              >
                <Text weight="semibold" size={400}>
                  {preset.name}
                </Text>
                {preset.category === 'social-media' && (
                  <Star24Filled style={{ color: tokens.colorPaletteYellowForeground1 }} />
                )}
              </div>
              <Text
                size={200}
                style={{
                  marginBottom: tokens.spacingVerticalS,
                  color: tokens.colorNeutralForeground3,
                }}
              >
                {preset.description}
              </Text>
              <div
                style={{
                  display: 'flex',
                  flexWrap: 'wrap',
                  gap: tokens.spacingVerticalXS,
                  marginTop: tokens.spacingVerticalM,
                }}
              >
                <span className={`${styles.badge} ${styles.builtInBadge}`}>Built-in</span>
                <span
                  className={styles.badge}
                  style={{ backgroundColor: tokens.colorNeutralBackground4 }}
                >
                  {preset.resolution}
                </span>
                <span
                  className={styles.badge}
                  style={{ backgroundColor: tokens.colorNeutralBackground4 }}
                >
                  {preset.frameRate} FPS
                </span>
                <span
                  className={styles.badge}
                  style={{ backgroundColor: tokens.colorNeutralBackground4 }}
                >
                  {preset.codec}
                </span>
              </div>
            </Card>
          ))}
        </div>

        {customPresets.length > 0 && (
          <>
            <Title3 style={{ marginTop: tokens.spacingVerticalXXL }}>Custom Presets</Title3>
            <Text
              size={200}
              style={{
                marginBottom: tokens.spacingVerticalM,
                color: tokens.colorNeutralForeground3,
              }}
            >
              Your custom export configurations
            </Text>
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
                gap: tokens.spacingVerticalM,
              }}
            >
              {customPresets.map((preset) => (
                <Card key={preset.id} className={styles.presetCard}>
                  <div
                    style={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'start',
                      marginBottom: tokens.spacingVerticalS,
                    }}
                  >
                    <Text weight="semibold" size={400}>
                      {preset.name}
                    </Text>
                    <div style={{ display: 'flex', gap: tokens.spacingHorizontalXS }}>
                      <Button
                        size="small"
                        appearance="subtle"
                        icon={<Edit24Regular />}
                        onClick={(e) => {
                          e.stopPropagation();
                          openEditPresetDialog(preset);
                        }}
                      />
                      <Button
                        size="small"
                        appearance="subtle"
                        icon={<Delete24Regular />}
                        onClick={(e) => {
                          e.stopPropagation();
                          deletePreset(preset.id);
                        }}
                      />
                    </div>
                  </div>
                  <Text
                    size={200}
                    style={{
                      marginBottom: tokens.spacingVerticalS,
                      color: tokens.colorNeutralForeground3,
                    }}
                  >
                    {preset.description}
                  </Text>
                  <div
                    style={{
                      display: 'flex',
                      flexWrap: 'wrap',
                      gap: tokens.spacingVerticalXS,
                      marginTop: tokens.spacingVerticalM,
                    }}
                  >
                    <span className={`${styles.badge} ${styles.customBadge}`}>Custom</span>
                    <span
                      className={styles.badge}
                      style={{ backgroundColor: tokens.colorNeutralBackground4 }}
                    >
                      {preset.resolution}
                    </span>
                    <span
                      className={styles.badge}
                      style={{ backgroundColor: tokens.colorNeutralBackground4 }}
                    >
                      {preset.frameRate} FPS
                    </span>
                    <span
                      className={styles.badge}
                      style={{ backgroundColor: tokens.colorNeutralBackground4 }}
                    >
                      {preset.codec}
                    </span>
                  </div>
                </Card>
              ))}
            </div>
          </>
        )}
      </Card>

      <Dialog open={isDialogOpen} onOpenChange={(_, data) => setIsDialogOpen(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{editingPreset?.id ? 'Edit' : 'Create'} Export Preset</DialogTitle>
            <DialogContent>
              {editingPreset && (
                <div className={styles.form}>
                  <Field label="Preset Name" required>
                    <Input
                      value={editingPreset.name}
                      onChange={(_, data) =>
                        setEditingPreset({ ...editingPreset, name: data.value })
                      }
                      placeholder="e.g., My YouTube 4K"
                    />
                  </Field>

                  <Field label="Description">
                    <Input
                      value={editingPreset.description}
                      onChange={(_, data) =>
                        setEditingPreset({ ...editingPreset, description: data.value })
                      }
                      placeholder="Describe this preset..."
                    />
                  </Field>

                  <Field label="Resolution">
                    <Select
                      value={editingPreset.resolution}
                      onChange={(_, data) =>
                        setEditingPreset({ ...editingPreset, resolution: data.value })
                      }
                    >
                      <option value="1280x720">1280x720 (720p)</option>
                      <option value="1920x1080">1920x1080 (1080p)</option>
                      <option value="2560x1440">2560x1440 (1440p)</option>
                      <option value="3840x2160">3840x2160 (4K)</option>
                      <option value="1080x1920">1080x1920 (Vertical 9:16)</option>
                    </Select>
                  </Field>

                  <Field label="Frame Rate (FPS)">
                    <Select
                      value={editingPreset.frameRate.toString()}
                      onChange={(_, data) =>
                        setEditingPreset({ ...editingPreset, frameRate: parseInt(data.value) })
                      }
                    >
                      <option value="24">24</option>
                      <option value="30">30</option>
                      <option value="60">60</option>
                      <option value="120">120</option>
                    </Select>
                  </Field>

                  <Field label="Video Codec">
                    <Select
                      value={editingPreset.codec}
                      onChange={(_, data) =>
                        setEditingPreset({ ...editingPreset, codec: data.value })
                      }
                    >
                      <option value="libx264">H.264 (libx264)</option>
                      <option value="libx265">H.265 (libx265)</option>
                      <option value="h264_nvenc">H.264 (NVENC)</option>
                      <option value="hevc_nvenc">H.265 (NVENC)</option>
                    </Select>
                  </Field>

                  <Field label="Video Bitrate">
                    <Input
                      value={editingPreset.bitrate}
                      onChange={(_, data) =>
                        setEditingPreset({ ...editingPreset, bitrate: data.value })
                      }
                      placeholder="e.g., 5M, 8M"
                    />
                  </Field>

                  <Field label="Audio Codec">
                    <Select
                      value={editingPreset.audioCodec}
                      onChange={(_, data) =>
                        setEditingPreset({ ...editingPreset, audioCodec: data.value })
                      }
                    >
                      <option value="aac">AAC</option>
                      <option value="mp3">MP3</option>
                      <option value="flac">FLAC</option>
                    </Select>
                  </Field>

                  <Field label="Container Format">
                    <Select
                      value={editingPreset.format}
                      onChange={(_, data) =>
                        setEditingPreset({ ...editingPreset, format: data.value })
                      }
                    >
                      <option value="mp4">MP4</option>
                      <option value="mkv">MKV</option>
                      <option value="webm">WebM</option>
                    </Select>
                  </Field>
                </div>
              )}
            </DialogContent>
            <DialogActions>
              <DialogTrigger disableButtonEnhancement>
                <Button appearance="secondary">Cancel</Button>
              </DialogTrigger>
              <Button appearance="primary" onClick={savePreset} disabled={saving}>
                {saving ? 'Saving...' : 'Save Preset'}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
}
