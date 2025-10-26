import {
  makeStyles,
  tokens,
  Title3,
  Text,
  Button,
  Card,
  Switch,
  Dropdown,
  Option,
  Input,
  Label,
} from '@fluentui/react-components';
import { Subtitles24Regular, ArrowDownload24Regular, Eye24Regular } from '@fluentui/react-icons';
import { useState } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  settingsGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingVerticalM,
    '@media (max-width: 768px)': {
      gridTemplateColumns: '1fr',
    },
  },
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  preview: {
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: 'monospace',
    fontSize: '12px',
    maxHeight: '200px',
    overflowY: 'auto',
    whiteSpace: 'pre-wrap',
  },
});

interface CaptionsPanelProps {
  scriptLines?: Array<{
    sceneIndex: number;
    text: string;
    start: string; // ISO 8601 duration format
    duration: string; // ISO 8601 duration format
  }>;
  onGenerate?: (format: 'srt' | 'vtt', burnIn: boolean, style: CaptionRenderStyle) => void;
  onExport?: (format: 'srt' | 'vtt') => void;
}

interface CaptionRenderStyle {
  fontName: string;
  fontSize: number;
  primaryColor: string;
  outlineColor: string;
  outlineWidth: number;
}

export function CaptionsPanel({ scriptLines = [], onGenerate, onExport }: CaptionsPanelProps) {
  const styles = useStyles();

  const [format, setFormat] = useState<'srt' | 'vtt'>('srt');
  const [burnIn, setBurnIn] = useState(false);
  const [style, setStyle] = useState<CaptionRenderStyle>({
    fontName: 'Arial',
    fontSize: 24,
    primaryColor: 'FFFFFF',
    outlineColor: '000000',
    outlineWidth: 2,
  });
  const [preview, setPreview] = useState<string>('');

  const handleGenerate = () => {
    if (onGenerate) {
      onGenerate(format, burnIn, style);
    }

    // Generate preview
    const previewText = generatePreview(scriptLines, format);
    setPreview(previewText);
  };

  const handleExport = () => {
    if (onExport) {
      onExport(format);
    }
  };

  const generatePreview = (
    lines: Array<{ sceneIndex: number; text: string; start: string; duration: string }>,
    captionFormat: 'srt' | 'vtt'
  ): string => {
    if (lines.length === 0) {
      return 'No script lines available. Generate a script first.';
    }

    const parseISODuration = (isoDuration: string): number => {
      // Parse ISO 8601 duration (e.g., "PT5.5S" = 5.5 seconds)
      // Pattern is safe: anchored with ^ and $, simple integer or decimal
      // eslint-disable-next-line security/detect-unsafe-regex
      const match = isoDuration.match(/^PT(\d+)(?:\.(\d+))?S$/);
      return match ? parseFloat(match[1] + (match[2] ? '.' + match[2] : '')) : 0;
    };

    const formatTime = (seconds: number, useDot: boolean): string => {
      const hours = Math.floor(seconds / 3600);
      const minutes = Math.floor((seconds % 3600) / 60);
      const secs = Math.floor(seconds % 60);
      const ms = Math.floor((seconds % 1) * 1000);
      const separator = useDot ? '.' : ',';
      return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}${separator}${ms.toString().padStart(3, '0')}`;
    };

    let result = '';

    if (captionFormat === 'vtt') {
      result = 'WEBVTT\n\n';
    }

    lines.slice(0, 3).forEach((line, index) => {
      const startSeconds = parseISODuration(line.start);
      const durationSeconds = parseISODuration(line.duration);
      const endSeconds = startSeconds + durationSeconds;

      if (captionFormat === 'srt') {
        result += `${index + 1}\n`;
        result += `${formatTime(startSeconds, false)} --> ${formatTime(endSeconds, false)}\n`;
        result += `${line.text}\n\n`;
      } else {
        result += `${formatTime(startSeconds, true)} --> ${formatTime(endSeconds, true)}\n`;
        result += `${line.text}\n\n`;
      }
    });

    if (lines.length > 3) {
      result += `... and ${lines.length - 3} more lines`;
    }

    return result;
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
          <Subtitles24Regular />
          <Title3>Captions &amp; Subtitles</Title3>
        </div>
      </div>

      <Card>
        <div className={styles.section}>
          <Text weight="semibold">Caption Settings</Text>

          <div className={styles.settingsGrid}>
            <div className={styles.field}>
              <Label htmlFor="format-select">Format</Label>
              <Dropdown
                id="format-select"
                value={format.toUpperCase()}
                onOptionSelect={(_, data) => setFormat(data.optionValue as 'srt' | 'vtt')}
              >
                <Option value="srt">SRT (SubRip)</Option>
                <Option value="vtt">VTT (WebVTT)</Option>
              </Dropdown>
            </div>

            <div className={styles.field}>
              <Label htmlFor="burn-in-switch">Burn-in to video</Label>
              <Switch
                id="burn-in-switch"
                checked={burnIn}
                onChange={(_, data) => setBurnIn(data.checked)}
                label={burnIn ? 'Enabled' : 'Disabled'}
              />
            </div>
          </div>

          {burnIn && (
            <>
              <Text weight="semibold">Burn-in Style</Text>
              <div className={styles.settingsGrid}>
                <div className={styles.field}>
                  <Label htmlFor="font-name">Font</Label>
                  <Dropdown
                    id="font-name"
                    value={style.fontName}
                    onOptionSelect={(_, data) =>
                      setStyle({ ...style, fontName: data.optionValue as string })
                    }
                  >
                    <Option value="Arial">Arial</Option>
                    <Option value="Helvetica">Helvetica</Option>
                    <Option value="Times New Roman">Times New Roman</Option>
                    <Option value="Courier New">Courier New</Option>
                  </Dropdown>
                </div>

                <div className={styles.field}>
                  <Label htmlFor="font-size">Font Size</Label>
                  <Input
                    id="font-size"
                    type="number"
                    value={style.fontSize.toString()}
                    onChange={(_, data) =>
                      setStyle({ ...style, fontSize: parseInt(data.value) || 24 })
                    }
                  />
                </div>

                <div className={styles.field}>
                  <Label htmlFor="primary-color">Text Color (Hex)</Label>
                  <Input
                    id="primary-color"
                    value={style.primaryColor}
                    onChange={(_, data) => setStyle({ ...style, primaryColor: data.value })}
                    placeholder="FFFFFF"
                  />
                </div>

                <div className={styles.field}>
                  <Label htmlFor="outline-color">Outline Color (Hex)</Label>
                  <Input
                    id="outline-color"
                    value={style.outlineColor}
                    onChange={(_, data) => setStyle({ ...style, outlineColor: data.value })}
                    placeholder="000000"
                  />
                </div>

                <div className={styles.field}>
                  <Label htmlFor="outline-width">Outline Width</Label>
                  <Input
                    id="outline-width"
                    type="number"
                    value={style.outlineWidth.toString()}
                    onChange={(_, data) =>
                      setStyle({ ...style, outlineWidth: parseInt(data.value) || 2 })
                    }
                  />
                </div>
              </div>
            </>
          )}

          <div className={styles.actions}>
            <Button
              appearance="primary"
              icon={<Subtitles24Regular />}
              onClick={handleGenerate}
              disabled={scriptLines.length === 0}
            >
              Generate Captions
            </Button>
            <Button
              appearance="secondary"
              icon={<ArrowDownload24Regular />}
              onClick={handleExport}
              disabled={preview === ''}
            >
              Export {format.toUpperCase()}
            </Button>
          </div>
        </div>
      </Card>

      {preview && (
        <Card>
          <div className={styles.section}>
            <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
              <Eye24Regular />
              <Text weight="semibold">Preview</Text>
            </div>
            <div className={styles.preview}>{preview}</div>
          </div>
        </Card>
      )}
    </div>
  );
}
