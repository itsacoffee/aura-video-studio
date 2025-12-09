/**
 * ResizablePanel Component
 *
 * A panel component that can be resized by dragging its edge.
 * Follows Apple Human Interface Guidelines with smooth animations,
 * proper cursor feedback, and accessible resize handles.
 */

import { makeStyles, tokens, mergeClasses } from '@fluentui/react-components';
import { useRef, useState, useCallback, useEffect } from 'react';
import type { FC, ReactNode, CSSProperties } from 'react';

export type ResizeDirection = 'left' | 'right' | 'top' | 'bottom';

export interface ResizablePanelProps {
  children: ReactNode;
  direction: ResizeDirection;
  defaultSize: number;
  minSize: number;
  maxSize: number;
  className?: string;
  style?: CSSProperties;
  onResize?: (size: number) => void;
  collapsed?: boolean;
  onCollapse?: () => void;
}

const useStyles = makeStyles({
  container: {
    position: 'relative',
    display: 'flex',
    flexDirection: 'column',
    transition: 'width 150ms ease-out, height 150ms ease-out',
  },
  containerResizing: {
    transition: 'none',
    userSelect: 'none',
  },
  content: {
    flex: 1,
    overflow: 'hidden',
    display: 'flex',
    flexDirection: 'column',
  },
  handleHorizontal: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '8px',
    cursor: 'ew-resize',
    zIndex: 10,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    transition: 'background-color 150ms ease-out',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
    ':hover::after': {
      backgroundColor: tokens.colorBrandStroke1,
      opacity: 1,
    },
    '::after': {
      content: '""',
      width: '2px',
      height: '48px',
      backgroundColor: tokens.colorNeutralStroke3,
      borderRadius: tokens.borderRadiusMedium,
      opacity: 0,
      transition: 'opacity 150ms ease-out, background-color 150ms ease-out',
    },
  },
  handleHorizontalActive: {
    backgroundColor: tokens.colorNeutralBackground1Pressed,
    '::after': {
      backgroundColor: tokens.colorBrandStroke1,
      opacity: 1,
    },
  },
  handleLeft: {
    left: '-4px',
  },
  handleRight: {
    right: '-4px',
  },
  handleVertical: {
    position: 'absolute',
    left: 0,
    right: 0,
    height: '8px',
    cursor: 'ns-resize',
    zIndex: 10,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    transition: 'background-color 150ms ease-out',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
    ':hover::after': {
      backgroundColor: tokens.colorBrandStroke1,
      opacity: 1,
    },
    '::after': {
      content: '""',
      height: '2px',
      width: '48px',
      backgroundColor: tokens.colorNeutralStroke3,
      borderRadius: tokens.borderRadiusMedium,
      opacity: 0,
      transition: 'opacity 150ms ease-out, background-color 150ms ease-out',
    },
  },
  handleVerticalActive: {
    backgroundColor: tokens.colorNeutralBackground1Pressed,
    '::after': {
      backgroundColor: tokens.colorBrandStroke1,
      opacity: 1,
    },
  },
  handleTop: {
    top: '-4px',
  },
  handleBottom: {
    bottom: '-4px',
  },
});

export const ResizablePanel: FC<ResizablePanelProps> = ({
  children,
  direction,
  defaultSize,
  minSize,
  maxSize,
  className,
  style,
  onResize,
  collapsed = false,
}) => {
  const styles = useStyles();
  const [size, setSize] = useState(defaultSize);
  const [isResizing, setIsResizing] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const startPosRef = useRef(0);
  const startSizeRef = useRef(0);

  const isHorizontal = direction === 'left' || direction === 'right';

  const handleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      e.preventDefault();
      setIsResizing(true);
      startPosRef.current = isHorizontal ? e.clientX : e.clientY;
      startSizeRef.current = size;
    },
    [isHorizontal, size]
  );

  useEffect(() => {
    if (!isResizing) return;

    const handleMouseMove = (e: MouseEvent) => {
      const currentPos = isHorizontal ? e.clientX : e.clientY;
      const delta = currentPos - startPosRef.current;

      let newSize: number;
      if (direction === 'right' || direction === 'bottom') {
        newSize = startSizeRef.current + delta;
      } else {
        newSize = startSizeRef.current - delta;
      }

      newSize = Math.max(minSize, Math.min(maxSize, newSize));
      setSize(newSize);
      onResize?.(newSize);
    };

    const handleMouseUp = () => {
      setIsResizing(false);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isResizing, isHorizontal, direction, minSize, maxSize, onResize]);

  const containerStyle: CSSProperties = {
    ...(isHorizontal ? { width: collapsed ? 0 : size } : { height: collapsed ? 0 : size }),
    ...style,
  };

  const handleClass = mergeClasses(
    isHorizontal ? styles.handleHorizontal : styles.handleVertical,
    direction === 'left' && styles.handleLeft,
    direction === 'right' && styles.handleRight,
    direction === 'top' && styles.handleTop,
    direction === 'bottom' && styles.handleBottom,
    isResizing && (isHorizontal ? styles.handleHorizontalActive : styles.handleVerticalActive)
  );

  return (
    <div
      ref={containerRef}
      className={mergeClasses(styles.container, isResizing && styles.containerResizing, className)}
      style={containerStyle}
    >
      <div className={styles.content}>{children}</div>
      <button
        type="button"
        className={handleClass}
        onMouseDown={handleMouseDown}
        aria-label={`Resize panel ${direction}`}
        style={{
          background: 'transparent',
          border: 'none',
          padding: 0,
          margin: 0,
        }}
      />
    </div>
  );
};

export default ResizablePanel;
