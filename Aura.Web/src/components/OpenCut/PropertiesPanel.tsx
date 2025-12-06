/**
 * PropertiesPanel Component
 *
 * Properties panel for editing selected elements following Apple HIG.
 * Features transform controls, audio/video properties, text styling,
 * effects stack, and professional editing capabilities similar to Final Cut Pro.
 */

import {
  makeStyles,
  tokens,
  Text,
  mergeClasses,
  Slider,
  Input,
  Button,
  Dropdown,
  Option,
  Tooltip,
  Switch,
} from '@fluentui/react-components';
import {
  Settings24Regular,
  TextT24Regular,
  LinkSquare24Regular,
  SquareMultiple24Regular,
  ArrowRotateClockwise24Regular,
  ScaleFill24Regular,
  Pin24Regular,
  Speaker224Regular,
  TextFont24Regular,
  Blur24Regular,
  Sparkle24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import type { FC } from 'react';
import { useOpenCutKeyframesStore } from '../../stores/opencutKeyframes';
import { useOpenCutMediaStore } from '../../stores/opencutMedia';
import { useOpenCutPlaybackStore } from '../../stores/opencutPlayback';
import { useTextAnimationsStore } from '../../stores/opencutTextAnimations';
import { useOpenCutTimelineStore, type BlendMode } from '../../stores/opencutTimeline';
import { useOpenCutTransitionsStore } from '../../stores/opencutTransitions';
import { openCutTokens } from '../../styles/designTokens';
import { EffectStack } from './Effects';
import { EmptyState } from './EmptyState';
import { KeyframeDiamond } from './KeyframeEditor';
import { SpeedControls } from './Speed/SpeedControls';
import { AnimationPresetPicker, AnimationEditor } from './TextAnimations';
import { TransitionEditor } from './Transitions';

export interface PropertiesPanelProps {
  className?: string;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${openCutTokens.spacing.md} ${openCutTokens.spacing.lg}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    minHeight: openCutTokens.layout.panelHeaderHeight,
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
  },
  headerIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '18px',
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: openCutTokens.spacing.md,
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.md,
  },
  propertyGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.sm,
    padding: openCutTokens.spacing.md,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: openCutTokens.radius.md,
  },
  propertyGroupHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: openCutTokens.spacing.xs,
  },
  propertyGroupTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.xs,
    color: tokens.colorNeutralForeground2,
  },
  propertyGroupIcon: {
    fontSize: '16px',
    color: tokens.colorNeutralForeground3,
  },
  propertyRow: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
    minHeight: openCutTokens.layout.controlButtonSizeCompact,
  },
  propertyLabel: {
    color: tokens.colorNeutralForeground3,
    minWidth: '56px',
    fontSize: openCutTokens.typography.fontSize.sm,
  },
  propertyInput: {
    flex: 1,
    minWidth: '56px',
  },
  propertyInputSmall: {
    width: '64px',
    minWidth: '64px',
  },
  dualInputRow: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.xs,
    flex: 1,
  },
  inputLabel: {
    color: tokens.colorNeutralForeground4,
    fontSize: openCutTokens.typography.fontSize.xs,
    width: '12px',
    textAlign: 'center',
  },
  keyframeButton: {
    minWidth: '24px',
    minHeight: '24px',
    padding: '2px',
  },
  keyframeActive: {
    color: tokens.colorBrandForeground1,
  },
  linkButton: {
    minWidth: openCutTokens.layout.iconButtonSize,
    minHeight: openCutTokens.layout.iconButtonSize,
    padding: '2px',
  },
  sliderRow: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
    width: '100%',
  },
  slider: {
    flex: 1,
    minWidth: '72px',
  },
  sliderValue: {
    minWidth: '36px',
    textAlign: 'right',
    fontSize: openCutTokens.typography.fontSize.sm,
    color: tokens.colorNeutralForeground2,
    fontFamily: openCutTokens.typography.fontFamily.mono,
  },
  dropdown: {
    minWidth: '90px',
  },
  colorInput: {
    width: openCutTokens.layout.controlButtonSizeCompact,
    height: openCutTokens.layout.controlButtonSizeCompact,
    padding: 0,
    border: 'none',
    borderRadius: openCutTokens.radius.sm,
    cursor: 'pointer',
    overflow: 'hidden',
  },
  colorSwatch: {
    width: '100%',
    height: '100%',
  },
  selectedMediaInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.md,
  },
  mediaThumbnail: {
    width: '100%',
    aspectRatio: '16 / 9',
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: openCutTokens.radius.md,
    objectFit: 'cover',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
  },
  mediaName: {
    wordBreak: 'break-word',
  },
  switchRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    minHeight: openCutTokens.layout.controlButtonSizeCompact,
  },
  textArea: {
    minHeight: '56px',
    resize: 'vertical',
    fontFamily: 'inherit',
    width: '100%',
    padding: openCutTokens.spacing.sm,
    borderRadius: openCutTokens.radius.sm,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1,
    color: tokens.colorNeutralForeground1,
    fontSize: openCutTokens.typography.fontSize.sm,
  },
});

