/**
 * KeyframeableProperty Component
 *
 * Wrapper for properties that can be keyframed. Shows a diamond icon button
 * to toggle keyframing, navigate between keyframes, and add keyframes at
 * the current playhead position.
 */

import { makeStyles, tokens, Button, Tooltip, mergeClasses } from '@fluentui/react-components';
import { ChevronLeft16Regular, ChevronRight16Regular } from '@fluentui/react-icons';
import { useCallback, useMemo } from 'react';
import type { FC, ReactNode } from 'react';
import { useOpenCutKeyframesStore } from '../../../stores/opencutKeyframes';
import { useOpenCutPlaybackStore } from '../../../stores/opencutPlayback';
import { KeyframeDiamond } from './KeyframeDiamond';

export interface KeyframeablePropertyProps {
  /** The clip ID this property belongs to */
  clipId: string;
  /** The property name being keyframed */
  property: string;
  /** Current value of the property */
  value: number | string;
  /** Called when user wants to set keyframe at current time */
  onAddKeyframe?: (time: number, value: number | string) => void;
  /** Called when user navigates to a keyframe */
  onNavigateToKeyframe?: (time: number) => void;
  /** The property control to render */
  children: ReactNode;
  /** Color variant for the diamond */
  color?: 'default' | 'position' | 'scale' | 'rotation' | 'opacity' | 'audio';
  /** Whether the control is disabled */
  disabled?: boolean;
  /** Additional class name */
  className?: string;
  /** Whether to show navigation buttons */
  showNavigation?: boolean;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    width: '100%',
  },
  controls: {
    display: 'flex',
    alignItems: 'center',
    gap: '2px',
    flexShrink: 0,
  },
  content: {
    flex: 1,
    minWidth: 0,
  },
  navButton: {
    minWidth: '20px',
    minHeight: '20px',
    padding: '2px',
  },
  keyframeButton: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    minWidth: '24px',
    minHeight: '24px',
  },
  animated: {
    backgroundColor: tokens.colorNeutralBackground1Hover,
    borderRadius: tokens.borderRadiusSmall,
  },
});

export const KeyframeableProperty: FC<KeyframeablePropertyProps> = ({
  clipId,
  property,
  value,
  onAddKeyframe,
  onNavigateToKeyframe,
  children,
  color = 'default',
  disabled = false,
  className,
  showNavigation = true,
}) => {
  const styles = useStyles();
  const playbackStore = useOpenCutPlaybackStore();
  const keyframesStore = useOpenCutKeyframesStore();

  const currentTime = playbackStore.currentTime;

  // Check if property has any keyframes
  const hasKeyframes = useMemo(
    () => keyframesStore.hasKeyframes(clipId, property),
    [keyframesStore, clipId, property]
  );

  // Get keyframe at current time if exists
  const keyframeAtTime = useMemo(
    () => keyframesStore.getKeyframeAtTime(clipId, property, currentTime),
    [keyframesStore, clipId, property, currentTime]
  );

  // Get adjacent keyframes for navigation
  const { prev: prevKeyframe, next: nextKeyframe } = useMemo(
    () => keyframesStore.getAdjacentKeyframes(clipId, property, currentTime),
    [keyframesStore, clipId, property, currentTime]
  );

  const handleKeyframeClick = useCallback(() => {
    if (keyframeAtTime) {
      // If there's a keyframe at current time, remove it
      keyframesStore.removeKeyframe(keyframeAtTime.id);
    } else {
      // Add new keyframe at current time
      keyframesStore.addKeyframe(clipId, property, currentTime, value);
      onAddKeyframe?.(currentTime, value);
    }
  }, [keyframeAtTime, keyframesStore, clipId, property, currentTime, value, onAddKeyframe]);

  const handlePrevKeyframe = useCallback(() => {
    if (prevKeyframe) {
      playbackStore.seek(prevKeyframe.time);
      onNavigateToKeyframe?.(prevKeyframe.time);
    }
  }, [prevKeyframe, playbackStore, onNavigateToKeyframe]);

  const handleNextKeyframe = useCallback(() => {
    if (nextKeyframe) {
      playbackStore.seek(nextKeyframe.time);
      onNavigateToKeyframe?.(nextKeyframe.time);
    }
  }, [nextKeyframe, playbackStore, onNavigateToKeyframe]);

  const isAnimated = hasKeyframes;
  const isAtKeyframe = !!keyframeAtTime;

  return (
    <div className={mergeClasses(styles.container, isAnimated && styles.animated, className)}>
      <div className={styles.content}>{children}</div>
      <div className={styles.controls}>
        {showNavigation && hasKeyframes && (
          <>
            <Tooltip content="Previous keyframe ([)" relationship="label">
              <Button
                appearance="subtle"
                size="small"
                icon={<ChevronLeft16Regular />}
                className={styles.navButton}
                onClick={handlePrevKeyframe}
                disabled={disabled || !prevKeyframe}
                aria-label="Go to previous keyframe"
              />
            </Tooltip>
          </>
        )}

        <Tooltip
          content={isAtKeyframe ? 'Remove keyframe' : 'Add keyframe at current time (K)'}
          relationship="label"
        >
          <span className={styles.keyframeButton}>
            <KeyframeDiamond
              isActive={isAtKeyframe}
              isSelected={false}
              color={color}
              size="medium"
              onClick={handleKeyframeClick}
              disabled={disabled}
              ariaLabel={isAtKeyframe ? 'Remove keyframe' : 'Add keyframe'}
            />
          </span>
        </Tooltip>

        {showNavigation && hasKeyframes && (
          <Tooltip content="Next keyframe (])" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<ChevronRight16Regular />}
              className={styles.navButton}
              onClick={handleNextKeyframe}
              disabled={disabled || !nextKeyframe}
              aria-label="Go to next keyframe"
            />
          </Tooltip>
        )}
      </div>
    </div>
  );
};

export default KeyframeableProperty;
