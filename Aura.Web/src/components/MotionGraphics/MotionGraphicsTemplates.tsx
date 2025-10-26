/**
 * Motion Graphics Templates Component
 * Pre-built customizable motion graphics templates
 */

import { useState } from 'react';
import {
  makeStyles,
  tokens,
  Button,
  Label,
  Card,
  Input,
  Select,
  Slider,
  Divider,
} from '@fluentui/react-components';
import {
  VideoClip24Regular,
  TextBulletListLtr24Regular,
  DataBarVertical24Regular,
  Timer24Regular,
  ShareScreenPerson24Regular,
  Checkmark24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  templates: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
  },
  templateCard: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    cursor: 'pointer',
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground2,
    transition: 'all 0.2s ease',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground2Hover,
    },
  },
  templateCardSelected: {
    backgroundColor: tokens.colorBrandBackground2,
  },
  templateIcon: {
    display: 'flex',
    justifyContent: 'center',
    fontSize: '48px',
    color: tokens.colorBrandForeground1,
  },
  preview: {
    minHeight: '150px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    padding: tokens.spacingVerticalL,
    overflow: 'hidden',
  },
  customization: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  controlRow: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
});

export type TemplateType = 'lower-third' | 'call-out' | 'progress-bar' | 'timer' | 'social-bug';

export interface MotionGraphicsTemplate {
  id: string;
  type: TemplateType;
  name: string;
  // Customizable parameters
  text?: string;
  subtitle?: string;
  color?: string;
  backgroundColor?: string;
  position?: 'top-left' | 'top-right' | 'bottom-left' | 'bottom-right' | 'center';
  size?: number;
  // Type-specific
  progress?: number; // For progress bar
  duration?: number; // For timer
  logo?: string; // For social bug
}

interface MotionGraphicsTemplatesProps {
  onTemplateCreated?: (template: MotionGraphicsTemplate) => void;
}

const TEMPLATE_DEFINITIONS: Record<
  TemplateType,
  {
    name: string;
    description: string;
    icon: React.ReactNode;
  }
> = {
  'lower-third': {
    name: 'Lower Third',
    description: 'Name and title overlay for the lower portion of the screen',
    icon: <TextBulletListLtr24Regular />,
  },
  'call-out': {
    name: 'Call-Out',
    description: 'Highlight and annotate specific areas of your video',
    icon: <VideoClip24Regular />,
  },
  'progress-bar': {
    name: 'Progress Bar',
    description: 'Animated progress indicator',
    icon: <DataBarVertical24Regular />,
  },
  timer: {
    name: 'Timer',
    description: 'Countdown or count-up timer display',
    icon: <Timer24Regular />,
  },
  'social-bug': {
    name: 'Social Media Bug',
    description: 'Persistent branding element in corner',
    icon: <ShareScreenPerson24Regular />,
  },
};

