/**
 * TransitionThumbnail Component
 *
 * Individual transition preview with hover animation, drag support,
 * and click to select functionality.
 */

import { makeStyles, tokens, Text, Tooltip, mergeClasses } from '@fluentui/react-components';
import { motion } from 'framer-motion';
import { useState, useCallback } from 'react';
import type { FC, DragEvent } from 'react';
import type { ExtendedTransitionDefinition } from '../../../stores/opencutTransitions';

export interface TransitionThumbnailProps {
  transition: ExtendedTransitionDefinition;
  onClick?: () => void;
  isSelected?: boolean;
}

const useStyles = makeStyles({
  container: {
    position: 'relative',
    aspectRatio: '1 / 1',
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusMedium,
    overflow: 'hidden',
    cursor: 'grab',
    border: `2px solid transparent`,
    transition: 'all 150ms ease-out',
    ':hover': {
      border: `2px solid ${tokens.colorBrandStroke1}`,
      transform: 'scale(1.02)',
      boxShadow: tokens.shadow4,
    },
    ':active': {
      cursor: 'grabbing',
    },
  },
  containerSelected: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    boxShadow: tokens.shadow8,
  },
  containerDragging: {
    opacity: 0.5,
    transform: 'scale(0.95)',
  },
  preview: {
    position: 'absolute',
    inset: 0,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingHorizontalXS,
  },
  previewGradient: {
    position: 'absolute',
    inset: 0,
  },
  label: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalXS}`,
    background: 'linear-gradient(transparent, rgba(0,0,0,0.7))',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  labelText: {
    fontSize: tokens.fontSizeBase100,
    color: 'white',
    textAlign: 'center',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    textShadow: '0 1px 2px rgba(0,0,0,0.5)',
  },
  durationBadge: {
    position: 'absolute',
    top: tokens.spacingVerticalXS,
    right: tokens.spacingHorizontalXS,
    backgroundColor: 'rgba(0, 0, 0, 0.75)',
    color: 'white',
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalXS}`,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: '10px',
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
  },
});

// Generate a visual preview based on transition type
function getPreviewGradient(category: string, id: string): string {
  switch (category) {
    case 'dissolve':
      if (id.includes('black')) {
        return 'linear-gradient(135deg, #3B82F6 0%, #000000 50%, #22C55E 100%)';
      } else if (id.includes('white')) {
        return 'linear-gradient(135deg, #3B82F6 0%, #FFFFFF 50%, #22C55E 100%)';
      }
      return 'linear-gradient(135deg, #3B82F6 0%, rgba(59, 130, 246, 0.5) 50%, #22C55E 100%)';
    case 'wipe':
      if (id.includes('left')) {
        return 'linear-gradient(90deg, #3B82F6 50%, #22C55E 50%)';
      } else if (id.includes('right')) {
        return 'linear-gradient(270deg, #3B82F6 50%, #22C55E 50%)';
      } else if (id.includes('up')) {
        return 'linear-gradient(180deg, #3B82F6 50%, #22C55E 50%)';
      }
      return 'linear-gradient(0deg, #3B82F6 50%, #22C55E 50%)';
    case 'slide':
      return id.includes('left')
        ? 'linear-gradient(90deg, #22C55E 30%, #3B82F6 70%)'
        : 'linear-gradient(270deg, #22C55E 30%, #3B82F6 70%)';
    case 'zoom':
      return id.includes('in')
        ? 'radial-gradient(circle, #22C55E 0%, #3B82F6 100%)'
        : 'radial-gradient(circle, #3B82F6 0%, #22C55E 100%)';
    case 'blur':
      return 'linear-gradient(135deg, #3B82F6 0%, rgba(59, 130, 246, 0.3) 50%, #22C55E 100%)';
    default:
      return 'linear-gradient(135deg, #3B82F6 0%, #22C55E 100%)';
  }
}

export const TransitionThumbnail: FC<TransitionThumbnailProps> = ({
  transition,
  onClick,
  isSelected = false,
}) => {
  const styles = useStyles();
  const [isDragging, setIsDragging] = useState(false);
  const [isHovered, setIsHovered] = useState(false);

  const handleNativeDragStart = useCallback(
    (e: DragEvent) => {
      setIsDragging(true);
      e.dataTransfer.setData('application/x-opencut-transition', transition.id);
      e.dataTransfer.effectAllowed = 'copy';
    },
    [transition.id]
  );

  const handleNativeDragEnd = useCallback(() => {
    setIsDragging(false);
  }, []);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        onClick?.();
      }
    },
    [onClick]
  );

  const previewGradient = getPreviewGradient(transition.category, transition.id);

  return (
    <Tooltip content={transition.description} relationship="description" positioning="below">
      <div
        className={mergeClasses(
          styles.container,
          isSelected && styles.containerSelected,
          isDragging && styles.containerDragging
        )}
        onClick={onClick}
        onKeyDown={handleKeyDown}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
        draggable
        onDragStart={handleNativeDragStart}
        onDragEnd={handleNativeDragEnd}
        role="button"
        tabIndex={0}
        aria-label={transition.name}
        aria-pressed={isSelected}
      >
        <div className={styles.preview}>
          <motion.div
            className={styles.previewGradient}
            style={{ background: previewGradient }}
            animate={{
              opacity: isHovered ? [0.8, 1, 0.8] : 1,
            }}
            transition={{
              duration: 1,
              repeat: isHovered ? Infinity : 0,
              ease: 'easeInOut',
            }}
          />
        </div>

        <span className={styles.durationBadge}>{transition.defaultDuration}s</span>

        <div className={styles.label}>
          <Text className={styles.labelText}>{transition.name}</Text>
        </div>
      </div>
    </Tooltip>
  );
};

export default TransitionThumbnail;
