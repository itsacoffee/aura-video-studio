/**
 * KeyframeTrack Component
 *
 * Inline keyframe visualization on timeline clips. Shows diamond markers
 * at keyframe positions that can be selected, moved, and edited.
 */

import {
  makeStyles,
  tokens,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  MenuDivider,
} from '@fluentui/react-components';
import { Delete16Regular } from '@fluentui/react-icons';
import { useCallback, useState, useRef, useEffect, useMemo } from 'react';
import type { FC, MouseEvent as ReactMouseEvent } from 'react';
import {
  useOpenCutKeyframesStore,
  type EasingType,
  type Keyframe,
} from '../../../stores/opencutKeyframes';
import { EasingPresets } from './EasingPresets';
import { KeyframeDiamond } from './KeyframeDiamond';

export interface KeyframeTrackProps {
  /** The clip ID to show keyframes for */
  clipId: string;
  /** The property to show keyframes for */
  property: string;
  /** Width of the track in pixels */
  width: number;
  /** Start time of the clip in seconds */
  clipStartTime: number;
  /** Duration of the clip in seconds */
  clipDuration: number;
  /** Pixels per second for scaling */
  pixelsPerSecond: number;
  /** Color variant for the diamonds */
  color?: 'default' | 'position' | 'scale' | 'rotation' | 'opacity' | 'audio';
  /** Called when a keyframe is double-clicked for editing */
  onKeyframeEdit?: (keyframe: Keyframe) => void;
  /** Called when a keyframe is moved */
  onKeyframeMoved?: (keyframeId: string, newTime: number) => void;
  /** Whether the track is disabled */
  disabled?: boolean;
  /** Additional class name */
  className?: string;
}

