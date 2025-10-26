/**
 * Text Animator Component
 * Animated text effects with presets
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
  Play24Regular,
  Stop24Regular,
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
  presets: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
  },
  presetButton: {
    height: '60px',
  },
  preview: {
    minHeight: '200px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    padding: tokens.spacingVerticalL,
    overflow: 'hidden',
  },
  previewText: {
    fontSize: '48px',
    fontWeight: tokens.fontWeightSemibold,
    textAlign: 'center',
    wordWrap: 'break-word',
  },
  controls: {
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
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'flex-end',
  },
});

export type TextAnimationPreset =
  | 'type-on'
  | 'fade-in-by-char'
  | 'bounce-in'
  | 'slide-in'
  | 'glitch'
  | 'tracking-in'
  | 'tracking-out';

interface TextAnimationConfig {
  preset: TextAnimationPreset;
  text: string;
  duration: number;
  fontSize: number;
  color: string;
  fontFamily: string;
  // Animation-specific parameters
  stagger?: number; // Delay between characters
  direction?: 'left' | 'right' | 'top' | 'bottom';
  bounceHeight?: number;
  glitchIntensity?: number;
  trackingAmount?: number;
}

interface TextAnimatorProps {
  onAnimationCreated?: (config: TextAnimationConfig) => void;
}

const PRESET_DESCRIPTIONS: Record<TextAnimationPreset, string> = {
  'type-on': 'Text appears character by character',
  'fade-in-by-char': 'Each character fades in sequentially',
  'bounce-in': 'Characters bounce in with spring effect',
  'slide-in': 'Text slides in from a direction',
  glitch: 'Digital glitch effect',
  'tracking-in': 'Letter spacing animates from wide to normal',
  'tracking-out': 'Letter spacing animates from normal to wide',
};

export function TextAnimator({ onAnimationCreated }: TextAnimatorProps) {
  const styles = useStyles();
  const [selectedPreset, setSelectedPreset] = useState<TextAnimationPreset>('type-on');
  const [isPlaying, setIsPlaying] = useState(false);

  // Configuration
  const [text, setText] = useState('Animated Text');
  const [duration, setDuration] = useState(2);
  const [fontSize, setFontSize] = useState(48);
  const [color, setColor] = useState('#ffffff');
  const [fontFamily, setFontFamily] = useState('Arial');
  const [stagger, setStagger] = useState(0.05);
  const [direction, setDirection] = useState<'left' | 'right' | 'top' | 'bottom'>('left');
  const [bounceHeight, setBounceHeight] = useState(50);
  const [glitchIntensity, setGlitchIntensity] = useState(5);
  const [trackingAmount, setTrackingAmount] = useState(0.5);

  const getAnimationStyle = (): React.CSSProperties => {
    const baseStyle: React.CSSProperties = {
      fontSize: `${fontSize}px`,
      color,
      fontFamily,
    };

    if (!isPlaying) {
      return baseStyle;
    }

    // Apply different animations based on preset
    switch (selectedPreset) {
      case 'type-on':
        return {
          ...baseStyle,
          animation: `typeOn ${duration}s steps(${text.length}, end)`,
        };
      case 'fade-in-by-char':
        return {
          ...baseStyle,
          animation: `fadeIn ${duration}s ease-in`,
        };
      case 'bounce-in':
        return {
          ...baseStyle,
          animation: `bounceIn ${duration}s cubic-bezier(0.68, -0.55, 0.265, 1.55)`,
        };
      case 'slide-in':
        return {
          ...baseStyle,
          animation: `slideIn${direction.charAt(0).toUpperCase() + direction.slice(1)} ${duration}s ease-out`,
        };
      case 'glitch':
        return {
          ...baseStyle,
          animation: `glitch ${duration}s infinite`,
        };
      case 'tracking-in':
        return {
          ...baseStyle,
          animation: `trackingIn ${duration}s ease-out`,
        };
      case 'tracking-out':
        return {
          ...baseStyle,
          animation: `trackingOut ${duration}s ease-in`,
        };
      default:
        return baseStyle;
    }
  };

  const handlePresetSelect = (preset: TextAnimationPreset) => {
    setSelectedPreset(preset);
    setIsPlaying(false);
  };

  const handleTogglePlay = () => {
    setIsPlaying(!isPlaying);
    // Reset animation by forcing re-render
    if (!isPlaying) {
      setTimeout(() => setIsPlaying(true), 10);
    }
  };

  const handleCreate = () => {
    const config: TextAnimationConfig = {
      preset: selectedPreset,
      text,
      duration,
      fontSize,
      color,
      fontFamily,
      stagger,
      direction,
      bounceHeight,
      glitchIntensity,
      trackingAmount,
    };

    onAnimationCreated?.(config);
    setIsPlaying(false);
  };

  return (
    <div className={styles.container}>
      <Card>
        <div className={styles.header}>
          <Label weight="semibold">Text Animation Presets</Label>
          <div style={{ display: 'flex', gap: tokens.spacingHorizontalS }}>
            <Button
              appearance={isPlaying ? 'secondary' : 'primary'}
              icon={isPlaying ? <Stop24Regular /> : <Play24Regular />}
              onClick={handleTogglePlay}
            >
              {isPlaying ? 'Stop' : 'Preview'}
            </Button>
            <Button appearance="primary" icon={<Checkmark24Regular />} onClick={handleCreate}>
              Create
            </Button>
          </div>
        </div>

        <div className={styles.presets}>
          <Button
            className={styles.presetButton}
            appearance={selectedPreset === 'type-on' ? 'primary' : 'secondary'}
            onClick={() => handlePresetSelect('type-on')}
          >
            Type On
          </Button>
          <Button
            className={styles.presetButton}
            appearance={selectedPreset === 'fade-in-by-char' ? 'primary' : 'secondary'}
            onClick={() => handlePresetSelect('fade-in-by-char')}
          >
            Fade In by Char
          </Button>
          <Button
            className={styles.presetButton}
            appearance={selectedPreset === 'bounce-in' ? 'primary' : 'secondary'}
            onClick={() => handlePresetSelect('bounce-in')}
          >
            Bounce In
          </Button>
          <Button
            className={styles.presetButton}
            appearance={selectedPreset === 'slide-in' ? 'primary' : 'secondary'}
            onClick={() => handlePresetSelect('slide-in')}
          >
            Slide In
          </Button>
          <Button
            className={styles.presetButton}
            appearance={selectedPreset === 'glitch' ? 'primary' : 'secondary'}
            onClick={() => handlePresetSelect('glitch')}
          >
            Glitch
          </Button>
          <Button
            className={styles.presetButton}
            appearance={selectedPreset === 'tracking-in' ? 'primary' : 'secondary'}
            onClick={() => handlePresetSelect('tracking-in')}
          >
            Tracking In
          </Button>
          <Button
            className={styles.presetButton}
            appearance={selectedPreset === 'tracking-out' ? 'primary' : 'secondary'}
            onClick={() => handlePresetSelect('tracking-out')}
          >
            Tracking Out
          </Button>
        </div>

        <Divider />

        <div style={{ padding: tokens.spacingVerticalM }}>
          <Label size="small" style={{ color: tokens.colorNeutralForeground2 }}>
            {PRESET_DESCRIPTIONS[selectedPreset]}
          </Label>
        </div>
      </Card>

      <div className={styles.preview}>
        <div className={styles.previewText} style={getAnimationStyle()}>
          {text}
        </div>
      </div>

      <Card>
        <div className={styles.controls}>
          <div className={styles.controlRow}>
            <Label>Text</Label>
            <Input value={text} onChange={(_, data) => setText(data.value)} />
          </div>

          <div className={styles.controlRow}>
            <Label>Duration: {duration}s</Label>
            <Slider
              min={0.5}
              max={5}
              step={0.1}
              value={duration}
              onChange={(_, data) => setDuration(data.value)}
            />
          </div>

          <div className={styles.controlRow}>
            <Label>Font Size: {fontSize}px</Label>
            <Slider
              min={20}
              max={120}
              step={1}
              value={fontSize}
              onChange={(_, data) => setFontSize(data.value)}
            />
          </div>

          <div className={styles.controlRow}>
            <Label>Color</Label>
            <input
              type="color"
              value={color}
              onChange={(e) => setColor(e.target.value)}
              style={{ width: '100%', height: '32px' }}
            />
          </div>

          <div className={styles.controlRow}>
            <Label>Font Family</Label>
            <Select value={fontFamily} onChange={(_, data) => setFontFamily(data.value)}>
              <option value="Arial">Arial</option>
              <option value="Helvetica">Helvetica</option>
              <option value="Times New Roman">Times New Roman</option>
              <option value="Courier New">Courier New</option>
              <option value="Georgia">Georgia</option>
              <option value="Verdana">Verdana</option>
              <option value="Impact">Impact</option>
            </Select>
          </div>

          {(selectedPreset === 'type-on' || selectedPreset === 'fade-in-by-char') && (
            <div className={styles.controlRow}>
              <Label>Character Delay: {stagger}s</Label>
              <Slider
                min={0.01}
                max={0.2}
                step={0.01}
                value={stagger}
                onChange={(_, data) => setStagger(data.value)}
              />
            </div>
          )}

          {selectedPreset === 'slide-in' && (
            <div className={styles.controlRow}>
              <Label>Direction</Label>
              <Select
                value={direction}
                onChange={(_, data) => setDirection(data.value as typeof direction)}
              >
                <option value="left">From Left</option>
                <option value="right">From Right</option>
                <option value="top">From Top</option>
                <option value="bottom">From Bottom</option>
              </Select>
            </div>
          )}

          {selectedPreset === 'bounce-in' && (
            <div className={styles.controlRow}>
              <Label>Bounce Height: {bounceHeight}px</Label>
              <Slider
                min={20}
                max={200}
                step={10}
                value={bounceHeight}
                onChange={(_, data) => setBounceHeight(data.value)}
              />
            </div>
          )}

          {selectedPreset === 'glitch' && (
            <div className={styles.controlRow}>
              <Label>Glitch Intensity: {glitchIntensity}</Label>
              <Slider
                min={1}
                max={20}
                step={1}
                value={glitchIntensity}
                onChange={(_, data) => setGlitchIntensity(data.value)}
              />
            </div>
          )}

          {(selectedPreset === 'tracking-in' || selectedPreset === 'tracking-out') && (
            <div className={styles.controlRow}>
              <Label>Tracking Amount: {trackingAmount}em</Label>
              <Slider
                min={0.1}
                max={2}
                step={0.1}
                value={trackingAmount}
                onChange={(_, data) => setTrackingAmount(data.value)}
              />
            </div>
          )}
        </div>
      </Card>

      {/* Add CSS animations */}
      <style>{`
        @keyframes typeOn {
          from { width: 0; }
          to { width: 100%; }
        }
        
        @keyframes fadeIn {
          from { opacity: 0; }
          to { opacity: 1; }
        }
        
        @keyframes bounceIn {
          0% { transform: translateY(-${bounceHeight}px); opacity: 0; }
          50% { transform: translateY(${bounceHeight / 4}px); }
          100% { transform: translateY(0); opacity: 1; }
        }
        
        @keyframes slideInLeft {
          from { transform: translateX(-100%); opacity: 0; }
          to { transform: translateX(0); opacity: 1; }
        }
        
        @keyframes slideInRight {
          from { transform: translateX(100%); opacity: 0; }
          to { transform: translateX(0); opacity: 1; }
        }
        
        @keyframes slideInTop {
          from { transform: translateY(-100%); opacity: 0; }
          to { transform: translateY(0); opacity: 1; }
        }
        
        @keyframes slideInBottom {
          from { transform: translateY(100%); opacity: 0; }
          to { transform: translateY(0); opacity: 1; }
        }
        
        @keyframes glitch {
          0%, 100% { transform: translate(0); }
          20% { transform: translate(-${glitchIntensity}px, ${glitchIntensity}px); }
          40% { transform: translate(${glitchIntensity}px, -${glitchIntensity}px); }
          60% { transform: translate(-${glitchIntensity}px, -${glitchIntensity}px); }
          80% { transform: translate(${glitchIntensity}px, ${glitchIntensity}px); }
        }
        
        @keyframes trackingIn {
          from { letter-spacing: ${trackingAmount}em; opacity: 0; }
          to { letter-spacing: normal; opacity: 1; }
        }
        
        @keyframes trackingOut {
          from { letter-spacing: normal; opacity: 1; }
          to { letter-spacing: ${trackingAmount}em; opacity: 0; }
        }
      `}</style>
    </div>
  );
}
