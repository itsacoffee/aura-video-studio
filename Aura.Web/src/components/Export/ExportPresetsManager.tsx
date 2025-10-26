import { useState, useEffect } from 'react';
import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
  Button,
  makeStyles,
  tokens,
  Input,
  Field,
  Dropdown,
  Option,
  Textarea,
  Card,
  CardHeader,
  Text,
  Body1,
  Caption1,
  Badge,
} from '@fluentui/react-components';
import {
  Dismiss24Regular,
  Add24Regular,
  Delete24Regular,
  Edit24Regular,
  Save24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  dialogSurface: {
    maxWidth: '800px',
    width: '100%',
  },
  presetList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalL,
  },
  presetCard: {
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  presetCardSelected: {
    backgroundColor: tokens.colorNeutralBackground1Selected,
    border: `1px solid ${tokens.colorBrandBackground}`,
  },
  presetInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  presetSpecs: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
  },
  formSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalL,
  },
  row: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
  field: {
    flex: 1,
  },
});

export interface CustomPreset {
  id: string;
  name: string;
  description: string;
  codec: string;
  resolution: string;
  bitrate: string;
  fps: number;
  audioCodec: string;
  audioBitrate: string;
  container: string;
}

export interface ExportPresetsManagerProps {
  open: boolean;
  onClose: () => void;
  onPresetSelected?: (preset: CustomPreset) => void;
}

const DEFAULT_PRESETS: CustomPreset[] = [];

