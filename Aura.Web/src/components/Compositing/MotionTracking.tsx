/**
 * Motion Tracking Component
 * Track objects and lock graphics/effects to movement
 */

import { makeStyles, tokens, Button, Label, Card, Input } from '@fluentui/react-components';
import {
  Location24Regular,
  Delete24Regular,
  Play24Regular,
  Stop24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import { TrackingPath } from '../../services/motionTrackingService';

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
  trackingList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  trackingItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  trackingInfo: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  controls: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  instructions: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    fontSize: tokens.fontSizeBase200,
  },
});

interface MotionTrackingProps {
  trackingPaths: Record<string, TrackingPath>;
  onStartTracking: (name: string) => void;
  onStopTracking: (pathId: string) => void;
  onRemoveTracking: (pathId: string) => void;
  isTracking: boolean;
}

export function MotionTracking({
  trackingPaths,
  onStartTracking,
  onStopTracking,
  onRemoveTracking,
  isTracking,
}: MotionTrackingProps) {
  const styles = useStyles();
  const [newTrackingName, setNewTrackingName] = useState('');

  const handleStartTracking = () => {
    if (newTrackingName.trim()) {
      onStartTracking(newTrackingName.trim());
      setNewTrackingName('');
    }
  };

  return (
    <Card className={styles.container}>
      <div className={styles.header}>
        <Label size="large" weight="semibold">
          Motion Tracking
        </Label>
      </div>

      <div className={styles.instructions}>
        <Label size="small">
          Click on the preview to set a tracking point, then play the video to track the point
          through the frames. Tracked points can be used to lock graphics and effects to moving
          objects.
        </Label>
      </div>

      {/* New Tracking Control */}
      <div style={{ display: 'flex', gap: tokens.spacingHorizontalS }}>
        <Input
          placeholder="Tracking point name"
          value={newTrackingName}
          onChange={(e) => setNewTrackingName(e.target.value)}
          style={{ flex: 1 }}
        />
        <Button
          appearance="primary"
          icon={<Location24Regular />}
          onClick={handleStartTracking}
          disabled={!newTrackingName.trim() || isTracking}
        >
          Add Point
        </Button>
      </div>

      {/* Tracking Points List */}
      {Object.keys(trackingPaths).length > 0 && (
        <div className={styles.trackingList}>
          <Label weight="semibold">Active Tracking Points</Label>
          {Object.entries(trackingPaths).map(([pathId, path]) => (
            <div key={pathId} className={styles.trackingItem}>
              <div className={styles.trackingInfo}>
                <Label weight="semibold">{path.name}</Label>
                <Label size="small">
                  {path.points.length} frames tracked • Start: {path.startFrame.toFixed(2)}s • End:{' '}
                  {path.endFrame.toFixed(2)}s
                </Label>
              </div>
              <div className={styles.controls}>
                {isTracking ? (
                  <Button
                    size="small"
                    appearance="subtle"
                    icon={<Stop24Regular />}
                    onClick={() => onStopTracking(pathId)}
                  >
                    Stop
                  </Button>
                ) : (
                  <Button
                    size="small"
                    appearance="subtle"
                    icon={<Play24Regular />}
                    onClick={() => onStartTracking(path.name)}
                  >
                    Track
                  </Button>
                )}
                <Button
                  size="small"
                  appearance="subtle"
                  icon={<Delete24Regular />}
                  onClick={() => onRemoveTracking(pathId)}
                />
              </div>
            </div>
          ))}
        </div>
      )}

      {Object.keys(trackingPaths).length === 0 && (
        <div style={{ textAlign: 'center', padding: tokens.spacingVerticalL }}>
          <Label size="small">No tracking points yet. Add a point to start tracking.</Label>
        </div>
      )}
    </Card>
  );
}
