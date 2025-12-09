/**
 * DraggableList Component
 *
 * Reorderable list component with drag handles and smooth animations.
 * Supports keyboard navigation and accessibility.
 */

import { makeStyles, tokens, mergeClasses } from '@fluentui/react-components';
import { ReOrderDotsVertical24Regular } from '@fluentui/react-icons';
import { motion, Reorder } from 'framer-motion';
import { useCallback, useState } from 'react';
import type { ReactNode } from 'react';
import { useReducedMotion } from '../../../hooks/useReducedMotion';
import { openCutTokens } from '../../../styles/designTokens';

export interface DraggableListItem {
  /** Unique identifier for the item */
  id: string;
}

export interface DraggableListProps<T extends DraggableListItem> {
  /** List items to render */
  items: T[];
  /** Callback when items are reordered */
  onReorder: (items: T[]) => void;
  /** Render function for each item */
  renderItem: (item: T, index: number, isDragging: boolean) => ReactNode;
  /** Whether to show drag handles */
  showHandles?: boolean;
  /** Additional class name for the container */
  className?: string;
  /** Additional class name for each item */
  itemClassName?: string;
  /** Whether reordering is disabled */
  disabled?: boolean;
  /** Gap between items */
  gap?: string;
  /** Axis of reordering */
  axis?: 'x' | 'y';
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    listStyle: 'none',
    margin: 0,
    padding: 0,
  },
  containerHorizontal: {
    flexDirection: 'row',
  },
  item: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: openCutTokens.radius.md,
    transition: `box-shadow ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    ':hover': {
      boxShadow: openCutTokens.shadows.sm,
    },
  },
  itemDragging: {
    boxShadow: openCutTokens.shadows.lg,
    cursor: 'grabbing',
    zIndex: openCutTokens.zIndex.dropdown,
  },
  dragHandle: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: openCutTokens.spacing.xs,
    color: tokens.colorNeutralForeground4,
    cursor: 'grab',
    touchAction: 'none',
    transition: `color ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    ':hover': {
      color: tokens.colorNeutralForeground2,
    },
    ':active': {
      cursor: 'grabbing',
    },
  },
  dragHandleDisabled: {
    cursor: 'default',
    opacity: 0.5,
    ':hover': {
      color: tokens.colorNeutralForeground4,
    },
  },
  itemContent: {
    flex: 1,
    minWidth: 0,
  },
});

/**
 * DraggableListItemWrapper handles individual item rendering with drag handle.
 */
function DraggableListItemWrapper<T extends DraggableListItem>({
  item,
  index,
  renderItem,
  showHandles,
  disabled,
  className,
  prefersReducedMotion,
}: {
  item: T;
  index: number;
  renderItem: (item: T, index: number, isDragging: boolean) => ReactNode;
  showHandles: boolean;
  disabled: boolean;
  className?: string;
  prefersReducedMotion: boolean;
}) {
  const styles = useStyles();
  const [isDragging, setIsDragging] = useState(false);

  return (
    <Reorder.Item
      key={item.id}
      value={item}
      className={mergeClasses(styles.item, isDragging && styles.itemDragging, className)}
      onDragStart={() => setIsDragging(true)}
      onDragEnd={() => setIsDragging(false)}
      dragListener={!showHandles}
      style={{ cursor: disabled ? 'default' : showHandles ? 'default' : 'grab' }}
      transition={prefersReducedMotion ? { duration: 0 } : undefined}
    >
      {showHandles && (
        <motion.div
          className={mergeClasses(styles.dragHandle, disabled && styles.dragHandleDisabled)}
          whileHover={disabled || prefersReducedMotion ? undefined : { scale: 1.1 }}
          whileTap={disabled || prefersReducedMotion ? undefined : { scale: 0.95 }}
        >
          <ReOrderDotsVertical24Regular />
        </motion.div>
      )}
      <div className={styles.itemContent}>{renderItem(item, index, isDragging)}</div>
    </Reorder.Item>
  );
}

/**
 * DraggableList provides a reorderable list with drag-and-drop support.
 * Uses framer-motion's Reorder component for smooth animations.
 */
export function DraggableList<T extends DraggableListItem>({
  items,
  onReorder,
  renderItem,
  showHandles = true,
  className,
  itemClassName,
  disabled = false,
  gap = openCutTokens.spacing.sm,
  axis = 'y',
}: DraggableListProps<T>) {
  const styles = useStyles();
  const prefersReducedMotion = useReducedMotion();

  const handleReorder = useCallback(
    (newItems: T[]) => {
      if (!disabled) {
        onReorder(newItems);
      }
    },
    [disabled, onReorder]
  );

  return (
    <Reorder.Group
      axis={axis}
      values={items}
      onReorder={handleReorder}
      className={mergeClasses(
        styles.container,
        axis === 'x' && styles.containerHorizontal,
        className
      )}
      style={{ gap }}
    >
      {items.map((item, index) => (
        <DraggableListItemWrapper
          key={item.id}
          item={item}
          index={index}
          renderItem={renderItem}
          showHandles={showHandles}
          disabled={disabled}
          className={itemClassName}
          prefersReducedMotion={prefersReducedMotion}
        />
      ))}
    </Reorder.Group>
  );
}

export default DraggableList;