export function ExportPresetsManager({
  open,
  onClose,
  onPresetSelected,
}: ExportPresetsManagerProps) {
  const styles = useStyles();
  const [presets, setPresets] = useState<CustomPreset[]>(DEFAULT_PRESETS);
  const [selectedPreset, setSelectedPreset] = useState<CustomPreset | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [editForm, setEditForm] = useState<Partial<CustomPreset>>({
    name: '',
    description: '',
    codec: 'H.264',
    resolution: '1920x1080',
    bitrate: '8000',
    fps: 30,
    audioCodec: 'AAC',
    audioBitrate: '192',
    container: 'mp4',
  });

  // Load presets from localStorage on mount
  useEffect(() => {
    const stored = localStorage.getItem('export-custom-presets');
    if (stored) {
      try {
        const parsed = JSON.parse(stored);
        setPresets(parsed);
      } catch (e) {
        console.error('Failed to load custom presets:', e);
      }
    }
  }, []);

  // Save presets to localStorage whenever they change
  useEffect(() => {
    localStorage.setItem('export-custom-presets', JSON.stringify(presets));
  }, [presets]);

  const handleAddNew = () => {
    setSelectedPreset(null);
    setEditForm({
      name: '',
      description: '',
      codec: 'H.264',
      resolution: '1920x1080',
      bitrate: '8000',
      fps: 30,
      audioCodec: 'AAC',
      audioBitrate: '192',
      container: 'mp4',
    });
    setIsEditing(true);
  };

  const handleEdit = (preset: CustomPreset) => {
    setSelectedPreset(preset);
    setEditForm(preset);
    setIsEditing(true);
  };

  const handleDelete = (presetId: string) => {
    setPresets((prev) => prev.filter((p) => p.id !== presetId));
    if (selectedPreset?.id === presetId) {
      setSelectedPreset(null);
    }
  };

  const handleSave = () => {
    if (!editForm.name || !editForm.description) {
      alert('Please fill in all required fields');
      return;
    }

    const newPreset: CustomPreset = {
      id: selectedPreset?.id || `preset-${Date.now()}`,
      name: editForm.name!,
      description: editForm.description!,
      codec: editForm.codec || 'H.264',
      resolution: editForm.resolution || '1920x1080',
      bitrate: editForm.bitrate || '8000',
      fps: editForm.fps || 30,
      audioCodec: editForm.audioCodec || 'AAC',
      audioBitrate: editForm.audioBitrate || '192',
      container: editForm.container || 'mp4',
    };

    if (selectedPreset) {
      // Update existing
      setPresets((prev) =>
        prev.map((p) => (p.id === selectedPreset.id ? newPreset : p))
      );
    } else {
      // Add new
      setPresets((prev) => [...prev, newPreset]);
    }

    setIsEditing(false);
    setSelectedPreset(null);
  };

  const handleCancel = () => {
    setIsEditing(false);
    setSelectedPreset(null);
  };

  const handleUsePreset = (preset: CustomPreset) => {
    onPresetSelected?.(preset);
    onClose();
  };

  if (isEditing) {
    return (
      <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
        <DialogSurface className={styles.dialogSurface}>
          <DialogBody>
            <DialogTitle
              action={
                <Button
                  appearance="subtle"
                  aria-label="close"
                  icon={<Dismiss24Regular />}
                  onClick={handleCancel}
                />
              }
            >
              {selectedPreset ? 'Edit Preset' : 'Create New Preset'}
            </DialogTitle>
            <DialogContent>
              <div className={styles.formSection}>
                <Field label="Preset Name" required>
                  <Input
                    value={editForm.name}
                    onChange={(_, data) =>
                      setEditForm({ ...editForm, name: data.value })
                    }
                    placeholder="My Custom Preset"
                  />
                </Field>

                <Field label="Description" required>
                  <Textarea
                    value={editForm.description}
                    onChange={(_, data) =>
                      setEditForm({ ...editForm, description: data.value })
                    }
                    placeholder="Describe the purpose of this preset"
                    rows={2}
                  />
                </Field>

                <div className={styles.row}>
                  <Field label="Container Format" className={styles.field}>
                    <Dropdown
                      value={editForm.container}
                      onOptionSelect={(_, data) =>
                        setEditForm({ ...editForm, container: data.optionValue as string })
                      }
                    >
                      <Option value="mp4">MP4</Option>
                      <Option value="webm">WebM</Option>
                      <Option value="mov">MOV</Option>
                      <Option value="avi">AVI</Option>
                    </Dropdown>
                  </Field>

                  <Field label="Video Codec" className={styles.field}>
                    <Dropdown
                      value={editForm.codec}
                      onOptionSelect={(_, data) =>
                        setEditForm({ ...editForm, codec: data.optionValue as string })
                      }
                    >
                      <Option value="H.264">H.264 (x264)</Option>
                      <Option value="H.265">H.265 (x265)</Option>
                      <Option value="VP9">VP9</Option>
                      <Option value="ProRes">ProRes</Option>
                    </Dropdown>
                  </Field>
                </div>

                <div className={styles.row}>
                  <Field label="Resolution" className={styles.field}>
                    <Input
                      value={editForm.resolution}
                      onChange={(_, data) =>
                        setEditForm({ ...editForm, resolution: data.value })
                      }
                      placeholder="1920x1080"
                    />
                  </Field>

                  <Field label="Frame Rate (FPS)" className={styles.field}>
                    <Input
                      type="number"
                      value={editForm.fps?.toString()}
                      onChange={(_, data) =>
                        setEditForm({ ...editForm, fps: parseInt(data.value) || 30 })
                      }
                    />
                  </Field>
                </div>

                <div className={styles.row}>
                  <Field label="Video Bitrate (kbps)" className={styles.field}>
                    <Input
                      type="number"
                      value={editForm.bitrate}
                      onChange={(_, data) =>
                        setEditForm({ ...editForm, bitrate: data.value })
                      }
                      placeholder="8000"
                    />
                  </Field>

                  <Field label="Audio Codec" className={styles.field}>
                    <Dropdown
                      value={editForm.audioCodec}
                      onOptionSelect={(_, data) =>
                        setEditForm({ ...editForm, audioCodec: data.optionValue as string })
                      }
                    >
                      <Option value="AAC">AAC</Option>
                      <Option value="MP3">MP3</Option>
                      <Option value="Opus">Opus</Option>
                      <Option value="PCM">PCM</Option>
                    </Dropdown>
                  </Field>

                  <Field label="Audio Bitrate (kbps)" className={styles.field}>
                    <Input
                      type="number"
                      value={editForm.audioBitrate}
                      onChange={(_, data) =>
                        setEditForm({ ...editForm, audioBitrate: data.value })
                      }
                      placeholder="192"
                    />
                  </Field>
                </div>
              </div>
            </DialogContent>
            <DialogActions>
              <div className={styles.actions}>
                <Button appearance="secondary" onClick={handleCancel}>
                  Cancel
                </Button>
                <Button appearance="primary" icon={<Save24Regular />} onClick={handleSave}>
                  Save Preset
                </Button>
              </div>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    );
  }

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className={styles.dialogSurface}>
        <DialogBody>
          <DialogTitle
            action={
              <Button
                appearance="subtle"
                aria-label="close"
                icon={<Dismiss24Regular />}
                onClick={onClose}
              />
            }
          >
            Custom Export Presets
          </DialogTitle>
          <DialogContent>
            {presets.length === 0 ? (
              <div style={{ textAlign: 'center', padding: tokens.spacingVerticalXXL }}>
                <Body1>No custom presets yet</Body1>
                <Caption1>Create your first custom export preset</Caption1>
              </div>
            ) : (
              <div className={styles.presetList}>
                {presets.map((preset) => (
                  <Card
                    key={preset.id}
                    className={`${styles.presetCard} ${
                      selectedPreset?.id === preset.id ? styles.presetCardSelected : ''
                    }`}
                    onClick={() => setSelectedPreset(preset)}
                  >
                    <CardHeader
                      header={<Text weight="semibold">{preset.name}</Text>}
                      description={<Caption1>{preset.description}</Caption1>}
                      action={
                        <div style={{ display: 'flex', gap: tokens.spacingHorizontalS }}>
                          <Button
                            appearance="subtle"
                            size="small"
                            icon={<Edit24Regular />}
                            onClick={(e) => {
                              e.stopPropagation();
                              handleEdit(preset);
                            }}
                            aria-label="Edit preset"
                          />
                          <Button
                            appearance="subtle"
                            size="small"
                            icon={<Delete24Regular />}
                            onClick={(e) => {
                              e.stopPropagation();
                              handleDelete(preset.id);
                            }}
                            aria-label="Delete preset"
                          />
                        </div>
                      }
                    />
                    <div className={styles.presetInfo}>
                      <div className={styles.presetSpecs}>
                        <Badge size="small">{preset.codec}</Badge>
                        <Caption1>{preset.resolution}</Caption1>
                        <Caption1>{preset.bitrate} kbps</Caption1>
                        <Caption1>{preset.fps} FPS</Caption1>
                        <Caption1>{preset.container.toUpperCase()}</Caption1>
                      </div>
                    </div>
                  </Card>
                ))}
              </div>
            )}
          </DialogContent>
          <DialogActions>
            <div className={styles.actions}>
              <Button appearance="secondary" onClick={onClose}>
                Close
              </Button>
              <Button appearance="secondary" icon={<Add24Regular />} onClick={handleAddNew}>
                Create New Preset
              </Button>
              {selectedPreset && (
                <Button
                  appearance="primary"
                  onClick={() => handleUsePreset(selectedPreset)}
                >
                  Use This Preset
                </Button>
              )}
            </div>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
