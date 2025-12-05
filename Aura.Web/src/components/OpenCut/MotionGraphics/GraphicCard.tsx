/**
 * GraphicCard Component
 *
 * Individual graphic preview card for the graphics browser panel.
 * Features animated thumbnail on hover, category badge, duration indicator,
 * drag handle for timeline placement, and quick-add button.
 */

import {
  makeStyles,
  tokens,
  Text,
  Button,
  Tooltip,
  Badge,
  mergeClasses,
} from '@fluentui/react-components';
import { Add24Regular, Drag24Regular } from '@fluentui/react-icons';
import { motion } from 'framer-motion';
import type { FC, DragEvent, MouseEvent } from 'react';
import { useState, useCallback } from 'react';
import { openCutTokens } from '../../../styles/designTokens';
import type { MotionGraphicAsset, GraphicCategory } from '../../../types/motionGraphics';

export interface GraphicCardProps {
  asset: MotionGraphicAsset;
  isSelected?: boolean;
  onSelect?: (assetId: string) => void;
  onAdd?: (assetId: string) => void;
  onDragStart?: (assetId: string, e: DragEvent) => void;
  onDragEnd?: () => void;
  onPreview?: (assetId: string | null) => void;
}

const useStyles = makeStyles({
  card: {
    aspectRatio: '16 / 9',
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    cursor: 'grab',
    border: `2px solid transparent`,
    transition: 'all 150ms ease-out',
    overflow: 'hidden',
    position: 'relative',
    ':hover': {
      border: `2px solid ${tokens.colorBrandStroke1}`,
      backgroundColor: tokens.colorNeutralBackground3,
      transform: 'scale(1.02)',
      boxShadow: tokens.shadow4,
    },
    ':focus-visible': {
      outline: `2px solid ${tokens.colorBrandStroke1}`,
      outlineOffset: '2px',
    },
    ':active': {
      cursor: 'grabbing',
    },
  },
  cardSelected: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
    boxShadow: tokens.shadow8,
  },
  cardDragging: {
    opacity: 0.5,
    transform: 'scale(0.95)',
  },
  thumbnail: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
  },
  placeholder: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    gap: tokens.spacingVerticalXS,
    color: tokens.colorNeutralForeground3,
    padding: openCutTokens.spacing.sm,
    textAlign: 'center',
  },
  placeholderIcon: {
    fontSize: '24px',
    color: tokens.colorNeutralForeground4,
  },
  placeholderText: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    maxWidth: '100%',
  },
  overlay: {
    position: 'absolute',
    inset: 0,
    background: 'linear-gradient(transparent 40%, rgba(0,0,0,0.7) 100%)',
    opacity: 0,
    transition: 'opacity 150ms ease-out',
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'flex-end',
    padding: tokens.spacingHorizontalS,
    ':hover': {
      opacity: 1,
    },
  },
  overlayVisible: {
    opacity: 1,
  },
  name: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
    color: 'white',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    textShadow: '0 1px 2px rgba(0,0,0,0.5)',
  },
  categoryBadge: {
    position: 'absolute',
    top: tokens.spacingVerticalXS,
    left: tokens.spacingHorizontalXS,
  },
  duration: {
    position: 'absolute',
    bottom: tokens.spacingVerticalXS,
    right: tokens.spacingHorizontalXS,
    backgroundColor: 'rgba(0, 0, 0, 0.75)',
    color: 'white',
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalXS}`,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase100,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
  },
  actions: {
    position: 'absolute',
    top: tokens.spacingVerticalXS,
    right: tokens.spacingHorizontalXS,
    display: 'flex',
    gap: '4px',
    opacity: 0,
    transition: 'opacity 150ms ease-out',
  },
  actionsVisible: {
    opacity: 1,
  },
  actionButton: {
    minWidth: '28px',
    minHeight: '28px',
    padding: '4px',
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    color: 'white',
    ':hover': {
      backgroundColor: 'rgba(0, 0, 0, 0.8)',
    },
  },
  premiumBadge: {
    position: 'absolute',
    top: tokens.spacingVerticalXS,
    left: tokens.spacingHorizontalXS,
  },
});

const categoryColors: Record<GraphicCategory, string> = {
  'lower-thirds': '#3B82F6',
  callouts: '#F59E0B',
  social: '#EC4899',
  titles: '#8B5CF6',
  'kinetic-text': '#10B981',
  shapes: '#6366F1',
  overlays: '#14B8A6',
  transitions: '#F97316',
  badges: '#84CC16',
  counters: '#06B6D4',
};

const categoryLabels: Record<GraphicCategory, string> = {
  'lower-thirds': 'Lower Third',
  callouts: 'Callout',
  social: 'Social',
  titles: 'Title',
  'kinetic-text': 'Kinetic',
  shapes: 'Shape',
  overlays: 'Overlay',
  transitions: 'Transition',
  badges: 'Badge',
  counters: 'Counter',
};

function formatDuration(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return mins > 0 ? `${mins}:${secs.toString().padStart(2, '0')}` : `${secs}s`;
}

export const GraphicCard: FC<GraphicCardProps> = ({
  asset,
  isSelected = false,
  onSelect,
  onAdd,
  onDragStart,
  onDragEnd,
  onPreview,
}) => {
  const styles = useStyles();
  const [isHovered, setIsHovered] = useState(false);
  const [isDragging, setIsDragging] = useState(false);

  const handleClick = useCallback(() => {
    onSelect?.(asset.id);
  }, [onSelect, asset.id]);

  const handleDoubleClick = useCallback(() => {
    onAdd?.(asset.id);
  }, [onAdd, asset.id]);

  const handleAddClick = useCallback(
    (e: MouseEvent) => {
      e.stopPropagation();
      onAdd?.(asset.id);
    },
    [onAdd, asset.id]
  );

  const handleDragStart = useCallback(
    (e: DragEvent) => {
      setIsDragging(true);
      e.dataTransfer.setData('application/x-opencut-graphic', asset.id);
      e.dataTransfer.effectAllowed = 'copy';
      onDragStart?.(asset.id, e);
    },
    [onDragStart, asset.id]
  );

  const handleDragEnd = useCallback(() => {
    setIsDragging(false);
    onDragEnd?.();
  }, [onDragEnd]);

  const handleMouseEnter = useCallback(() => {
    setIsHovered(true);
    onPreview?.(asset.id);
  }, [onPreview, asset.id]);

  const handleMouseLeave = useCallback(() => {
    setIsHovered(false);
    onPreview?.(null);
  }, [onPreview]);

  return (
    <Tooltip content={asset.description} relationship="description" positioning="below">
      <motion.div
        className={mergeClasses(
          styles.card,
          isSelected && styles.cardSelected,
          isDragging && styles.cardDragging
        )}
        onClick={handleClick}
        onDoubleClick={handleDoubleClick}
        onMouseEnter={handleMouseEnter}
        onMouseLeave={handleMouseLeave}
        draggable
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => {
          if (e.key === 'Enter') handleClick();
          if (e.key === ' ') handleAddClick(e as unknown as MouseEvent);
        }}
        aria-label={asset.name}
        aria-pressed={isSelected}
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{ duration: 0.2 }}
      >
        {asset.thumbnailUrl ? (
          <img src={asset.thumbnailUrl} alt={asset.name} className={styles.thumbnail} />
        ) : (
          <div className={styles.placeholder}>
            <Drag24Regular className={styles.placeholderIcon} />
            <span className={styles.placeholderText}>{asset.name}</span>
          </div>
        )}

        {/* Category Badge */}
        <Badge
          className={styles.categoryBadge}
          size="small"
          appearance="filled"
          style={{ backgroundColor: categoryColors[asset.category] }}
        >
          {categoryLabels[asset.category]}
        </Badge>

        {/* Duration */}
        <span className={styles.duration}>{formatDuration(asset.duration)}</span>

        {/* Overlay with name */}
        <div className={mergeClasses(styles.overlay, isHovered && styles.overlayVisible)}>
          <Text className={styles.name}>{asset.name}</Text>
        </div>

        {/* Action buttons */}
        <div className={mergeClasses(styles.actions, isHovered && styles.actionsVisible)}>
          <Tooltip content="Add to timeline" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              className={styles.actionButton}
              icon={<Add24Regular />}
              onClick={handleAddClick}
            />
          </Tooltip>
        </div>

        {/* Premium badge */}
        {asset.isPremium && (
          <Badge className={styles.premiumBadge} size="small" appearance="tint" color="warning">
            Premium
          </Badge>
        )}
      </motion.div>
    </Tooltip>
  );
};

export default GraphicCard;