export function MotionGraphicsTemplates({ onTemplateCreated }: MotionGraphicsTemplatesProps) {
  const styles = useStyles();
  const [selectedType, setSelectedType] = useState<TemplateType>('lower-third');

  // Template customization
  const [text, setText] = useState('Your Name');
  const [subtitle, setSubtitle] = useState('Your Title');
  const [color, setColor] = useState('#ffffff');
  const [backgroundColor, setBackgroundColor] = useState('#0066cc');
  const [position, setPosition] = useState<MotionGraphicsTemplate['position']>('bottom-left');
  const [size, setSize] = useState(100);
  const [progress, setProgress] = useState(50);
  const [duration, setDuration] = useState(60);

  const handleCreate = () => {
    const template: MotionGraphicsTemplate = {
      id: `template-${Date.now()}`,
      type: selectedType,
      name: TEMPLATE_DEFINITIONS[selectedType].name,
      text,
      subtitle,
      color,
      backgroundColor,
      position,
      size,
      progress,
      duration,
    };

    onTemplateCreated?.(template);
  };

  const renderPreview = () => {
    const previewStyles: React.CSSProperties = {
      color,
      backgroundColor,
      padding: '16px 24px',
      borderRadius: '8px',
      fontSize: `${size}%`,
    };

    switch (selectedType) {
      case 'lower-third':
        return (
          <div style={previewStyles}>
            <div style={{ fontSize: '1.5em', fontWeight: 'bold' }}>{text}</div>
            <div style={{ fontSize: '1em', opacity: 0.9 }}>{subtitle}</div>
          </div>
        );

      case 'call-out':
        return (
          <div
            style={{
              ...previewStyles,
              position: 'relative',
              display: 'inline-block',
            }}
          >
            <div style={{ fontSize: '1.2em', fontWeight: 'bold' }}>{text}</div>
            <div
              style={{
                position: 'absolute',
                bottom: '-20px',
                left: '50%',
                transform: 'translateX(-50%)',
                width: 0,
                height: 0,
                borderLeft: '10px solid transparent',
                borderRight: '10px solid transparent',
                borderTop: `10px solid ${backgroundColor}`,
              }}
            />
          </div>
        );

      case 'progress-bar':
        return (
          <div style={{ width: '80%', maxWidth: '400px' }}>
            <div
              style={{
                width: '100%',
                height: '24px',
                backgroundColor: tokens.colorNeutralBackground1,
                borderRadius: '12px',
                overflow: 'hidden',
              }}
            >
              <div
                style={{
                  width: `${progress}%`,
                  height: '100%',
                  backgroundColor,
                  transition: 'width 0.3s ease',
                }}
              />
            </div>
            <div style={{ textAlign: 'center', marginTop: '8px', color }}>
              {progress}%
            </div>
          </div>
        );

      case 'timer': {
        const minutes = Math.floor(duration / 60);
        const seconds = duration % 60;
        return (
          <div style={previewStyles}>
            <div style={{ fontSize: '3em', fontFamily: 'monospace', fontWeight: 'bold' }}>
              {minutes.toString().padStart(2, '0')}:{seconds.toString().padStart(2, '0')}
            </div>
          </div>
        );
      }

      case 'social-bug':
        return (
          <div
            style={{
              ...previewStyles,
              borderRadius: '50%',
              width: `${size}px`,
              height: `${size}px`,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '1.5em',
              fontWeight: 'bold',
            }}
          >
            {text.substring(0, 2).toUpperCase()}
          </div>
        );

      default:
        return null;
    }
  };

  return (
    <div className={styles.container}>
      <Card>
        <div className={styles.header}>
          <Label weight="semibold">Motion Graphics Templates</Label>
          <Button appearance="primary" icon={<Checkmark24Regular />} onClick={handleCreate}>
            Create Template
          </Button>
        </div>

        <div className={styles.templates}>
          {(Object.entries(TEMPLATE_DEFINITIONS) as Array<[TemplateType, typeof TEMPLATE_DEFINITIONS[TemplateType]]>).map(
            ([type, def]) => (
              <div
                key={type}
                className={`${styles.templateCard} ${selectedType === type ? styles.templateCardSelected : ''}`}
                onClick={() => setSelectedType(type)}
                role="button"
                tabIndex={0}
                aria-label={`Select ${def.name} template`}
              >
                <div className={styles.templateIcon}>{def.icon}</div>
                <Label weight="semibold">{def.name}</Label>
                <Label size="small" style={{ color: tokens.colorNeutralForeground3 }}>
                  {def.description}
                </Label>
              </div>
            )
          )}
        </div>
      </Card>

      <Card>
        <div style={{ padding: tokens.spacingVerticalM }}>
          <Label weight="semibold">Preview</Label>
          <Divider />
        </div>
        <div className={styles.preview}>{renderPreview()}</div>
      </Card>

      <Card>
        <div className={styles.customization}>
          <Label weight="semibold">Customize Template</Label>
          <Divider />

          <div className={styles.controlRow}>
            <Label>Text</Label>
            <Input value={text} onChange={(_, data) => setText(data.value)} />
          </div>

          {(selectedType === 'lower-third' || selectedType === 'call-out') && (
            <div className={styles.controlRow}>
              <Label>Subtitle</Label>
              <Input value={subtitle} onChange={(_, data) => setSubtitle(data.value)} />
            </div>
          )}

          <div className={styles.controlRow}>
            <Label>Text Color</Label>
            <input
              type="color"
              value={color}
              onChange={(e) => setColor(e.target.value)}
              style={{ width: '100%', height: '32px' }}
            />
          </div>

          <div className={styles.controlRow}>
            <Label>Background Color</Label>
            <input
              type="color"
              value={backgroundColor}
              onChange={(e) => setBackgroundColor(e.target.value)}
              style={{ width: '100%', height: '32px' }}
            />
          </div>

          {(selectedType === 'lower-third' || selectedType === 'call-out') && (
            <div className={styles.controlRow}>
              <Label>Position</Label>
              <Select
                value={position}
                onChange={(_, data) => setPosition(data.value as typeof position)}
              >
                <option value="top-left">Top Left</option>
                <option value="top-right">Top Right</option>
                <option value="bottom-left">Bottom Left</option>
                <option value="bottom-right">Bottom Right</option>
                <option value="center">Center</option>
              </Select>
            </div>
          )}

          <div className={styles.controlRow}>
            <Label>Size: {size}%</Label>
            <Slider
              min={50}
              max={200}
              value={size}
              onChange={(_, data) => setSize(data.value)}
            />
          </div>

          {selectedType === 'progress-bar' && (
            <div className={styles.controlRow}>
              <Label>Progress: {progress}%</Label>
              <Slider
                min={0}
                max={100}
                value={progress}
                onChange={(_, data) => setProgress(data.value)}
              />
            </div>
          )}

          {selectedType === 'timer' && (
            <div className={styles.controlRow}>
              <Label>Duration: {duration} seconds</Label>
              <Slider
                min={10}
                max={300}
                step={10}
                value={duration}
                onChange={(_, data) => setDuration(data.value)}
              />
            </div>
          )}
        </div>
      </Card>
    </div>
  );
}
