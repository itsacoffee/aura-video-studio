import { makeStyles, Button, Tooltip, ToggleButton } from '@fluentui/react-components';
import {
  ArrowUndo20Regular,
  ArrowRedo20Regular,
  Cursor20Regular,
  Cut20Regular,
  Handshake20Regular,
  ZoomIn20Regular,
  ZoomOut20Regular,
  ZoomFit20Regular,
  ArrowSplit20Regular,
} from '@fluentui/react-icons';
import React from 'react';
import '../../styles/editor-design-tokens.css';
import { ToolbarGroup } from './ToolbarGroup';

const useStyles = makeStyles({
  toolbar: {
    display: 'flex',
    alignItems: 'center',
    height: '44px',
    backgroundColor: 'var(--color-bg-panel-header)',
    borderBottom: '1px solid var(--color-border-subtle)',
    padding: '0 var(--space-md)',
    gap: 'var(--space-sm)',
    flexShrink: 0,
  },
  leftSection: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--space-xs)',
  },
  centerSection: {
    display: 'flex',
    alignItems: 'center',
    flex: 1,
    justifyContent: 'center',
    gap: 'var(--space-xs)',
  },
  rightSection: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--space-xs)',
  },
  toolButton: {
    minWidth: 'var(--toolbar-button-size)',
    minHeight: 'var(--toolbar-button-size)',
    padding: 'var(--space-sm)',
    color: 'var(--color-text-secondary)',
    transition: 'all var(--transition-fast)',
    '&:hover': {
      backgroundColor: 'var(--color-bg-hover)',
      color: 'var(--color-text-primary)',
    },
  },
  toolButtonActive: {
    backgroundColor: 'var(--color-bg-selected)',
    color: 'var(--color-accent-primary)',
    '&:hover': {
      backgroundColor: 'var(--color-bg-active)',
    },
  },
  toggleButton: {
    minWidth: 'var(--toolbar-button-size)',
    minHeight: 'var(--toolbar-button-size)',
    fontSize: 'var(--font-size-xs)',
    fontWeight: 'var(--font-weight-medium)',
    color: 'var(--color-text-secondary)',
    border: '1px solid var(--color-border-subtle)',
    borderRadius: 'var(--radius-sm)',
    transition: 'all var(--transition-fast)',
    '&:hover': {
      backgroundColor: 'var(--color-bg-hover)',
      color: 'var(--color-text-primary)',
      borderColor: 'var(--color-border-strong)',
    },
  },
  toggleButtonChecked: {
    backgroundColor: 'var(--color-bg-selected)',
    color: 'var(--color-accent-primary)',
    borderColor: 'var(--color-accent-primary)',
    '&:hover': {
      backgroundColor: 'var(--color-bg-active)',
    },
  },
  timecode: {
    fontFamily: 'var(--font-family-mono)',
    fontSize: 'var(--font-size-sm)',
    color: 'var(--color-text-primary)',
    backgroundColor: 'var(--color-bg-surface-subtle)',
    padding: '4px 12px',
    borderRadius: 'var(--radius-md)',
    minWidth: '150px',
    textAlign: 'center',
    cursor: 'pointer',
    userSelect: 'none',
    transition: 'all var(--transition-fast)',
    '&:hover': {
      backgroundColor: 'var(--color-bg-hover)',
    },
  },
  zoomSlider: {
    width: '100px',
    margin: '0 var(--space-sm)',
  },
});

export type EditTool = 'select' | 'trim' | 'ripple' | 'razor' | 'slip' | 'hand';

export interface TimelineToolbarProps {
  /** Currently active editing tool */
  activeTool: EditTool;
  /** Callback when tool is changed */
  onToolChange: (tool: EditTool) => void;
  /** Whether ripple mode is enabled */
  rippleMode: boolean;
  /** Callback when ripple mode is toggled */
  onRippleModeToggle: () => void;
  /** Whether snapping is enabled */
  snapping: boolean;
  /** Callback when snapping is toggled */
  onSnappingToggle: () => void;
  /** Whether magnetic mode is enabled */
  magnetic: boolean;
  /** Callback when magnetic mode is toggled */
  onMagneticToggle: () => void;
  /** Current timecode string (e.g., "00:00:10 / 00:01:30") */
  timecode: string;
  /** Callback when timecode is clicked */
  onTimecodeClick?: () => void;
  /** Current zoom level (0-100) */
  zoomLevel: number;
  /** Callback when zoom changes */
  onZoomChange: (level: number) => void;
  /** Callback for zoom in */
  onZoomIn: () => void;
  /** Callback for zoom out */
  onZoomOut: () => void;
  /** Callback for fit to view */
  onZoomFit: () => void;
  /** Callback for undo */
  onUndo?: () => void;
  /** Callback for redo */
  onRedo?: () => void;
  /** Whether undo is available */
  canUndo?: boolean;
  /** Whether redo is available */
  canRedo?: boolean;
}

/**
 * TimelineToolbar Component
 *
 * Provides a workflow-oriented toolbar for timeline editing.
 * Groups tools logically: Edit Tools → Modes → View → Zoom
 */
