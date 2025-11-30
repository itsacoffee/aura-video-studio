/**
 * Panel Divider Component
 *
 * A draggable divider between two panels that allows resizing.
 * Provides visual feedback and accessibility features.
 */

import { makeStyles, tokens } from '@fluentui/react-components';
import {
  useRef,
  useState,
  useCallback,
  useEffect,
  type CSSProperties,
  type KeyboardEvent,
} from 'react';

export interface PanelDividerProps {
  /** Unique identifier for this divider */
  id: string;
  /** Orientation of the divider */
  orientation: 'vertical' | 'horizontal';
  /** Callback when resize occurs, with delta in pixels */
  onResize?: (delta: number) => void;
  /** Callback when resize starts */
  onResizeStart?: () => void;
  /** Callback when resize ends */
  onResizeEnd?: () => void;
  /** Size of the resize target area in pixels */
  hitAreaSize?: number;
  /** Additional CSS class */
  className?: string;
  /** Additional inline styles */
  style?: CSSProperties;
  /** Accessible label for the divider */
  ariaLabel?: string;
}

const useStyles = makeStyles({
  divider: {
    position: 'relative',
    flexShrink: 0,
    zIndex: 5,
    touchAction: 'none',
    '&::before': {
      content: '""',
      position: 'absolute',
      backgroundColor: 'transparent',
      transition: 'background-color 150ms',
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
  vertical: {
    width: '1px',
    height: '100%',
    cursor: 'col-resize',
    backgroundColor: tokens.colorNeutralStroke1,
    '&::before': {
      top: 0,
      bottom: 0,
      left: '-4px',
      width: '9px',
    },
  },
  horizontal: {
    height: '1px',
    width: '100%',
    cursor: 'row-resize',
    backgroundColor: tokens.colorNeutralStroke1,
    '&::before': {
      left: 0,
      right: 0,
      top: '-4px',
      height: '9px',
    },
  },
  resizing: {
    '&::before': {
      backgroundColor: tokens.colorBrandBackgroundPressed,
    },
  },
});

/**
 * Panel Divider
 *
 * A draggable divider between panels for resizing. Supports mouse, touch,
 * and keyboard interaction for accessibility.
 *
 * @example
 * ```tsx
 * // Vertical divider between left sidebar and content
 * <PanelDivider
 *   id="sidebar-divider"
 *   orientation="vertical"
 *   onResize={(delta) => setSidebarWidth(w => w + delta)}
 *   ariaLabel="Resize sidebar"
 * />
 * ```
 */
export function PanelDivider({
  id,
  orientation,
  onResize,
  onResizeStart,
  onResizeEnd,
  hitAreaSize = 8,
  className,
  style,
  ariaLabel,
}: PanelDividerProps): React.ReactElement {
  const styles = useStyles();
  const dividerRef = useRef<HTMLDivElement>(null);
  const isResizingRef = useRef(false);
  const lastPosition = useRef(0);
  const [isResizing, setIsResizing] = useState(false);

  /**
   * Start resize on mouse down
   */
  const handleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      e.preventDefault();
      isResizingRef.current = true;
      setIsResizing(true);
      lastPosition.current = orientation === 'vertical' ? e.clientX : e.clientY;

      document.body.style.cursor = orientation === 'vertical' ? 'col-resize' : 'row-resize';
      document.body.style.userSelect = 'none';

      onResizeStart?.();
    },
    [orientation, onResizeStart]
  );

  /**
   * Start resize on touch start
   */
  const handleTouchStart = useCallback(
    (e: React.TouchEvent) => {
      const touch = e.touches[0];
      isResizingRef.current = true;
      setIsResizing(true);
      lastPosition.current = orientation === 'vertical' ? touch.clientX : touch.clientY;

      document.body.style.userSelect = 'none';
      onResizeStart?.();
    },
    [orientation, onResizeStart]
  );

  /**
   * Handle keyboard resize
   */
  const handleKeyDown = useCallback(
    (e: KeyboardEvent) => {
      const step = e.shiftKey ? 50 : 10;
      let delta = 0;

      if (orientation === 'vertical') {
        if (e.key === 'ArrowLeft') delta = -step;
        else if (e.key === 'ArrowRight') delta = step;
      } else {
        if (e.key === 'ArrowUp') delta = -step;
        else if (e.key === 'ArrowDown') delta = step;
      }

      if (delta !== 0) {
        e.preventDefault();
        onResize?.(delta);
      }
    },
    [orientation, onResize]
  );

  useEffect(() => {
    /**
     * Handle mouse movement during resize
     */
    const handleMouseMove = (e: MouseEvent) => {
      if (!isResizingRef.current) return;

      const currentPosition = orientation === 'vertical' ? e.clientX : e.clientY;
      const delta = currentPosition - lastPosition.current;
      lastPosition.current = currentPosition;

      onResize?.(delta);
    };

    /**
     * Handle touch movement during resize
     */
    const handleTouchMove = (e: TouchEvent) => {
      if (!isResizingRef.current) return;

      const touch = e.touches[0];
      const currentPosition = orientation === 'vertical' ? touch.clientX : touch.clientY;
      const delta = currentPosition - lastPosition.current;
      lastPosition.current = currentPosition;

      onResize?.(delta);
    };

    /**
     * Complete resize
     */
    const handleEnd = () => {
      if (!isResizingRef.current) return;

      isResizingRef.current = false;
      setIsResizing(false);
      document.body.style.cursor = '';
      document.body.style.userSelect = '';

      onResizeEnd?.();
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
  }, [orientation, onResize, onResizeEnd]);

  const dividerClassName = [
    styles.divider,
    orientation === 'vertical' ? styles.vertical : styles.horizontal,
    isResizing ? styles.resizing : '',
    className || '',
  ]
    .filter(Boolean)
    .join(' ');

  const dividerStyle: CSSProperties = {
    ...style,
    ...(orientation === 'vertical'
      ? { marginLeft: -Math.floor(hitAreaSize / 2), marginRight: -Math.ceil(hitAreaSize / 2) }
      : { marginTop: -Math.floor(hitAreaSize / 2), marginBottom: -Math.ceil(hitAreaSize / 2) }),
  };

  /* 
    The separator role with tabIndex is valid for interactive resize handles per WAI-ARIA.
    This is a focusable separator that allows keyboard-based resizing.
  */
  /* eslint-disable jsx-a11y/no-noninteractive-element-interactions, jsx-a11y/no-noninteractive-tabindex */
  return (
    <div
      ref={dividerRef}
      className={dividerClassName}
      style={dividerStyle}
      data-divider-id={id}
      data-resizing={isResizing}
      onMouseDown={handleMouseDown}
      onTouchStart={handleTouchStart}
      onKeyDown={handleKeyDown}
      role="separator"
      aria-orientation={orientation}
      aria-label={
        ariaLabel || `Resize ${orientation === 'vertical' ? 'horizontally' : 'vertically'}`
      }
      tabIndex={0}
    />
  );
  /* eslint-enable jsx-a11y/no-noninteractive-element-interactions, jsx-a11y/no-noninteractive-tabindex */
}
