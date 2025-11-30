/**
 * Collapsible Panel Component
 *
 * A panel that can collapse to a smaller width with smooth animation.
 * Supports both controlled and uncontrolled modes.
 */

import { makeStyles, tokens, Button, Tooltip } from '@fluentui/react-components';
import {
  ChevronLeft20Regular,
  ChevronRight20Regular,
  ChevronDown20Regular,
  ChevronUp20Regular,
} from '@fluentui/react-icons';
import {
  useState,
  useCallback,
  useEffect,
  useRef,
  type ReactNode,
  type CSSProperties,
  type KeyboardEvent,
} from 'react';

export interface CollapsiblePanelProps {
  /** Unique identifier for the panel */
  id: string;
  /** Panel content */
  children: ReactNode;
  /** Width when expanded */
  expandedWidth: number;
  /** Width when collapsed */
  collapsedWidth: number;
  /** Panel position affects collapse direction */
  position: 'left' | 'right' | 'bottom';
  /** Content to show when collapsed (e.g., icon toolbar) */
  collapsedContent?: ReactNode;
  /** Label for the panel (used in aria-label) */
  label?: string;
  /** Controlled collapsed state */
  collapsed?: boolean;
  /** Callback when collapsed state changes */
  onCollapsedChange?: (collapsed: boolean) => void;
  /** Additional CSS class */
  className?: string;
  /** Additional inline styles */
  style?: CSSProperties;
  /** Show collapse button */
  showCollapseButton?: boolean;
}

const useStyles = makeStyles({
  panel: {
    position: 'relative',
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
    backgroundColor: tokens.colorNeutralBackground2,
    transition: 'width 200ms ease-out, height 200ms ease-out',
    flexShrink: 0,
  },
  panelHorizontal: {
    height: '100%',
  },
  panelVertical: {
    width: '100%',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: '8px 12px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3,
    minHeight: '40px',
  },
  headerCollapsed: {
    flexDirection: 'column',
    padding: '8px',
    justifyContent: 'flex-start',
    gap: '8px',
  },
  collapseButton: {
    minWidth: '24px',
    minHeight: '24px',
    padding: '4px',
    backgroundColor: 'transparent',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    transition: 'background-color 150ms',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3Hover,
    },
    ':focus-visible': {
      outline: `2px solid ${tokens.colorBrandStroke1}`,
      outlineOffset: '2px',
    },
  },
  content: {
    flex: 1,
    overflow: 'auto',
    minHeight: 0,
    minWidth: 0,
  },
  contentHidden: {
    display: 'none',
  },
  collapsedContent: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    padding: '8px',
    gap: '8px',
  },
});

/**
 * Collapsible Panel
 *
 * A panel that can smoothly transition between expanded and collapsed states.
 * When collapsed, it can show alternative content like an icon toolbar.
 *
 * @example
 * ```tsx
 * // Left sidebar with collapse to icons
 * <CollapsiblePanel
 *   id="sidebar"
 *   expandedWidth={280}
 *   collapsedWidth={48}
 *   position="left"
 *   collapsedContent={<IconToolbar />}
 *   label="Sidebar"
 * >
 *   <FullSidebarContent />
 * </CollapsiblePanel>
 * ```
 */
export function CollapsiblePanel({
  id,
  children,
  expandedWidth,
  collapsedWidth,
  position,
  collapsedContent,
  label,
  collapsed: controlledCollapsed,
  onCollapsedChange,
  className,
  style,
  showCollapseButton = true,
}: CollapsiblePanelProps): React.ReactElement {
  const styles = useStyles();
  const panelRef = useRef<HTMLDivElement>(null);

  // Internal state for uncontrolled mode
  const [internalCollapsed, setInternalCollapsed] = useState(false);

  // Determine actual collapsed state
  const isCollapsed = controlledCollapsed !== undefined ? controlledCollapsed : internalCollapsed;

  const handleToggle = useCallback(() => {
    const newValue = !isCollapsed;
    if (onCollapsedChange) {
      onCollapsedChange(newValue);
    } else {
      setInternalCollapsed(newValue);
    }
  }, [isCollapsed, onCollapsedChange]);

  const handleKeyDown = useCallback(
    (e: KeyboardEvent) => {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        handleToggle();
      }
    },
    [handleToggle]
  );

  // Sync with controlled state
  useEffect(() => {
    if (controlledCollapsed !== undefined) {
      setInternalCollapsed(controlledCollapsed);
    }
  }, [controlledCollapsed]);

  const isHorizontal = position === 'left' || position === 'right';

  // Determine collapse icon based on position and state
  const getCollapseIcon = () => {
    if (position === 'left') {
      return isCollapsed ? <ChevronRight20Regular /> : <ChevronLeft20Regular />;
    }
    if (position === 'right') {
      return isCollapsed ? <ChevronLeft20Regular /> : <ChevronRight20Regular />;
    }
    // bottom
    return isCollapsed ? <ChevronUp20Regular /> : <ChevronDown20Regular />;
  };

  const panelStyle: CSSProperties = {
    ...(isHorizontal
      ? { width: isCollapsed ? collapsedWidth : expandedWidth }
      : { height: isCollapsed ? collapsedWidth : expandedWidth }),
    ...style,
  };

  const panelClassName = `${styles.panel} ${
    isHorizontal ? styles.panelHorizontal : styles.panelVertical
  } ${className || ''}`;

  const ariaLabel = label || `${id} panel`;
  const buttonLabel = isCollapsed ? `Expand ${ariaLabel}` : `Collapse ${ariaLabel}`;

  return (
    <div
      ref={panelRef}
      className={panelClassName}
      style={panelStyle}
      data-panel-id={id}
      data-collapsed={isCollapsed}
      aria-label={ariaLabel}
    >
      {showCollapseButton && (
        <div className={`${styles.header} ${isCollapsed ? styles.headerCollapsed : ''}`}>
          <Tooltip content={buttonLabel} relationship="label">
            <Button
              appearance="subtle"
              className={styles.collapseButton}
              icon={getCollapseIcon()}
              onClick={handleToggle}
              onKeyDown={handleKeyDown}
              aria-expanded={!isCollapsed}
              aria-label={buttonLabel}
            />
          </Tooltip>
        </div>
      )}

      {/* Collapsed content (icons/minimal UI) */}
      {isCollapsed && collapsedContent && (
        <div className={styles.collapsedContent}>{collapsedContent}</div>
      )}

      {/* Full content */}
      <div className={`${styles.content} ${isCollapsed ? styles.contentHidden : ''}`}>
        {children}
      </div>
    </div>
  );
}
