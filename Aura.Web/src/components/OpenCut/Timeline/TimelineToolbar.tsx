/**
 * TimelineToolbar Component
 *
 * Toolbar with editing mode toggles for magnetic timeline features.
 * Includes toggles for magnetic timeline, ripple edit, snap, and snap tolerance.
 */

import {
  makeStyles,
  mergeClasses,
  Button,
  Tooltip,
  Slider,
  Text,
  Popover,
  PopoverTrigger,
  PopoverSurface,
  Switch,
  Divider,
} from '@fluentui/react-components';
import {
  AlignCenterHorizontal24Regular,
  AlignCenterHorizontal24Filled,
  LinkSquare24Regular,
  LinkSquare24Filled,
  TargetArrow24Regular,
  TargetArrow24Filled,
  Settings24Regular,
  ArrowSync24Regular,
} from '@fluentui/react-icons';
import { motion } from 'framer-motion';
import { useState, useCallback } from 'react';
import type { FC } from 'react';
import { useOpenCutTimelineStore } from '../../../stores/opencutTimeline';
import { openCutTokens } from '../../../styles/designTokens';

export interface TimelineToolbarProps {
  /** Optional className */
  className?: string;
  /** Callback when close all gaps is triggered */
  onCloseAllGaps?: () => void;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
    padding: `${openCutTokens.spacing.xxs} ${openCutTokens.spacing.xs}`,
    backgroundColor: 'transparent',
  },
  toggleGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: '2px',
  },
  divider: {
    height: '24px',
    margin: `0 ${openCutTokens.spacing.xs}`,
  },
  toggleButton: {
    minWidth: '32px',
    minHeight: '32px',
    padding: '4px',
  },
  toggleButtonActive: {
    backgroundColor: 'rgba(139, 92, 246, 0.2)',
    color: openCutTokens.colors.snap,
    ':hover': {
      backgroundColor: 'rgba(139, 92, 246, 0.3)',
    },
  },
  settingsPopover: {
    padding: openCutTokens.spacing.md,
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.sm,
    minWidth: '200px',
  },
  settingRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: openCutTokens.spacing.sm,
  },
  settingLabel: {
    fontSize: openCutTokens.typography.fontSize.sm,
    color: 'var(--colorNeutralForeground2)',
  },
  sliderRow: {
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.xxs,
  },
  sliderLabel: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    fontSize: openCutTokens.typography.fontSize.sm,
    color: 'var(--colorNeutralForeground2)',
  },
  sliderValue: {
    fontSize: openCutTokens.typography.fontSize.xs,
    color: 'var(--colorNeutralForeground3)',
    fontFamily: openCutTokens.typography.fontFamily.mono,
  },
  actionButton: {
    marginTop: openCutTokens.spacing.xs,
  },
});

