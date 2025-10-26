/**
 * Keyframe Editor Component
 * Timeline-based keyframe animation editor
 */

import { makeStyles, tokens, Button, Label, Card, Select } from '@fluentui/react-components';
import {
  Add24Regular,
  Delete24Regular,
  Diamond24Regular,
  DiamondFilled,
} from '@fluentui/react-icons';
import { useState } from 'react';
import { AnimationUtils } from '../../services/animationEngine';
import { Keyframe } from '../../types/effects';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalM,
  },
  propertyList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    maxHeight: '200px',
    overflowY: 'auto',
  },
  propertyItem: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusSmall,
    cursor: 'pointer',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground2Hover,
    },
  },
  propertyItemSelected: {
    backgroundColor: tokens.colorBrandBackground2,
    '&:hover': {
      backgroundColor: tokens.colorBrandBackground2Hover,
    },
  },
  timelineContainer: {
    position: 'relative',
    height: '80px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    overflow: 'hidden',
  },
  timelineRuler: {
    display: 'flex',
    alignItems: 'center',
    height: '30px',
    backgroundColor: tokens.colorNeutralBackground2,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    position: 'relative',
  },
  timelineTracks: {
    position: 'relative',
    height: '50px',
    cursor: 'pointer',
  },
  keyframeMarker: {
    position: 'absolute',
    top: '15px',
    width: '12px',
    height: '12px',
    transform: 'translateX(-6px) rotate(45deg)',
    cursor: 'pointer',
    zIndex: 2,
  },
  keyframeMarkerActive: {
    width: '16px',
    height: '16px',
    transform: 'translateX(-8px) rotate(45deg)',
    top: '13px',
  },
  playhead: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '2px',
    backgroundColor: tokens.colorPaletteRedBackground3,
    pointerEvents: 'none',
    zIndex: 3,
  },
  playheadHandle: {
    position: 'absolute',
    top: '-4px',
    left: '-5px',
    width: '12px',
    height: '12px',
    backgroundColor: tokens.colorPaletteRedBackground3,
    borderRadius: '50%',
  },
  controls: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  controlGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    flex: 1,
  },
});

export interface AnimatedProperty {
  name: string;
  label: string;
  keyframes: Keyframe[];
}

interface KeyframeEditorProps {
  properties: AnimatedProperty[];
  currentTime: number;
  duration: number;
  onKeyframeAdd?: (propertyName: string, keyframe: Keyframe) => void;
  onKeyframeRemove?: (propertyName: string, time: number) => void;
  onKeyframeUpdate?: (propertyName: string, time: number, updates: Partial<Keyframe>) => void;
  onTimeChange?: (time: number) => void;
}