const useStyles = makeStyles({
  container: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    pointerEvents: 'none',
    overflow: 'hidden',
  },
  keyframe: {
    position: 'absolute',
    top: '50%',
    transform: 'translate(-50%, -50%)',
    pointerEvents: 'auto',
    zIndex: 10,
  },
  contextMenu: {
    minWidth: '160px',
  },
  easingRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalM}`,
  },
  easingLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  },
});

export const KeyframeTrack: FC<KeyframeTrackProps> = ({
  clipId,
  property,
  clipStartTime,
  clipDuration,
  pixelsPerSecond,
  color = 'default',
  onKeyframeEdit,
  onKeyframeMoved,
  disabled = false,
  className,
}) => {
  const styles = useStyles();
  const keyframesStore = useOpenCutKeyframesStore();

  const [contextMenuKeyframe, setContextMenuKeyframe] = useState<Keyframe | null>(null);
  const [contextMenuPosition, setContextMenuPosition] = useState<{ x: number; y: number } | null>(
    null
  );
  const [draggingKeyframe, setDraggingKeyframe] = useState<string | null>(null);
  const dragStartRef = useRef<{ x: number; time: number } | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  // Get keyframes for this property
  const track = useMemo(
    () => keyframesStore.getTrackForProperty(clipId, property),
    [keyframesStore, clipId, property]
  );
  const keyframes = track?.keyframes || [];
  const selectedKeyframeIds = keyframesStore.selectedKeyframeIds;

  // Calculate pixel position for a keyframe
  const getKeyframePosition = useCallback(
    (time: number): number => {
      // Time relative to clip start
      const relativeTime = time - clipStartTime;
      return relativeTime * pixelsPerSecond;
    },
    [clipStartTime, pixelsPerSecond]
  );

  // Handle keyframe click
  const handleKeyframeClick = useCallback(
    (keyframe: Keyframe, e: ReactMouseEvent) => {
      e.stopPropagation();
      if (disabled) return;
      keyframesStore.selectKeyframe(keyframe.id, e.shiftKey || e.metaKey || e.ctrlKey);
    },
    [keyframesStore, disabled]
  );

  // Handle keyframe double-click
  const handleKeyframeDoubleClick = useCallback(
    (keyframe: Keyframe, e: ReactMouseEvent) => {
      e.stopPropagation();
      if (disabled) return;
      onKeyframeEdit?.(keyframe);
    },
    [onKeyframeEdit, disabled]
  );

  // Handle context menu
  const handleContextMenu = useCallback(
    (keyframe: Keyframe, e: ReactMouseEvent) => {
      e.preventDefault();
      e.stopPropagation();
      if (disabled) return;
      setContextMenuKeyframe(keyframe);
      setContextMenuPosition({ x: e.clientX, y: e.clientY });
    },
    [disabled]
  );

  // Handle drag start
  const handleMouseDown = useCallback(
    (keyframe: Keyframe, e: ReactMouseEvent) => {
      if (disabled || e.button !== 0) return;
      e.stopPropagation();

      setDraggingKeyframe(keyframe.id);
      dragStartRef.current = { x: e.clientX, time: keyframe.time };

      // Select if not already selected
      if (!selectedKeyframeIds.includes(keyframe.id)) {
        keyframesStore.selectKeyframe(keyframe.id);
      }
    },
    [disabled, selectedKeyframeIds, keyframesStore]
  );

  // Handle dragging
  useEffect(() => {
    if (!draggingKeyframe) return;

    const handleMouseMove = (e: globalThis.MouseEvent) => {
      if (!dragStartRef.current || !containerRef.current) return;

      const deltaX = e.clientX - dragStartRef.current.x;
      const deltaTime = deltaX / pixelsPerSecond;
      const newTime = Math.max(
        clipStartTime,
        Math.min(clipStartTime + clipDuration, dragStartRef.current.time + deltaTime)
      );

      keyframesStore.moveKeyframe(draggingKeyframe, newTime);
    };

    const handleMouseUp = () => {
      if (draggingKeyframe && dragStartRef.current) {
        const keyframe = keyframes.find((k) => k.id === draggingKeyframe);
        if (keyframe) {
          onKeyframeMoved?.(draggingKeyframe, keyframe.time);
        }
      }
      setDraggingKeyframe(null);
      dragStartRef.current = null;
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [
    draggingKeyframe,
    keyframes,
    keyframesStore,
    pixelsPerSecond,
    clipStartTime,
    clipDuration,
    onKeyframeMoved,
  ]);

  // Handle easing change from context menu
  const handleEasingChange = useCallback(
    (easing: EasingType) => {
      if (contextMenuKeyframe) {
        keyframesStore.updateKeyframe(contextMenuKeyframe.id, { easing });
      }
      setContextMenuKeyframe(null);
      setContextMenuPosition(null);
    },
    [contextMenuKeyframe, keyframesStore]
  );

  // Handle delete from context menu
  const handleDelete = useCallback(() => {
    if (contextMenuKeyframe) {
      keyframesStore.removeKeyframe(contextMenuKeyframe.id);
    }
    setContextMenuKeyframe(null);
    setContextMenuPosition(null);
  }, [contextMenuKeyframe, keyframesStore]);

  // Close context menu
  const closeContextMenu = useCallback(() => {
    setContextMenuKeyframe(null);
    setContextMenuPosition(null);
  }, []);

  // Filter keyframes to only visible ones early
  const visibleKeyframes = useMemo(() => {
    return keyframes.filter((keyframe) => {
      const position = getKeyframePosition(keyframe.time);
      return position >= 0 && position <= clipDuration * pixelsPerSecond;
    });
  }, [keyframes, getKeyframePosition, clipDuration, pixelsPerSecond]);

  if (visibleKeyframes.length === 0) return null;

  return (
    <div ref={containerRef} className={`${styles.container} ${className || ''}`}>
      {visibleKeyframes.map((keyframe) => {
        const position = getKeyframePosition(keyframe.time);
        const isSelected = selectedKeyframeIds.includes(keyframe.id);
        const isDragging = draggingKeyframe === keyframe.id;

        return (
          <div
            key={keyframe.id}
            className={styles.keyframe}
            style={{
              left: position,
              cursor: isDragging ? 'grabbing' : 'grab',
            }}
            onContextMenu={(e) => handleContextMenu(keyframe, e)}
          >
            <KeyframeDiamond
              isActive
              isSelected={isSelected}
              color={color}
              size="small"
              onClick={(e) => handleKeyframeClick(keyframe, e)}
              onDoubleClick={(e) => handleKeyframeDoubleClick(keyframe, e)}
              onMouseDown={(e) => handleMouseDown(keyframe, e)}
              disabled={disabled}
              ariaLabel={`Keyframe at ${keyframe.time.toFixed(2)}s`}
            />
          </div>
        );
      })}

      {/* Context Menu */}
      {contextMenuKeyframe && contextMenuPosition && (
        <Menu
          open
          onOpenChange={(_, data) => {
            if (!data.open) closeContextMenu();
          }}
          positioning={{
            target: {
              getBoundingClientRect: () => ({
                x: contextMenuPosition.x,
                y: contextMenuPosition.y,
                width: 0,
                height: 0,
                top: contextMenuPosition.y,
                right: contextMenuPosition.x,
                bottom: contextMenuPosition.y,
                left: contextMenuPosition.x,
                toJSON: () => ({}),
              }),
            },
          }}
        >
          <MenuTrigger disableButtonEnhancement>
            <span />
          </MenuTrigger>
          <MenuPopover>
            <MenuList className={styles.contextMenu}>
              <div className={styles.easingRow}>
                <span className={styles.easingLabel}>Easing:</span>
                <EasingPresets
                  value={contextMenuKeyframe.easing}
                  onChange={handleEasingChange}
                  size="small"
                />
              </div>
              <MenuDivider />
              <MenuItem icon={<Delete16Regular />} onClick={handleDelete}>
                Delete Keyframe
              </MenuItem>
            </MenuList>
          </MenuPopover>
        </Menu>
      )}
    </div>
  );
};

export default KeyframeTrack;
