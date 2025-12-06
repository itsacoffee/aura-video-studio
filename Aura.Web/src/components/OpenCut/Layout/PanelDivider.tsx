/**
 * PanelDivider Component
 *
 * A draggable divider between panels that allows resizing.
 * Features:
 * - 6px wide resize handle with visual hover indicator
 * - Double-click to toggle collapse/expand
 * - Accent color highlight on hover/drag
 */

import { makeStyles, tokens, mergeClasses } from '@fluentui/react-components';
import { useRef, useState, useCallback, useEffect } from 'react';
import type { FC, MouseEvent as ReactMouseEvent } from 'react';
import { LAYOUT_CONSTANTS } from '../../../stores/opencutLayout';
import { openCutTokens } from '../../../styles/designTokens';

export type DividerDirection = 'left' | 'right';

export interface PanelDividerProps {
  /** Which side of the divider is being resized */
  direction: DividerDirection;
  /** Whether the adjacent panel is collapsed */
  isCollapsed: boolean;
  /** Callback when resizing occurs */
  onResize: (deltaX: number) => void;
  /** Callback when double-clicked to toggle collapse */
  onDoubleClick: () => void;
  className?: string;
}

const useStyles = makeStyles({
  divider: {
    position: 'relative',
    width: '6px',
    height: '100%',
    cursor: 'col-resize',
    backgroundColor: 'transparent',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    flexShrink: 0,
    zIndex: openCutTokens.zIndex.dropdown,
    transition: `background-color ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    border: 'none',
    padding: 0,
    margin: 0,
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
    ':hover::after': {
      backgroundColor: tokens.colorBrandStroke1,
      opacity: 1,
    },
    '::after': {
      content: '""',
      position: 'absolute',
      width: '2px',
      height: '48px',
      backgroundColor: tokens.colorNeutralStroke3,
      borderRadius: openCutTokens.radius.sm,
      opacity: 0,
      transition: `opacity ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}, background-color ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    },
  },
  dividerActive: {
    backgroundColor: tokens.colorNeutralBackground1Pressed,
    '::after': {
      backgroundColor: tokens.colorBrandStroke1,
      opacity: 1,
    },
  },
  dividerCollapsed: {
    cursor: 'pointer',
    ':hover::after': {
      width: '4px',
    },
  },
});

export const PanelDivider: FC<PanelDividerProps> = ({
  direction,
  isCollapsed,
  onResize,
  onDoubleClick,
  className,
}) => {
  const styles = useStyles();
  const [isDragging, setIsDragging] = useState(false);
  const startXRef = useRef(0);
  const lastClickRef = useRef(0);

  const handleMouseDown = useCallback(
    (e: ReactMouseEvent) => {
      e.preventDefault();
      const now = Date.now();

      // Check for double-click (300ms threshold)
      if (now - lastClickRef.current < 300) {
        onDoubleClick();
        return;
      }
      lastClickRef.current = now;

      if (!isCollapsed) {
        setIsDragging(true);
        startXRef.current = e.clientX;
      }
    },
    [isCollapsed, onDoubleClick]
  );

  useEffect(() => {
    if (!isDragging) return;

    const handleMouseMove = (e: MouseEvent) => {
      const deltaX = e.clientX - startXRef.current;
      startXRef.current = e.clientX;
      onResize(direction === 'left' ? deltaX : -deltaX);
    };

    const handleMouseUp = () => {
      setIsDragging(false);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    };
  }, [isDragging, direction, onResize]);

  return (
    <button
      type="button"
      className={mergeClasses(
        styles.divider,
        isDragging && styles.dividerActive,
        isCollapsed && styles.dividerCollapsed,
        className
      )}
      onMouseDown={handleMouseDown}
      aria-label={`Resize ${direction} panel`}
    />
  );
};

export default PanelDivider;