export function KeyframeEditor({
  properties,
  currentTime,
  duration,
  onKeyframeAdd,
  onKeyframeRemove,
  onKeyframeUpdate,
  onTimeChange,
}: KeyframeEditorProps) {
  const styles = useStyles();
  const [selectedProperty, setSelectedProperty] = useState<string | null>(
    properties.length > 0 ? properties[0].name : null
  );
  const [selectedKeyframeTime, setSelectedKeyframeTime] = useState<number | null>(null);

  const pixelsPerSecond = 100;

  const selectedProp = properties.find((p) => p.name === selectedProperty);
  const selectedKeyframe = selectedProp?.keyframes.find((k) => k.time === selectedKeyframeTime);

  const handleTimelineClick = (e: React.MouseEvent<HTMLDivElement>) => {
    const rect = e.currentTarget.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const time = Math.max(0, Math.min(duration, x / pixelsPerSecond));
    onTimeChange?.(time);
  };

  const handleAddKeyframe = () => {
    if (!selectedProperty) return;

    const newKeyframe = AnimationUtils.createKeyframe(currentTime, 0, 'linear');
    onKeyframeAdd?.(selectedProperty, newKeyframe);
    setSelectedKeyframeTime(currentTime);
  };

  const handleDeleteKeyframe = () => {
    if (!selectedProperty || selectedKeyframeTime === null) return;

    onKeyframeRemove?.(selectedProperty, selectedKeyframeTime);
    setSelectedKeyframeTime(null);
  };

  const handleEasingChange = (easing: Keyframe['easing']) => {
    if (!selectedProperty || selectedKeyframeTime === null || !easing) return;

    onKeyframeUpdate?.(selectedProperty, selectedKeyframeTime, { easing });
  };

  const renderTimelineMarkers = () => {
    const markers = [];
    const interval = duration > 10 ? 1 : 0.5;

    for (let i = 0; i <= duration; i += interval) {
      const left = i * pixelsPerSecond;
      markers.push(
        <div
          key={i}
          style={{
            position: 'absolute',
            left: `${left}px`,
            height: i % 1 === 0 ? '10px' : '5px',
            width: '1px',
            backgroundColor: tokens.colorNeutralStroke2,
            bottom: 0,
          }}
        />
      );
    }

    return markers;
  };

  const renderKeyframes = () => {
    if (!selectedProp) return null;

    return selectedProp.keyframes.map((keyframe) => {
      const left = keyframe.time * pixelsPerSecond;
      const isSelected = keyframe.time === selectedKeyframeTime;

      return (
        <div
          key={keyframe.time}
          className={`${styles.keyframeMarker} ${isSelected ? styles.keyframeMarkerActive : ''}`}
          style={{
            left: `${left}px`,
            backgroundColor: tokens.colorBrandBackground,
            border: `1px solid ${tokens.colorBrandStroke1}`,
          }}
          onClick={(e) => {
            e.stopPropagation();
            setSelectedKeyframeTime(keyframe.time);
          }}
          onKeyDown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              e.preventDefault();
              e.stopPropagation();
              setSelectedKeyframeTime(keyframe.time);
            }
          }}
          role="button"
          tabIndex={0}
          aria-label={`Keyframe at ${keyframe.time.toFixed(2)}s`}
        />
      );
    });
  };

  return (
    <div className={styles.container}>
      <Card>
        <div className={styles.header}>
          <Label weight="semibold">Animated Properties</Label>
          <Button
            appearance="primary"
            icon={<Add24Regular />}
            onClick={handleAddKeyframe}
            disabled={!selectedProperty}
          >
            Add Keyframe
          </Button>
        </div>

        <div className={styles.propertyList}>
          {properties.map((prop) => (
            <div
              key={prop.name}
              className={`${styles.propertyItem} ${
                selectedProperty === prop.name ? styles.propertyItemSelected : ''
              }`}
              onClick={() => setSelectedProperty(prop.name)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  setSelectedProperty(prop.name);
                }
              }}
              role="button"
              tabIndex={0}
              aria-label={`Select ${prop.label}`}
            >
              <div
                style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
              >
                {prop.keyframes.length > 0 ? (
                  <DiamondFilled fontSize={16} color={tokens.colorBrandForeground1} />
                ) : (
                  <Diamond24Regular fontSize={16} />
                )}
                <Label>{prop.label}</Label>
              </div>
              <Label size="small" style={{ color: tokens.colorNeutralForeground3 }}>
                {prop.keyframes.length} keyframe{prop.keyframes.length !== 1 ? 's' : ''}
              </Label>
            </div>
          ))}
        </div>
      </Card>

      <Card>
        <div className={styles.timelineContainer}>
          <div className={styles.timelineRuler}>{renderTimelineMarkers()}</div>
          {/* Timeline tracks - intentionally clickable for seeking */}
          {/* eslint-disable-next-line jsx-a11y/click-events-have-key-events, jsx-a11y/no-static-element-interactions */}
          <div className={styles.timelineTracks} onClick={handleTimelineClick}>
            {renderKeyframes()}

            {/* Playhead */}
            <div className={styles.playhead} style={{ left: `${currentTime * pixelsPerSecond}px` }}>
              <div className={styles.playheadHandle} />
            </div>
          </div>
        </div>
      </Card>

      {selectedKeyframe && (
        <Card>
          <div className={styles.controls}>
            <div className={styles.controlGroup}>
              <Label>Easing</Label>
              <Select
                value={selectedKeyframe.easing || 'linear'}
                onChange={(_, data) => handleEasingChange(data.value as Keyframe['easing'])}
              >
                <option value="linear">Linear</option>
                <option value="ease-in">Ease In</option>
                <option value="ease-out">Ease Out</option>
                <option value="ease-in-out">Ease In-Out</option>
                <option value="bezier">Bezier (Custom)</option>
              </Select>
            </div>

            <div className={styles.controlGroup}>
              <Label>Time: {selectedKeyframe.time.toFixed(2)}s</Label>
              <Label>Value: {String(selectedKeyframe.value)}</Label>
            </div>

            <div style={{ display: 'flex', alignItems: 'flex-end' }}>
              <Button
                appearance="secondary"
                icon={<Delete24Regular />}
                onClick={handleDeleteKeyframe}
              >
                Delete
              </Button>
            </div>
          </div>
        </Card>
      )}
    </div>
  );
}