export function TimelineToolbar({
  activeTool,
  onToolChange,
  rippleMode,
  onRippleModeToggle,
  snapping,
  onSnappingToggle,
  magnetic,
  onMagneticToggle,
  timecode,
  onTimecodeClick,
  zoomLevel,
  onZoomChange,
  onZoomIn,
  onZoomOut,
  onZoomFit,
  onUndo,
  onRedo,
  canUndo = true,
  canRedo = true,
}: TimelineToolbarProps) {
  const styles = useStyles();

  const tools: Array<{ id: EditTool; icon: React.ReactNode; label: string; shortcut?: string }> = [
    { id: 'select', icon: <Cursor20Regular />, label: 'Select', shortcut: 'V' },
    { id: 'trim', icon: <Cut20Regular />, label: 'Trim', shortcut: 'C' },
    { id: 'ripple', icon: <ArrowSplit20Regular />, label: 'Ripple Edit', shortcut: 'B' },
    { id: 'razor', icon: <Cut20Regular />, label: 'Razor', shortcut: 'R' },
    { id: 'hand', icon: <Handshake20Regular />, label: 'Hand', shortcut: 'H' },
  ];

  return (
    <div className={styles.toolbar} role="toolbar" aria-label="Timeline toolbar">
      <div className={styles.leftSection}>
        {/* Undo/Redo Group */}
        {(onUndo || onRedo) && (
          <ToolbarGroup showSeparator aria-label="History">
            {onUndo && (
              <Tooltip content={`Undo (Ctrl+Z)`} relationship="label">
                <Button
                  appearance="subtle"
                  icon={<ArrowUndo20Regular />}
                  onClick={onUndo}
                  disabled={!canUndo}
                  className={styles.toolButton}
                  aria-label="Undo"
                />
              </Tooltip>
            )}
            {onRedo && (
              <Tooltip content={`Redo (Ctrl+Y)`} relationship="label">
                <Button
                  appearance="subtle"
                  icon={<ArrowRedo20Regular />}
                  onClick={onRedo}
                  disabled={!canRedo}
                  className={styles.toolButton}
                  aria-label="Redo"
                />
              </Tooltip>
            )}
          </ToolbarGroup>
        )}

        {/* Edit Tools Group */}
        <ToolbarGroup showSeparator aria-label="Edit tools">
          {tools.map((tool) => (
            <Tooltip
              key={tool.id}
              content={tool.shortcut ? `${tool.label} (${tool.shortcut})` : tool.label}
              relationship="label"
            >
              <Button
                appearance="subtle"
                icon={tool.icon}
                onClick={() => onToolChange(tool.id)}
                className={
                  activeTool === tool.id
                    ? `${styles.toolButton} ${styles.toolButtonActive}`
                    : styles.toolButton
                }
                aria-label={tool.label}
                aria-pressed={activeTool === tool.id}
              />
            </Tooltip>
          ))}
        </ToolbarGroup>

        {/* Edit Modes Group */}
        <ToolbarGroup showSeparator aria-label="Edit modes">
          <Tooltip content="Ripple mode: Edit and shift subsequent clips" relationship="label">
            <ToggleButton
              checked={rippleMode}
              onClick={onRippleModeToggle}
              className={
                rippleMode
                  ? `${styles.toggleButton} ${styles.toggleButtonChecked}`
                  : styles.toggleButton
              }
              size="small"
            >
              Ripple
            </ToggleButton>
          </Tooltip>
          <Tooltip content="Snap clips to playhead and other clips" relationship="label">
            <ToggleButton
              checked={snapping}
              onClick={onSnappingToggle}
              className={
                snapping
                  ? `${styles.toggleButton} ${styles.toggleButtonChecked}`
                  : styles.toggleButton
              }
              size="small"
            >
              Snap
            </ToggleButton>
          </Tooltip>
          <Tooltip content="Magnetic mode: Clips attract to each other" relationship="label">
            <ToggleButton
              checked={magnetic}
              onClick={onMagneticToggle}
              className={
                magnetic
                  ? `${styles.toggleButton} ${styles.toggleButtonChecked}`
                  : styles.toggleButton
              }
              size="small"
            >
              Magnetic
            </ToggleButton>
          </Tooltip>
        </ToolbarGroup>
      </div>

      {/* Center: Timecode Display */}
      <div className={styles.centerSection}>
        <Tooltip content="Click to jump to timecode" relationship="label">
          <div
            className={styles.timecode}
            onClick={onTimecodeClick}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                onTimecodeClick?.();
              }
            }}
            role="button"
            tabIndex={0}
          >
            {timecode}
          </div>
        </Tooltip>
      </div>

      {/* Right: Zoom Controls */}
      <div className={styles.rightSection}>
        <ToolbarGroup aria-label="Zoom controls">
          <Tooltip content="Zoom out" relationship="label">
            <Button
              appearance="subtle"
              icon={<ZoomOut20Regular />}
              onClick={onZoomOut}
              className={styles.toolButton}
              aria-label="Zoom out"
            />
          </Tooltip>
          <input
            type="range"
            min="0"
            max="100"
            value={zoomLevel}
            onChange={(e) => onZoomChange(Number(e.target.value))}
            className={styles.zoomSlider}
            aria-label="Zoom level"
          />
          <Tooltip content="Zoom in" relationship="label">
            <Button
              appearance="subtle"
              icon={<ZoomIn20Regular />}
              onClick={onZoomIn}
              className={styles.toolButton}
              aria-label="Zoom in"
            />
          </Tooltip>
          <Tooltip content="Fit timeline to view" relationship="label">
            <Button
              appearance="subtle"
              icon={<ZoomFit20Regular />}
              onClick={onZoomFit}
              className={styles.toolButton}
              aria-label="Fit to view"
            />
          </Tooltip>
        </ToolbarGroup>
      </div>
    </div>
  );
}