const BLEND_MODES: { value: BlendMode; label: string }[] = [
  { value: 'normal', label: 'Normal' },
  { value: 'multiply', label: 'Multiply' },
  { value: 'screen', label: 'Screen' },
  { value: 'overlay', label: 'Overlay' },
  { value: 'darken', label: 'Darken' },
  { value: 'lighten', label: 'Lighten' },
  { value: 'color-dodge', label: 'Color Dodge' },
  { value: 'color-burn', label: 'Color Burn' },
  { value: 'hard-light', label: 'Hard Light' },
  { value: 'soft-light', label: 'Soft Light' },
  { value: 'difference', label: 'Difference' },
  { value: 'exclusion', label: 'Exclusion' },
];

const FONT_FAMILIES = [
  'Inter, system-ui, sans-serif',
  'Arial, sans-serif',
  'Helvetica Neue, sans-serif',
  'Georgia, serif',
  'Times New Roman, serif',
  'Courier New, monospace',
  'SF Pro Display, system-ui, sans-serif',
];

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
}

function formatDuration(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}

export const PropertiesPanel: FC<PropertiesPanelProps> = ({ className }) => {
  const styles = useStyles();
  const mediaStore = useOpenCutMediaStore();
  const timelineStore = useOpenCutTimelineStore();
  const keyframesStore = useOpenCutKeyframesStore();
  const playbackStore = useOpenCutPlaybackStore();
  const transitionsStore = useOpenCutTransitionsStore();
  const textAnimationsStore = useTextAnimationsStore();

  const [aspectLocked, setAspectLocked] = useState(true);

  const selectedMedia = mediaStore.selectedMediaId
    ? mediaStore.getMediaById(mediaStore.selectedMediaId)
    : null;

  const selectedClips = timelineStore.getSelectedClips();
  const selectedClip = selectedClips.length === 1 ? selectedClips[0] : null;
  const currentTime = playbackStore.currentTime;

  // Get selected transition
  const selectedTransition = transitionsStore.getSelectedTransition();

  // Keyframe helpers
  const hasKeyframeAt = useCallback(
    (property: string): boolean => {
      if (!selectedClip) return false;
      const kf = keyframesStore.getKeyframeAtTime(selectedClip.id, property, currentTime);
      return !!kf;
    },
    [selectedClip, keyframesStore, currentTime]
  );

  const hasKeyframes = useCallback(
    (property: string): boolean => {
      if (!selectedClip) return false;
      return keyframesStore.hasKeyframes(selectedClip.id, property);
    },
    [selectedClip, keyframesStore]
  );

  const handleKeyframeToggle = useCallback(
    (property: string, value: number) => {
      if (!selectedClip) return;

      const existingKf = keyframesStore.getKeyframeAtTime(selectedClip.id, property, currentTime);
      if (existingKf) {
        keyframesStore.removeKeyframe(existingKf.id);
      } else {
        keyframesStore.addKeyframe(selectedClip.id, property, currentTime, value);
      }
    },
    [selectedClip, keyframesStore, currentTime]
  );

  const handleTransformChange = useCallback(
    (property: string, value: number) => {
      if (!selectedClip) return;

      if (aspectLocked && (property === 'scaleX' || property === 'scaleY')) {
        timelineStore.updateClipTransform(selectedClip.id, {
          scaleX: value,
          scaleY: value,
        });
      } else {
        timelineStore.updateClipTransform(selectedClip.id, { [property]: value });
      }
    },
    [selectedClip, aspectLocked, timelineStore]
  );

  const handleBlendModeChange = useCallback(
    (mode: BlendMode) => {
      if (!selectedClip) return;
      timelineStore.updateClipBlendMode(selectedClip.id, mode);
    },
    [selectedClip, timelineStore]
  );

  const handleAudioChange = useCallback(
    (property: string, value: number | boolean) => {
      if (!selectedClip) return;
      timelineStore.updateClipAudio(selectedClip.id, { [property]: value });
    },
    [selectedClip, timelineStore]
  );

  const handleTextChange = useCallback(
    (property: string, value: string | number) => {
      if (!selectedClip) return;
      timelineStore.updateClipText(selectedClip.id, { [property]: value });
    },
    [selectedClip, timelineStore]
  );

  const renderTransformSection = () => {
    if (!selectedClip) return null;
    const { transform, blendMode } = selectedClip;

    return (
      <div className={styles.propertyGroup}>
        <div className={styles.propertyGroupHeader}>
          <div className={styles.propertyGroupTitle}>
            <ScaleFill24Regular className={styles.propertyGroupIcon} />
            <Text weight="semibold" size={200}>
              Transform
            </Text>
          </div>
        </div>

        {/* Scale */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Scale
          </Text>
          <div className={styles.dualInputRow}>
            <Text className={styles.inputLabel}>X</Text>
            <Input
              className={styles.propertyInputSmall}
              type="number"
              value={transform.scaleX.toString()}
              onChange={(_, data) => handleTransformChange('scaleX', Number(data.value) || 0)}
              contentAfter={<Text size={100}>%</Text>}
              size="small"
            />
            <Text className={styles.inputLabel}>Y</Text>
            <Input
              className={styles.propertyInputSmall}
              type="number"
              value={transform.scaleY.toString()}
              onChange={(_, data) => handleTransformChange('scaleY', Number(data.value) || 0)}
              contentAfter={<Text size={100}>%</Text>}
              size="small"
              disabled={aspectLocked}
            />
            <Tooltip
              content={aspectLocked ? 'Unlock aspect ratio' : 'Lock aspect ratio'}
              relationship="label"
            >
              <Button
                appearance="subtle"
                size="small"
                className={styles.linkButton}
                icon={aspectLocked ? <LinkSquare24Regular /> : <SquareMultiple24Regular />}
                onClick={() => setAspectLocked(!aspectLocked)}
              />
            </Tooltip>
            <Tooltip
              content={hasKeyframeAt('scaleX') ? 'Remove keyframe' : 'Add keyframe'}
              relationship="label"
            >
              <span className={styles.keyframeButton}>
                <KeyframeDiamond
                  isActive={hasKeyframeAt('scaleX')}
                  color="scale"
                  size="small"
                  onClick={() => handleKeyframeToggle('scaleX', transform.scaleX)}
                  ariaLabel={
                    hasKeyframeAt('scaleX') ? 'Remove scale keyframe' : 'Add scale keyframe'
                  }
                />
              </span>
            </Tooltip>
          </div>
        </div>

        {/* Position */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Position
          </Text>
          <div className={styles.dualInputRow}>
            <Text className={styles.inputLabel}>X</Text>
            <Input
              className={styles.propertyInputSmall}
              type="number"
              value={transform.positionX.toString()}
              onChange={(_, data) => handleTransformChange('positionX', Number(data.value) || 0)}
              contentAfter={<Text size={100}>px</Text>}
              size="small"
            />
            <Text className={styles.inputLabel}>Y</Text>
            <Input
              className={styles.propertyInputSmall}
              type="number"
              value={transform.positionY.toString()}
              onChange={(_, data) => handleTransformChange('positionY', Number(data.value) || 0)}
              contentAfter={<Text size={100}>px</Text>}
              size="small"
            />
            <Tooltip
              content={hasKeyframeAt('positionX') ? 'Remove keyframe' : 'Add keyframe'}
              relationship="label"
            >
              <span className={styles.keyframeButton}>
                <KeyframeDiamond
                  isActive={hasKeyframeAt('positionX')}
                  color="position"
                  size="small"
                  onClick={() => handleKeyframeToggle('positionX', transform.positionX)}
                  ariaLabel={
                    hasKeyframeAt('positionX')
                      ? 'Remove position keyframe'
                      : 'Add position keyframe'
                  }
                />
              </span>
            </Tooltip>
          </div>
        </div>

        {/* Rotation */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Rotation
          </Text>
          <div className={styles.sliderRow}>
            <Slider
              className={styles.slider}
              min={-180}
              max={180}
              value={transform.rotation}
              onChange={(_, data) => handleTransformChange('rotation', data.value)}
              size="small"
            />
            <Text className={styles.sliderValue}>{transform.rotation}°</Text>
            <Tooltip content="Reset rotation" relationship="label">
              <Button
                appearance="subtle"
                size="small"
                className={styles.keyframeButton}
                icon={<ArrowRotateClockwise24Regular />}
                onClick={() => handleTransformChange('rotation', 0)}
              />
            </Tooltip>
            <Tooltip
              content={hasKeyframeAt('rotation') ? 'Remove keyframe' : 'Add keyframe'}
              relationship="label"
            >
              <span className={styles.keyframeButton}>
                <KeyframeDiamond
                  isActive={hasKeyframeAt('rotation')}
                  color="rotation"
                  size="small"
                  onClick={() => handleKeyframeToggle('rotation', transform.rotation)}
                  ariaLabel={
                    hasKeyframeAt('rotation') ? 'Remove rotation keyframe' : 'Add rotation keyframe'
                  }
                />
              </span>
            </Tooltip>
          </div>
        </div>

        {/* Opacity */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Opacity
          </Text>
          <div className={styles.sliderRow}>
            <Slider
              className={styles.slider}
              min={0}
              max={100}
              value={transform.opacity}
              onChange={(_, data) => handleTransformChange('opacity', data.value)}
              size="small"
            />
            <Text className={styles.sliderValue}>{transform.opacity}%</Text>
            <Tooltip
              content={hasKeyframeAt('opacity') ? 'Remove keyframe' : 'Add keyframe'}
              relationship="label"
            >
              <span className={styles.keyframeButton}>
                <KeyframeDiamond
                  isActive={hasKeyframeAt('opacity')}
                  color="opacity"
                  size="small"
                  onClick={() => handleKeyframeToggle('opacity', transform.opacity)}
                  ariaLabel={
                    hasKeyframeAt('opacity') ? 'Remove opacity keyframe' : 'Add opacity keyframe'
                  }
                />
              </span>
            </Tooltip>
          </div>
        </div>

        {/* Anchor Point */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Anchor
          </Text>
          <div className={styles.dualInputRow}>
            <Text className={styles.inputLabel}>X</Text>
            <Input
              className={styles.propertyInputSmall}
              type="number"
              value={transform.anchorX.toString()}
              onChange={(_, data) => handleTransformChange('anchorX', Number(data.value) || 0)}
              contentAfter={<Text size={100}>%</Text>}
              size="small"
            />
            <Text className={styles.inputLabel}>Y</Text>
            <Input
              className={styles.propertyInputSmall}
              type="number"
              value={transform.anchorY.toString()}
              onChange={(_, data) => handleTransformChange('anchorY', Number(data.value) || 0)}
              contentAfter={<Text size={100}>%</Text>}
              size="small"
            />
          </div>
        </div>

        {/* Blend Mode */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Blend
          </Text>
          <Dropdown
            className={styles.dropdown}
            value={BLEND_MODES.find((m) => m.value === blendMode)?.label || 'Normal'}
            onOptionSelect={(_, data) => handleBlendModeChange(data.optionValue as BlendMode)}
            size="small"
          >
            {BLEND_MODES.map((mode) => (
              <Option key={mode.value} value={mode.value}>
                {mode.label}
              </Option>
            ))}
          </Dropdown>
        </div>
      </div>
    );
  };

  const renderAudioSection = () => {
    if (!selectedClip?.audio) return null;
    const { audio } = selectedClip;

    return (
      <div className={styles.propertyGroup}>
        <div className={styles.propertyGroupHeader}>
          <div className={styles.propertyGroupTitle}>
            <Speaker224Regular className={styles.propertyGroupIcon} />
            <Text weight="semibold" size={200}>
              Audio
            </Text>
          </div>
        </div>

        {/* Volume */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Volume
          </Text>
          <div className={styles.sliderRow}>
            <Slider
              className={styles.slider}
              min={0}
              max={200}
              value={audio.volume}
              onChange={(_, data) => handleAudioChange('volume', data.value)}
              size="small"
            />
            <Text className={styles.sliderValue}>
              {audio.volume > 100
                ? `+${(((audio.volume - 100) / 100) * 6).toFixed(1)}`
                : audio.volume === 100
                  ? '0.0'
                  : `-${(((100 - audio.volume) / 100) * 48).toFixed(1)}`}{' '}
              dB
            </Text>
          </div>
        </div>

        {/* Pan */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Pan
          </Text>
          <div className={styles.sliderRow}>
            <Text size={100}>L</Text>
            <Slider
              className={styles.slider}
              min={-100}
              max={100}
              value={audio.pan}
              onChange={(_, data) => handleAudioChange('pan', data.value)}
              size="small"
            />
            <Text size={100}>R</Text>
            <Text className={styles.sliderValue}>{audio.pan}</Text>
          </div>
        </div>

        {/* Fade In */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Fade In
          </Text>
          <div className={styles.sliderRow}>
            <Slider
              className={styles.slider}
              min={0}
              max={5}
              step={0.1}
              value={audio.fadeInDuration}
              onChange={(_, data) => handleAudioChange('fadeInDuration', data.value)}
              size="small"
            />
            <Text className={styles.sliderValue}>{audio.fadeInDuration.toFixed(1)}s</Text>
          </div>
        </div>

        {/* Fade Out */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Fade Out
          </Text>
          <div className={styles.sliderRow}>
            <Slider
              className={styles.slider}
              min={0}
              max={5}
              step={0.1}
              value={audio.fadeOutDuration}
              onChange={(_, data) => handleAudioChange('fadeOutDuration', data.value)}
              size="small"
            />
            <Text className={styles.sliderValue}>{audio.fadeOutDuration.toFixed(1)}s</Text>
          </div>
        </div>

        {/* Mute */}
        <div className={styles.switchRow}>
          <Text size={200} className={styles.propertyLabel}>
            Mute
          </Text>
          <Switch
            checked={audio.muted}
            onChange={(_, data) => handleAudioChange('muted', data.checked)}
          />
        </div>
      </div>
    );
  };

  const renderTextSection = () => {
    if (!selectedClip?.text) return null;
    const { text } = selectedClip;

    return (
      <div className={styles.propertyGroup}>
        <div className={styles.propertyGroupHeader}>
          <div className={styles.propertyGroupTitle}>
            <TextFont24Regular className={styles.propertyGroupIcon} />
            <Text weight="semibold" size={200}>
              Text
            </Text>
          </div>
        </div>

        {/* Text Content */}
        <div className={styles.propertyRow}>
          <textarea
            className={styles.textArea}
            value={text.content}
            onChange={(e) => handleTextChange('content', e.target.value)}
            aria-label="Text content"
          />
        </div>

        {/* Font Family */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Font
          </Text>
          <Dropdown
            className={styles.propertyInput}
            value={text.fontFamily.split(',')[0]}
            onOptionSelect={(_, data) => handleTextChange('fontFamily', data.optionValue as string)}
            size="small"
          >
            {FONT_FAMILIES.map((font) => (
              <Option key={font} value={font}>
                {font.split(',')[0]}
              </Option>
            ))}
          </Dropdown>
        </div>

        {/* Font Size */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Size
          </Text>
          <Input
            className={styles.propertyInputSmall}
            type="number"
            value={text.fontSize.toString()}
            onChange={(_, data) => handleTextChange('fontSize', Number(data.value) || 24)}
            contentAfter={<Text size={100}>px</Text>}
            size="small"
          />
        </div>

        {/* Font Weight */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Weight
          </Text>
          <Dropdown
            className={styles.dropdown}
            value={text.fontWeight.toString()}
            onOptionSelect={(_, data) => handleTextChange('fontWeight', Number(data.optionValue))}
            size="small"
          >
            <Option value="100">Thin</Option>
            <Option value="300">Light</Option>
            <Option value="400">Regular</Option>
            <Option value="500">Medium</Option>
            <Option value="600">Semibold</Option>
            <Option value="700">Bold</Option>
            <Option value="900">Black</Option>
          </Dropdown>
        </div>

        {/* Text Alignment */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Align
          </Text>
          <Dropdown
            className={styles.dropdown}
            value={text.textAlign}
            onOptionSelect={(_, data) => handleTextChange('textAlign', data.optionValue as string)}
            size="small"
          >
            <Option value="left">Left</Option>
            <Option value="center">Center</Option>
            <Option value="right">Right</Option>
          </Dropdown>
        </div>

        {/* Text Color */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Color
          </Text>
          <input
            type="color"
            className={styles.colorInput}
            value={text.color}
            onChange={(e) => handleTextChange('color', e.target.value)}
            title="Text color"
          />
        </div>

        {/* Stroke */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Stroke
          </Text>
          <div className={styles.dualInputRow}>
            <input
              type="color"
              className={styles.colorInput}
              value={text.strokeColor}
              onChange={(e) => handleTextChange('strokeColor', e.target.value)}
              title="Stroke color"
            />
            <Input
              className={styles.propertyInputSmall}
              type="number"
              min={0}
              max={20}
              value={text.strokeWidth.toString()}
              onChange={(_, data) => handleTextChange('strokeWidth', Number(data.value) || 0)}
              contentAfter={<Text size={100}>px</Text>}
              size="small"
            />
          </div>
        </div>

        {/* Shadow */}
        <div className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            Shadow
          </Text>
          <div className={styles.sliderRow}>
            <Slider
              className={styles.slider}
              min={0}
              max={50}
              value={text.shadowBlur}
              onChange={(_, data) => handleTextChange('shadowBlur', data.value)}
              size="small"
            />
            <Text className={styles.sliderValue}>{text.shadowBlur}px</Text>
          </div>
        </div>
      </div>
    );
  };

  const renderFileInfoSection = () => {
    if (!selectedMedia) return null;

    return (
      <>
        {/* Media Preview */}
        <div className={styles.mediaThumbnail}>
          {selectedMedia.thumbnailUrl ? (
            <img
              src={selectedMedia.thumbnailUrl}
              alt={selectedMedia.name}
              style={{ width: '100%', height: '100%', objectFit: 'cover' }}
            />
          ) : (
            <TextT24Regular style={{ fontSize: '32px', color: tokens.colorNeutralForeground3 }} />
          )}
        </div>

        {/* File Info */}
        <div className={styles.propertyGroup}>
          <div className={styles.propertyGroupHeader}>
            <div className={styles.propertyGroupTitle}>
              <Pin24Regular className={styles.propertyGroupIcon} />
              <Text weight="semibold" size={200}>
                File Information
              </Text>
            </div>
          </div>
          <div className={styles.propertyRow}>
            <Text size={200} className={styles.propertyLabel}>
              Name
            </Text>
            <Text size={200} className={mergeClasses(styles.propertyInput, styles.mediaName)}>
              {selectedMedia.name}
            </Text>
          </div>
          <div className={styles.propertyRow}>
            <Text size={200} className={styles.propertyLabel}>
              Type
            </Text>
            <Text size={200}>
              {selectedMedia.type.charAt(0).toUpperCase() + selectedMedia.type.slice(1)}
            </Text>
          </div>
          {selectedMedia.file && (
            <div className={styles.propertyRow}>
              <Text size={200} className={styles.propertyLabel}>
                Size
              </Text>
              <Text size={200}>{formatBytes(selectedMedia.file.size)}</Text>
            </div>
          )}
          {selectedMedia.duration !== undefined && (
            <div className={styles.propertyRow}>
              <Text size={200} className={styles.propertyLabel}>
                Duration
              </Text>
              <Text size={200}>{formatDuration(selectedMedia.duration)}</Text>
            </div>
          )}
          {selectedMedia.width !== undefined && selectedMedia.height !== undefined && (
            <div className={styles.propertyRow}>
              <Text size={200} className={styles.propertyLabel}>
                Resolution
              </Text>
              <Text size={200}>
                {selectedMedia.width} × {selectedMedia.height}
              </Text>
            </div>
          )}
          {selectedMedia.fps !== undefined && (
            <div className={styles.propertyRow}>
              <Text size={200} className={styles.propertyLabel}>
                Frame Rate
              </Text>
              <Text size={200}>{selectedMedia.fps} fps</Text>
            </div>
          )}
        </div>
      </>
    );
  };

  const renderTransitionSection = () => {
    if (!selectedTransition) return null;

    return (
      <TransitionEditor
        transition={selectedTransition}
        onRemove={() => transitionsStore.selectTransition(null)}
      />
    );
  };

  const renderSpeedSection = () => {
    if (!selectedClip) return null;

    return <SpeedControls clipId={selectedClip.id} className={styles.propertyGroup} />;
  };

  const renderEffectsSection = () => {
    if (!selectedClip) return null;

    return <EffectStack clipId={selectedClip.id} className={styles.propertyGroup} />;
  };

  const renderTextAnimationsSection = () => {
    // Only show for text clips (clips with text property)
    if (!selectedClip?.text) return null;

    // Get animations for this clip
    const animations = textAnimationsStore.getAnimationsForTarget(selectedClip.id);
    const inAnimation = animations.find((a) => a.position === 'in');
    const outAnimation = animations.find((a) => a.position === 'out');
    const continuousAnimation = animations.find((a) => a.position === 'continuous');

    return (
      <div className={styles.propertyGroup}>
        <div className={styles.propertyGroupHeader}>
          <div className={styles.propertyGroupTitle}>
            <Sparkle24Regular className={styles.propertyGroupIcon} />
            <Text weight="semibold" size={200}>
              Text Animations
            </Text>
          </div>
        </div>

        {/* Entry Animation */}
        {inAnimation ? (
          <AnimationEditor
            animation={inAnimation}
            onRemove={() => textAnimationsStore.removeAnimation(inAnimation.id)}
          />
        ) : (
          <AnimationPresetPicker targetId={selectedClip.id} targetType="clip" position="in" />
        )}

        {/* Exit Animation */}
        {outAnimation ? (
          <AnimationEditor
            animation={outAnimation}
            onRemove={() => textAnimationsStore.removeAnimation(outAnimation.id)}
          />
        ) : (
          <AnimationPresetPicker targetId={selectedClip.id} targetType="clip" position="out" />
        )}

        {/* Continuous Animation */}
        {continuousAnimation ? (
          <AnimationEditor
            animation={continuousAnimation}
            onRemove={() => textAnimationsStore.removeAnimation(continuousAnimation.id)}
          />
        ) : (
          <AnimationPresetPicker
            targetId={selectedClip.id}
            targetType="clip"
            position="continuous"
          />
        )}
      </div>
    );
  };

  const hasSelection = selectedMedia || selectedClip || selectedTransition;

  return (
    <div className={mergeClasses(styles.container, className)}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Settings24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={400}>
            Properties
          </Text>
        </div>
      </div>

      <div className={styles.content}>
        {!hasSelection ? (
          <EmptyState
            icon={<TextT24Regular />}
            title="No selection"
            description="Select an element on the timeline or in the media library to view its properties"
            size="medium"
          />
        ) : (
          <div className={styles.selectedMediaInfo}>
            {renderFileInfoSection()}
            {renderTransformSection()}
            {renderSpeedSection()}
            {renderAudioSection()}
            {renderTextSection()}
            {renderTextAnimationsSection()}
            {renderEffectsSection()}
            {renderTransitionSection()}
          </div>
        )}
      </div>
    </div>
  );
};

export default PropertiesPanel;
