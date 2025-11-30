/**
 * Resizable Panel Component
 *
 * A panel that can be resized by dragging its edge. Supports both left and right
 * positioned panels with appropriate drag handle placement.
 */

import { makeStyles, tokens } from '@fluentui/react-components';
import { useRef, useCallback, useEffect, type ReactNode, type CSSProperties } from 'react';

export interface ResizablePanelProps {
  /** Unique identifier for the panel */
  id: string;
  /** Minimum width the panel can be resized to */
  minWidth: number;
  /** Maximum width the panel can be resized to */
  maxWidth: number;
  /** Initial/default width of the panel */
  defaultWidth: number;
  /** Position of the panel determines handle placement */
  position: 'left' | 'right';
  /** Callback fired during and after resize */
  onResize?: (width: number) => void;
  /** Panel content */
  children: ReactNode;
  /** Additional CSS class */
  className?: string;
  /** Additional inline styles */
  style?: CSSProperties;
}

const useStyles = makeStyles({
  panel: {
    position: 'relative',
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    overflow: 'hidden',
    backgroundColor: tokens.colorNeutralBackground2,
    flexShrink: 0,
  },
  resizeHandle: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '8px',
    cursor: 'col-resize',
    zIndex: 10,
    touchAction: 'none', // Enable touch support
    '&::before': {
      content: '""',
      position: 'absolute',
      top: 0,
      bottom: 0,
      left: '3px',
      width: '2px',
      backgroundColor: 'transparent',
      transition: 'background-color 0.15s',
    },
    '&:hover::before': {
      backgroundColor: tokens.colorBrandBackground,
    },
    '&:active::before': {
      backgroundColor: tokens.colorBrandBackgroundPressed,
    },
    '&:focus-visible': {
      outline: 'none',
      '&::before': {
        backgroundColor: tokens.colorBrandBackground,
      },
    },
  },
  resizeHandleLeft: {
    left: 0,
    transform: 'translateX(-50%)',
  },
  resizeHandleRight: {
    right: 0,
    transform: 'translateX(50%)',
  },
  content: {
    flex: 1,
    overflow: 'auto',
    minHeight: 0,
  },
});

/**
 * Resizable Panel
 *
 * A container that allows users to drag its edge to resize. The drag handle
 * is placed on the appropriate edge based on the panel's position.
 *
 * @example
 * ```tsx
 * // Left sidebar that can be resized from its right edge
 * <ResizablePanel
 *   id="sidebar"
 *   minWidth={200}
 *   maxWidth={400}
 *   defaultWidth={280}
 *   position="left"
 *   onResize={(width) => console.log('New width:', width)}
 * >
 *   <SidebarContent />
 * </ResizablePanel>
 * ```
 */
export function ResizablePanel({
  id,
  minWidth,
  maxWidth,
  defaultWidth,
  position,
  onResize,
  children,
  className,
  style,
}: ResizablePanelProps): React.ReactElement {
  const styles = useStyles();
  const panelRef = useRef<HTMLDivElement>(null);
  const isResizing = useRef(false);
  const startX = useRef(0);
  const startWidth = useRef(0);

  /**
   * Start resizing on mouse down
   */
  const handleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      e.preventDefault();
      isResizing.current = true;
      startX.current = e.clientX;
      startWidth.current = panelRef.current?.offsetWidth ?? defaultWidth;

      document.body.style.cursor = 'col-resize';
      document.body.style.userSelect = 'none';
    },
    [defaultWidth]
  );

  /**
   * Start resizing on touch start (for tablet support)
   */
  const handleTouchStart = useCallback(
    (e: React.TouchEvent) => {
      const touch = e.touches[0];
      isResizing.current = true;
      startX.current = touch.clientX;
      startWidth.current = panelRef.current?.offsetWidth ?? defaultWidth;

      document.body.style.userSelect = 'none';
    },
    [defaultWidth]
  );

  /**
   * Handle keyboard resize (accessibility)
   */
  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (!panelRef.current) return;

      const step = e.shiftKey ? 50 : 10;
      let newWidth = panelRef.current.offsetWidth;

      if (e.key === 'ArrowLeft') {
        newWidth = position === 'left' ? newWidth - step : newWidth + step;
      } else if (e.key === 'ArrowRight') {
        newWidth = position === 'left' ? newWidth + step : newWidth - step;
      } else {
        return;
      }

      e.preventDefault();
      const clampedWidth = Math.min(maxWidth, Math.max(minWidth, newWidth));
      panelRef.current.style.width = `${clampedWidth}px`;
      onResize?.(clampedWidth);
    },
    [position, minWidth, maxWidth, onResize]
  );

  useEffect(() => {
    /**
     * Handle mouse movement during resize
     */
    const handleMouseMove = (e: MouseEvent) => {
      if (!isResizing.current || !panelRef.current) return;

      const delta = position === 'left' ? e.clientX - startX.current : startX.current - e.clientX;

      const newWidth = Math.min(maxWidth, Math.max(minWidth, startWidth.current + delta));
      panelRef.current.style.width = `${newWidth}px`;
      onResize?.(newWidth);
    };

    /**
     * Handle touch movement during resize
     */
    const handleTouchMove = (e: TouchEvent) => {
      if (!isResizing.current || !panelRef.current) return;

      const touch = e.touches[0];
      const delta =
        position === 'left' ? touch.clientX - startX.current : startX.current - touch.clientX;

      const newWidth = Math.min(maxWidth, Math.max(minWidth, startWidth.current + delta));
      panelRef.current.style.width = `${newWidth}px`;
      onResize?.(newWidth);
    };

    /**
     * Complete resize on mouse/touch release
     */
    const handleEnd = () => {
      isResizing.current = false;
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleEnd);
    document.addEventListener('touchmove', handleTouchMove, { passive: false });
    document.addEventListener('touchend', handleEnd);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleEnd);
      document.removeEventListener('touchmove', handleTouchMove);
      document.removeEventListener('touchend', handleEnd);
    };
  }, [position, minWidth, maxWidth, onResize]);

  const handleClassName = `${styles.resizeHandle} ${
    position === 'left' ? styles.resizeHandleRight : styles.resizeHandleLeft
  }`;

  return (
    <div
      ref={panelRef}
      className={`${styles.panel} ${className || ''}`}
      style={{ width: defaultWidth, ...style }}
      data-panel-id={id}
    >
      {/* 
        The separator role with tabIndex is valid for interactive resize handles per WAI-ARIA.
        This is a focusable separator that allows keyboard-based resizing.
      */}
      {/* eslint-disable jsx-a11y/no-noninteractive-element-interactions, jsx-a11y/no-noninteractive-tabindex */}
      <div
        className={handleClassName}
        onMouseDown={handleMouseDown}
        onTouchStart={handleTouchStart}
        onKeyDown={handleKeyDown}
        role="separator"
        aria-orientation="vertical"
        aria-label={`Resize ${id} panel`}
        aria-valuenow={panelRef.current?.offsetWidth ?? defaultWidth}
        aria-valuemin={minWidth}
        aria-valuemax={maxWidth}
        tabIndex={0}
      />
      {/* eslint-enable jsx-a11y/no-noninteractive-element-interactions, jsx-a11y/no-noninteractive-tabindex */}
      <div className={styles.content}>{children}</div>
    </div>
  );
}
