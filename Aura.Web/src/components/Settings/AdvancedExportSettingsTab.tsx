import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Button,
  Input,
  Switch,
  Field,
  Card,
  Slider,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import { Save24Regular, Delete24Regular, Add24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import type {
  ExportSettings,
  WatermarkPosition,
  WatermarkType,
  UploadDestination,
  UploadDestinationType,
} from '../../types/settings';

const useStyles = makeStyles({
  section: {
    padding: tokens.spacingVerticalXL,
    marginBottom: tokens.spacingVerticalL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  row: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalL,
    '@media (max-width: 768px)': {
      gridTemplateColumns: '1fr',
    },
  },
  cardHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  destinationCard: {
    padding: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
  },
});

interface AdvancedExportSettingsTabProps {
  settings: ExportSettings;
  onChange: (settings: ExportSettings) => void;
  onSave: () => void;
  hasChanges: boolean;
}

export function AdvancedExportSettingsTab({
  settings,
  onChange,
  onSave,
  hasChanges,
}: AdvancedExportSettingsTabProps) {
  const styles = useStyles();
  const [_selectedDestination, _setSelectedDestination] = useState<UploadDestination | null>(null);

  const updateWatermark = (updates: Partial<typeof settings.watermark>) => {
    onChange({
      ...settings,
      watermark: { ...settings.watermark, ...updates },
    });
  };

  const updateNamingPattern = (updates: Partial<typeof settings.namingPattern>) => {
    onChange({
      ...settings,
      namingPattern: { ...settings.namingPattern, ...updates },
    });
  };

  const addUploadDestination = () => {
    const newDestination: UploadDestination = {
      id: crypto.randomUUID(),
      name: 'New Destination',
      type: 'LocalFolder' as UploadDestinationType,
      enabled: true,
      localPath: '',
      host: '',
      port: 22,
      username: '',
      password: '',
      remotePath: '/',
      s3BucketName: '',
      s3Region: 'us-east-1',
      s3AccessKey: '',
      s3SecretKey: '',
      azureContainerName: '',
      azureConnectionString: '',
      googleDriveFolderId: '',
      dropboxPath: '',
      deleteAfterUpload: false,
      maxRetries: 3,
      timeoutSeconds: 300,
    };
    onChange({
      ...settings,
      uploadDestinations: [...settings.uploadDestinations, newDestination],
    });
  };

  const updateUploadDestination = (id: string, updates: Partial<UploadDestination>) => {
    onChange({
      ...settings,
      uploadDestinations: settings.uploadDestinations.map((dest) =>
        dest.id === id ? { ...dest, ...updates } : dest
      ),
    });
  };

  const removeUploadDestination = (id: string) => {
    onChange({
      ...settings,
      uploadDestinations: settings.uploadDestinations.filter((dest) => dest.id !== id),
    });
  };

  return (
    <>
      {/* Watermark Settings */}
      <Card className={styles.section}>
        <Title2>Watermark Configuration</Title2>
        <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
          Add a watermark to your exported videos for branding or copyright protection
        </Text>

        <div className={styles.form}>
          <Field label="Enable Watermark">
            <Switch
              checked={settings.watermark.enabled}
              onChange={(_, data) => updateWatermark({ enabled: data.checked })}
            />
          </Field>

          {settings.watermark.enabled && (
            <>
              <Field label="Watermark Type">
                <Dropdown
                  value={settings.watermark.type}
                  onOptionSelect={(_, data) =>
                    updateWatermark({ type: data.optionValue as WatermarkType })
                  }
                >
                  <Option value="Text">Text</Option>
                  <Option value="Image">Image</Option>
                </Dropdown>
              </Field>

              {settings.watermark.type === 'Text' ? (
                <>
                  <Field label="Watermark Text">
                    <Input
                      value={settings.watermark.text}
                      onChange={(e) => updateWatermark({ text: e.target.value })}
                      placeholder="© 2025 Your Brand"
                    />
                  </Field>
                  <div className={styles.row}>
                    <Field label="Font Family">
                      <Input
                        value={settings.watermark.fontFamily}
                        onChange={(e) => updateWatermark({ fontFamily: e.target.value })}
                      />
                    </Field>
                    <Field label="Font Size">
                      <Input
                        type="number"
                        value={settings.watermark.fontSize.toString()}
                        onChange={(e) => updateWatermark({ fontSize: parseInt(e.target.value) })}
                      />
                    </Field>
                  </div>
                  <Field label="Font Color">
                    <Input
                      type="text"
                      value={settings.watermark.fontColor}
                      onChange={(e) => updateWatermark({ fontColor: e.target.value })}
                      placeholder="#FFFFFF"
                    />
                  </Field>
                  <Field label="Enable Shadow">
                    <Switch
                      checked={settings.watermark.enableShadow}
                      onChange={(_, data) => updateWatermark({ enableShadow: data.checked })}
                    />
                  </Field>
                </>
              ) : (
                <Field label="Watermark Image Path">
                  <Input
                    value={settings.watermark.imagePath}
                    onChange={(e) => updateWatermark({ imagePath: e.target.value })}
                    placeholder="C:\path\to\watermark.png"
                  />
                </Field>
              )}

              <Field label="Position">
                <Dropdown
                  value={settings.watermark.position}
                  onOptionSelect={(_, data) =>
                    updateWatermark({ position: data.optionValue as WatermarkPosition })
                  }
                >
                  <Option value="TopLeft">Top Left</Option>
                  <Option value="TopCenter">Top Center</Option>
                  <Option value="TopRight">Top Right</Option>
                  <Option value="MiddleLeft">Middle Left</Option>
                  <Option value="Center">Center</Option>
                  <Option value="MiddleRight">Middle Right</Option>
                  <Option value="BottomLeft">Bottom Left</Option>
                  <Option value="BottomCenter">Bottom Center</Option>
                  <Option value="BottomRight">Bottom Right</Option>
                </Dropdown>
              </Field>

              <div className={styles.row}>
                <Field label={`Opacity: ${Math.round(settings.watermark.opacity * 100)}%`}>
                  <Slider
                    min={0}
                    max={1}
                    step={0.1}
                    value={settings.watermark.opacity}
                    onChange={(_, data) => updateWatermark({ opacity: data.value })}
                  />
                </Field>
                <Field label={`Scale: ${Math.round(settings.watermark.scale * 100)}%`}>
                  <Slider
                    min={0.05}
                    max={0.5}
                    step={0.05}
                    value={settings.watermark.scale}
                    onChange={(_, data) => updateWatermark({ scale: data.value })}
                  />
                </Field>
              </div>

              <div className={styles.row}>
                <Field label="Horizontal Offset (px)">
                  <Input
                    type="number"
                    value={settings.watermark.offsetX.toString()}
                    onChange={(e) => updateWatermark({ offsetX: parseInt(e.target.value) })}
                  />
                </Field>
                <Field label="Vertical Offset (px)">
                  <Input
                    type="number"
                    value={settings.watermark.offsetY.toString()}
                    onChange={(e) => updateWatermark({ offsetY: parseInt(e.target.value) })}
                  />
                </Field>
              </div>
            </>
          )}
        </div>
      </Card>

      {/* Naming Pattern Settings */}
      <Card className={styles.section}>
        <Title2>Output File Naming Pattern</Title2>
        <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
          Customize how exported files are named. Available placeholders: {'{'}project{'}'}, {'{'}
          date{'}'}, {'{'}time{'}'}, {'{'}preset{'}'}, {'{'}resolution{'}'}, {'{'}duration{'}'},{' '}
          {'{'}counter{'}'}
        </Text>

        <div className={styles.form}>
          <Field label="Naming Pattern">
            <Input
              value={settings.namingPattern.pattern}
              onChange={(e) => updateNamingPattern({ pattern: e.target.value })}
              placeholder="{project}_{date}_{time}"
            />
            <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
              Preview: MyProject_2025-11-10_143052.mp4
            </Text>
          </Field>

          <div className={styles.row}>
            <Field label="Date Format">
              <Input
                value={settings.namingPattern.dateFormat}
                onChange={(e) => updateNamingPattern({ dateFormat: e.target.value })}
                placeholder="yyyy-MM-dd"
              />
            </Field>
            <Field label="Time Format">
              <Input
                value={settings.namingPattern.timeFormat}
                onChange={(e) => updateNamingPattern({ timeFormat: e.target.value })}
                placeholder="HHmmss"
              />
            </Field>
          </div>

          <div className={styles.row}>
            <Field label="Custom Prefix">
              <Input
                value={settings.namingPattern.customPrefix}
                onChange={(e) => updateNamingPattern({ customPrefix: e.target.value })}
                placeholder="Optional prefix"
              />
            </Field>
            <Field label="Custom Suffix">
              <Input
                value={settings.namingPattern.customSuffix}
                onChange={(e) => updateNamingPattern({ customSuffix: e.target.value })}
                placeholder="Optional suffix"
              />
            </Field>
          </div>

          <div className={styles.row}>
            <Field label="Counter Start">
              <Input
                type="number"
                value={settings.namingPattern.counterStart.toString()}
                onChange={(e) => updateNamingPattern({ counterStart: parseInt(e.target.value) })}
              />
            </Field>
            <Field label="Counter Digits (zero-padding)">
              <Input
                type="number"
                value={settings.namingPattern.counterDigits.toString()}
                onChange={(e) => updateNamingPattern({ counterDigits: parseInt(e.target.value) })}
              />
            </Field>
          </div>

          <Field label="Sanitize Filenames">
            <Switch
              checked={settings.namingPattern.sanitizeFilenames}
              onChange={(_, data) => updateNamingPattern({ sanitizeFilenames: data.checked })}
            />
            <Text size={200}>Remove special characters that may cause issues</Text>
          </Field>

          <Field label="Replace Spaces with Underscores">
            <Switch
              checked={settings.namingPattern.replaceSpaces}
              onChange={(_, data) => updateNamingPattern({ replaceSpaces: data.checked })}
            />
          </Field>

          <Field label="Force Lowercase">
            <Switch
              checked={settings.namingPattern.forceLowercase}
              onChange={(_, data) => updateNamingPattern({ forceLowercase: data.checked })}
            />
          </Field>
        </div>
      </Card>

      {/* Upload Destinations */}
      <Card className={styles.section}>
        <div className={styles.cardHeader}>
          <div>
            <Title2>Auto-Upload Destinations</Title2>
            <Text size={200}>Configure automatic upload locations for exported videos</Text>
          </div>
          <Button appearance="primary" icon={<Add24Regular />} onClick={addUploadDestination}>
            Add Destination
          </Button>
        </div>

        {settings.uploadDestinations.length === 0 ? (
          <Text>No upload destinations configured. Add one to get started.</Text>
        ) : (
          settings.uploadDestinations.map((dest) => (
            <Card key={dest.id} className={styles.destinationCard}>
              <div className={styles.cardHeader}>
                <Title3>{dest.name}</Title3>
                <div style={{ display: 'flex', gap: tokens.spacingHorizontalS }}>
                  <Switch
                    checked={dest.enabled}
                    onChange={(_, data) =>
                      updateUploadDestination(dest.id, { enabled: data.checked })
                    }
                  />
                  <Button
                    appearance="subtle"
                    icon={<Delete24Regular />}
                    onClick={() => removeUploadDestination(dest.id)}
                  />
                </div>
              </div>

              <div className={styles.form}>
                <Field label="Destination Name">
                  <Input
                    value={dest.name}
                    onChange={(e) => updateUploadDestination(dest.id, { name: e.target.value })}
                  />
                </Field>

                <Field label="Destination Type">
                  <Dropdown
                    value={dest.type}
                    onOptionSelect={(_, data) =>
                      updateUploadDestination(dest.id, {
                        type: data.optionValue as UploadDestinationType,
                      })
                    }
                  >
                    <Option value="LocalFolder">Local Folder</Option>
                    <Option value="FTP">FTP</Option>
                    <Option value="SFTP">SFTP</Option>
                    <Option value="S3">Amazon S3</Option>
                    <Option value="AzureBlob">Azure Blob Storage</Option>
                    <Option value="GoogleDrive">Google Drive</Option>
                    <Option value="Dropbox">Dropbox</Option>
                  </Dropdown>
                </Field>

                {dest.type === 'LocalFolder' && (
                  <Field label="Local Path">
                    <Input
                      value={dest.localPath}
                      onChange={(e) =>
                        updateUploadDestination(dest.id, { localPath: e.target.value })
                      }
                      placeholder="C:\Exports\Videos"
                    />
                  </Field>
                )}

                {(dest.type === 'FTP' || dest.type === 'SFTP') && (
                  <>
                    <div className={styles.row}>
                      <Field label="Host">
                        <Input
                          value={dest.host}
                          onChange={(e) =>
                            updateUploadDestination(dest.id, { host: e.target.value })
                          }
                          placeholder="ftp.example.com"
                        />
                      </Field>
                      <Field label="Port">
                        <Input
                          type="number"
                          value={dest.port.toString()}
                          onChange={(e) =>
                            updateUploadDestination(dest.id, { port: parseInt(e.target.value) })
                          }
                        />
                      </Field>
                    </div>
                    <div className={styles.row}>
                      <Field label="Username">
                        <Input
                          value={dest.username}
                          onChange={(e) =>
                            updateUploadDestination(dest.id, { username: e.target.value })
                          }
                        />
                      </Field>
                      <Field label="Password">
                        <Input
                          type="password"
                          value={dest.password}
                          onChange={(e) =>
                            updateUploadDestination(dest.id, { password: e.target.value })
                          }
                        />
                      </Field>
                    </div>
                    <Field label="Remote Path">
                      <Input
                        value={dest.remotePath}
                        onChange={(e) =>
                          updateUploadDestination(dest.id, { remotePath: e.target.value })
                        }
                      />
                    </Field>
                  </>
                )}

                {dest.type === 'S3' && (
                  <>
                    <Field label="Bucket Name">
                      <Input
                        value={dest.s3BucketName}
                        onChange={(e) =>
                          updateUploadDestination(dest.id, { s3BucketName: e.target.value })
                        }
                      />
                    </Field>
                    <Field label="Region">
                      <Input
                        value={dest.s3Region}
                        onChange={(e) =>
                          updateUploadDestination(dest.id, { s3Region: e.target.value })
                        }
                      />
                    </Field>
                    <div className={styles.row}>
                      <Field label="Access Key">
                        <Input
                          value={dest.s3AccessKey}
                          onChange={(e) =>
                            updateUploadDestination(dest.id, { s3AccessKey: e.target.value })
                          }
                        />
                      </Field>
                      <Field label="Secret Key">
                        <Input
                          type="password"
                          value={dest.s3SecretKey}
                          onChange={(e) =>
                            updateUploadDestination(dest.id, { s3SecretKey: e.target.value })
                          }
                        />
                      </Field>
                    </div>
                  </>
                )}

                <Field label="Delete Local File After Upload">
                  <Switch
                    checked={dest.deleteAfterUpload}
                    onChange={(_, data) =>
                      updateUploadDestination(dest.id, { deleteAfterUpload: data.checked })
                    }
                  />
                </Field>
              </div>
            </Card>
          ))
        )}
      </Card>

      {/* General Export Options */}
      <Card className={styles.section}>
        <Title2>General Export Options</Title2>
        <div className={styles.form}>
          <Field label="Auto-open Output Folder">
            <Switch
              checked={settings.autoOpenOutputFolder}
              onChange={(_, data) => onChange({ ...settings, autoOpenOutputFolder: data.checked })}
            />
            <Text size={200}>Automatically open the output folder after export completes</Text>
          </Field>

          <Field label="Auto-upload on Complete">
            <Switch
              checked={settings.autoUploadOnComplete}
              onChange={(_, data) => onChange({ ...settings, autoUploadOnComplete: data.checked })}
            />
            <Text size={200}>Start upload automatically when export finishes</Text>
          </Field>

          <Field label="Generate Thumbnail">
            <Switch
              checked={settings.generateThumbnail}
              onChange={(_, data) => onChange({ ...settings, generateThumbnail: data.checked })}
            />
            <Text size={200}>Create a thumbnail image alongside the video</Text>
          </Field>

          <Field label="Generate Subtitles">
            <Switch
              checked={settings.generateSubtitles}
              onChange={(_, data) => onChange({ ...settings, generateSubtitles: data.checked })}
            />
            <Text size={200}>Export subtitles as separate SRT file if available</Text>
          </Field>

          <Field label="Keep Intermediate Files">
            <Switch
              checked={settings.keepIntermediateFiles}
              onChange={(_, data) => onChange({ ...settings, keepIntermediateFiles: data.checked })}
            />
            <Text size={200}>Save temporary files for debugging (increases disk usage)</Text>
          </Field>
        </div>
      </Card>

      <div className={styles.actions}>
        <Button
          appearance="primary"
          icon={<Save24Regular />}
          onClick={onSave}
          disabled={!hasChanges}
        >
          Save Export Settings
        </Button>
      </div>
    </>
  );
}
