/**
 * GraphicCard Component
 *
 * Individual graphic preview card with hover-to-preview,
 * drag to timeline support, and quick-add functionality.
 */

import {
  makeStyles,
  tokens,
  Text,
  Tooltip,
  Button,
  mergeClasses,
  Badge,
} from '@fluentui/react-components';
import { Add24Regular, Timer24Regular } from '@fluentui/react-icons';
import { useState, useCallback, useRef } from 'react';
import type { FC, DragEvent, MouseEvent } from 'react';
import type { MotionGraphicAsset } from '../../../types/motionGraphics';

export interface GraphicCardProps {
  /** The graphic asset to display */
  asset: MotionGraphicAsset;
  /** Whether this card is selected */
  isSelected?: boolean;
  /** Called when the card is clicked */
  onClick?: () => void;
  /** Called when the card is double-clicked */
  onDoubleClick?: () => void;
  /** Called when quick-add button is clicked */
  onAdd?: () => void;
  /** Called when drag starts */
  onDragStart?: (e: DragEvent) => void;
  /** Called when drag ends */
  onDragEnd?: () => void;
}

const useStyles = makeStyles({
  card: {
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    overflow: 'hidden',
    cursor: 'pointer',
    border: '2px solid transparent',
    transition: 'all 150ms ease-out',
    position: 'relative',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3Hover,
      transform: 'translateY(-2px)',
      boxShadow: tokens.shadow8,
      border: `2px solid ${tokens.colorBrandStroke1}`,
    },
    ':active': {
      transform: 'translateY(0)',
    },
  },
  cardSelected: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3Hover,
    boxShadow: tokens.shadow8,
  },
  cardDragging: {
    opacity: 0.5,
    transform: 'scale(0.95)',
  },
  thumbnail: {
    aspectRatio: '16 / 9',
    backgroundColor: tokens.colorNeutralBackground4,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    position: 'relative',
    overflow: 'hidden',
  },
  thumbnailPlaceholder: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    gap: tokens.spacingVerticalXS,
    color: tokens.colorNeutralForeground3,
    padding: tokens.spacingHorizontalS,
    textAlign: 'center',
    fontSize: tokens.fontSizeBase100,
  },
  thumbnailImage: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
  },
  overlay: {
    position: 'absolute',
    inset: 0,
    background: 'linear-gradient(transparent 40%, rgba(0,0,0,0.6) 100%)',
    opacity: 0,
    transition: 'opacity 150ms ease-out',
    display: 'flex',
    alignItems: 'flex-end',
    justifyContent: 'space-between',
    padding: tokens.spacingHorizontalXS,
  },
  overlayVisible: {
    opacity: 1,
  },
  duration: {
    backgroundColor: 'rgba(0, 0, 0, 0.75)',
    color: 'white',
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalXS}`,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase100,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXXS,
  },
  addButton: {
    opacity: 0,
    transition: 'opacity 150ms ease-out',
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    ':hover': {
      backgroundColor: 'rgba(0, 0, 0, 0.8)',
    },
  },
  addButtonVisible: {
    opacity: 1,
  },
  info: {
    padding: tokens.spacingHorizontalS,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  name: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase200,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  category: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
    textTransform: 'capitalize',
  },
  premiumBadge: {
    position: 'absolute',
    top: tokens.spacingVerticalXS,
    right: tokens.spacingHorizontalXS,
    zIndex: 1,
  },
});

/**
 * Format duration in seconds to a readable string
 */
function formatDuration(seconds: number): string {
  if (seconds < 60) {
    return `${seconds.toFixed(1)}s`;
  }
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}

/**
 * Get category display name
 */
function getCategoryDisplayName(category: string): string {
  return category.replace(/-/g, ' ');
}

export const GraphicCard: FC<GraphicCardProps> = ({
  asset,
  isSelected = false,
  onClick,
  onDoubleClick,
  onAdd,
  onDragStart,
  onDragEnd,
}) => {
  const styles = useStyles();
  const [isHovered, setIsHovered] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const cardRef = useRef<HTMLDivElement>(null);

  const handleMouseEnter = useCallback(() => {
    setIsHovered(true);
  }, []);

  const handleMouseLeave = useCallback(() => {
    setIsHovered(false);
  }, []);

  const handleDragStart = useCallback(
    (e: DragEvent) => {
      setIsDragging(true);
      e.dataTransfer.setData('application/x-opencut-graphic', asset.id);
      e.dataTransfer.effectAllowed = 'copy';
      onDragStart?.(e);
    },
    [asset.id, onDragStart]
  );

  const handleDragEnd = useCallback(() => {
    setIsDragging(false);
    onDragEnd?.();
  }, [onDragEnd]);

  const handleAddClick = useCallback(
    (e: MouseEvent) => {
      e.stopPropagation();
      onAdd?.();
    },
    [onAdd]
  );

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        onClick?.();
      }
    },
    [onClick]
  );

  return (
    <Tooltip content={asset.description} relationship="description" positioning="below">
      <div
        ref={cardRef}
        className={mergeClasses(
          styles.card,
          isSelected && styles.cardSelected,
          isDragging && styles.cardDragging
        )}
        onClick={onClick}
        onDoubleClick={onDoubleClick}
        onMouseEnter={handleMouseEnter}
        onMouseLeave={handleMouseLeave}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
        onKeyDown={handleKeyDown}
        draggable
        role="button"
        tabIndex={0}
        aria-label={`${asset.name} graphic`}
        aria-pressed={isSelected}
      >
        {/* Thumbnail */}
        <div className={styles.thumbnail}>
          {asset.thumbnail ? (
            <img
              src={asset.thumbnail}
              alt={asset.name}
              className={styles.thumbnailImage}
              draggable={false}
            />
          ) : (
            <div className={styles.thumbnailPlaceholder}>
              <Text size={200}>{asset.name}</Text>
            </div>
          )}

          {/* Premium Badge */}
          {asset.isPremium && (
            <div className={styles.premiumBadge}>
              <Badge appearance="filled" color="warning" size="small">
                PRO
              </Badge>
            </div>
          )}

          {/* Hover Overlay */}
          <div className={mergeClasses(styles.overlay, isHovered && styles.overlayVisible)}>
            <span className={styles.duration}>
              <Timer24Regular style={{ width: 12, height: 12 }} />
              {formatDuration(asset.duration)}
            </span>
          </div>
        </div>

        {/* Quick Add Button */}
        <Tooltip content="Add at playhead" relationship="label">
          <Button
            appearance="subtle"
            size="small"
            icon={<Add24Regular />}
            className={mergeClasses(styles.addButton, isHovered && styles.addButtonVisible)}
            onClick={handleAddClick}
            aria-label={`Add ${asset.name} to timeline`}
            style={{
              position: 'absolute',
              top: tokens.spacingVerticalXS,
              left: tokens.spacingHorizontalXS,
            }}
          />
        </Tooltip>

        {/* Info */}
        <div className={styles.info}>
          <Text className={styles.name}>{asset.name}</Text>
          <Text className={styles.category}>{getCategoryDisplayName(asset.category)}</Text>
        </div>
      </div>
    </Tooltip>
  );
};

export default GraphicCard;