export const TimelineToolbar: FC<TimelineToolbarProps> = ({ className, onCloseAllGaps }) => {
  const styles = useStyles();
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);

  const {
    magneticTimelineEnabled,
    rippleEnabled,
    snapToClips,
    snapTolerance,
    setMagneticTimeline,
    setRippleEnabled,
    setSnapToClips,
    setSnapTolerance,
    closeAllGaps,
  } = useOpenCutTimelineStore();

  const handleToggleMagnetic = useCallback(() => {
    setMagneticTimeline(!magneticTimelineEnabled);
  }, [magneticTimelineEnabled, setMagneticTimeline]);

  const handleToggleRipple = useCallback(() => {
    setRippleEnabled(!rippleEnabled);
  }, [rippleEnabled, setRippleEnabled]);

  const handleToggleSnap = useCallback(() => {
    setSnapToClips(!snapToClips);
  }, [snapToClips, setSnapToClips]);

  const handleSnapToleranceChange = useCallback(
    (_: unknown, data: { value: number }) => {
      setSnapTolerance(data.value);
    },
    [setSnapTolerance]
  );

  const handleCloseAllGaps = useCallback(() => {
    closeAllGaps();
    onCloseAllGaps?.();
    setIsSettingsOpen(false);
  }, [closeAllGaps, onCloseAllGaps]);

  return (
    <motion.div
      className={mergeClasses(styles.container, className)}
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ duration: 0.2 }}
    >
      {/* Magnetic Timeline Toggle */}
      <div className={styles.toggleGroup}>
        <Tooltip content="Magnetic Timeline" relationship="label">
          <Button
            appearance="subtle"
            icon={
              magneticTimelineEnabled ? (
                <AlignCenterHorizontal24Filled />
              ) : (
                <AlignCenterHorizontal24Regular />
              )
            }
            className={mergeClasses(
              styles.toggleButton,
              magneticTimelineEnabled && styles.toggleButtonActive
            )}
            onClick={handleToggleMagnetic}
            aria-pressed={magneticTimelineEnabled}
            aria-label="Toggle magnetic timeline"
          />
        </Tooltip>
      </div>

      <Divider vertical className={styles.divider} />

      {/* Ripple Edit Toggle */}
      <div className={styles.toggleGroup}>
        <Tooltip content="Ripple Edit" relationship="label">
          <Button
            appearance="subtle"
            icon={rippleEnabled ? <LinkSquare24Filled /> : <LinkSquare24Regular />}
            className={mergeClasses(
              styles.toggleButton,
              rippleEnabled && styles.toggleButtonActive
            )}
            onClick={handleToggleRipple}
            aria-pressed={rippleEnabled}
            aria-label="Toggle ripple edit"
          />
        </Tooltip>
      </div>

      <Divider vertical className={styles.divider} />

      {/* Snap Toggle */}
      <div className={styles.toggleGroup}>
        <Tooltip content="Snap to Clips" relationship="label">
          <Button
            appearance="subtle"
            icon={snapToClips ? <TargetArrow24Filled /> : <TargetArrow24Regular />}
            className={mergeClasses(styles.toggleButton, snapToClips && styles.toggleButtonActive)}
            onClick={handleToggleSnap}
            aria-pressed={snapToClips}
            aria-label="Toggle snap to clips"
          />
        </Tooltip>
      </div>

      <Divider vertical className={styles.divider} />

      {/* Settings Popover */}
      <Popover open={isSettingsOpen} onOpenChange={(_, data) => setIsSettingsOpen(data.open)}>
        <PopoverTrigger disableButtonEnhancement>
          <Tooltip content="Magnetic Settings" relationship="label">
            <Button
              appearance="subtle"
              icon={<Settings24Regular />}
              className={styles.toggleButton}
              aria-label="Magnetic timeline settings"
            />
          </Tooltip>
        </PopoverTrigger>
        <PopoverSurface>
          <div className={styles.settingsPopover}>
            <Text weight="semibold">Magnetic Timeline Settings</Text>

            <Divider />

            <div className={styles.settingRow}>
              <Text className={styles.settingLabel}>Magnetic Timeline</Text>
              <Switch
                checked={magneticTimelineEnabled}
                onChange={(_, data) => setMagneticTimeline(data.checked)}
              />
            </div>

            <div className={styles.settingRow}>
              <Text className={styles.settingLabel}>Ripple Edit</Text>
              <Switch
                checked={rippleEnabled}
                onChange={(_, data) => setRippleEnabled(data.checked)}
              />
            </div>

            <div className={styles.settingRow}>
              <Text className={styles.settingLabel}>Snap to Clips</Text>
              <Switch checked={snapToClips} onChange={(_, data) => setSnapToClips(data.checked)} />
            </div>

            <Divider />

            <div className={styles.sliderRow}>
              <div className={styles.sliderLabel}>
                <Text className={styles.settingLabel}>Snap Tolerance</Text>
                <Text className={styles.sliderValue}>{snapTolerance}px</Text>
              </div>
              <Slider
                min={1}
                max={50}
                step={1}
                value={snapTolerance}
                onChange={handleSnapToleranceChange}
              />
            </div>

            <Divider />

            <Button
              appearance="primary"
              icon={<ArrowSync24Regular />}
              className={styles.actionButton}
              onClick={handleCloseAllGaps}
            >
              Close All Gaps
            </Button>
          </div>
        </PopoverSurface>
      </Popover>
    </motion.div>
  );
};

export default TimelineToolbar;
