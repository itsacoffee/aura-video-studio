import {
  makeStyles,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  MenuDivider,
  MenuItemRadio,
  MenuGroup,
  MenuGroupHeader,
} from '@fluentui/react-components';
import {
  Cut24Regular,
  Copy24Regular,
  ClipboardPaste24Regular,
  Delete24Regular,
  DocumentCopy24Regular,
  ChevronRight24Regular,
  Eye24Regular,
  EyeOff24Regular,
  Color24Regular,
  Wand24Regular,
  Speaker224Regular,
  SpeakerOff24Regular,
  TopSpeed24Regular,
} from '@fluentui/react-icons';
import React, { useState, useCallback, useEffect } from 'react';
import '../../styles/video-editor-theme.css';

const useStyles = makeStyles({
  menu: {
    minWidth: '220px',
    backgroundColor: 'var(--editor-panel-bg)',
    border: `1px solid var(--editor-panel-border)`,
    borderRadius: 'var(--editor-radius-md)',
    boxShadow: 'var(--editor-shadow-xl)',
    padding: 'var(--editor-space-xs)',
    animation: 'editorFadeIn var(--editor-transition-base) ease-out',
  },
  submenuTrigger: {
    '::after': {
      content: '""',
      marginLeft: 'auto',
    },
  },
  menuItem: {
    padding: 'var(--editor-space-sm) var(--editor-space-md)',
    borderRadius: 'var(--editor-radius-sm)',
    color: 'var(--editor-text-primary)',
    fontSize: 'var(--editor-font-size-sm)',
    cursor: 'pointer',
    transition: 'all var(--editor-transition-fast)',
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--editor-space-md)',
    '&:hover': {
      backgroundColor: 'var(--editor-panel-hover)',
      transform: 'translateX(2px)',
    },
    '&:active': {
      backgroundColor: 'var(--editor-panel-active)',
    },
  },
  shortcut: {
    marginLeft: 'auto',
    opacity: 0.7,
    fontSize: 'var(--editor-font-size-xs)',
    color: 'var(--editor-text-secondary)',
    fontFamily: 'monospace',
  },
  divider: {
    height: '1px',
    backgroundColor: 'var(--editor-panel-border)',
    margin: 'var(--editor-space-xs) 0',
  },
  groupHeader: {
    fontSize: 'var(--editor-font-size-xs)',
    fontWeight: 'var(--editor-font-weight-semibold)',
    color: 'var(--editor-text-secondary)',
    textTransform: 'uppercase',
    letterSpacing: '0.5px',
    padding: 'var(--editor-space-sm) var(--editor-space-md)',
  },
});

interface ClipData {
  id: string;
  type: 'video' | 'audio' | 'image' | 'text';
  duration: number;
  muted?: boolean;
  hidden?: boolean;
}

interface TimelineContextMenuProps {
  targetElement: HTMLElement | null;
  position: { x: number; y: number } | null;
  clipData?: ClipData;
  onClose: () => void;
  onAction: (action: string, data?: unknown) => void;
}

export function TimelineContextMenu({
  targetElement,
  position,
  clipData,
  onClose,
  onAction,
}: TimelineContextMenuProps) {
  const styles = useStyles();
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    if (position && targetElement) {
      setIsOpen(true);
    } else {
      setIsOpen(false);
    }
  }, [position, targetElement]);

  const handleAction = useCallback(
    (action: string, data?: unknown) => {
      onAction(action, data);
      setIsOpen(false);
      onClose();
    },
    [onAction, onClose]
  );

  if (!position || !isOpen) {
    return null;
  }

  return (
    <div
      style={{
        position: 'fixed',
        left: position.x,
        top: position.y,
        zIndex: 10000,
      }}
    >
      <Menu open={isOpen} onOpenChange={(_, data) => !data.open && onClose()}>
        <MenuTrigger>
          <div style={{ width: 0, height: 0 }} />
        </MenuTrigger>
        <MenuPopover className={styles.menu}>
          <MenuList>
            {/* Basic Editing */}
            <MenuItem icon={<Cut24Regular />} onClick={() => handleAction('cut')}>
              Cut
              <span className={styles.shortcut}>Ctrl+X</span>
            </MenuItem>
            <MenuItem icon={<Copy24Regular />} onClick={() => handleAction('copy')}>
              Copy
              <span className={styles.shortcut}>Ctrl+C</span>
            </MenuItem>
            <MenuItem icon={<ClipboardPaste24Regular />} onClick={() => handleAction('paste')}>
              Paste
              <span className={styles.shortcut}>Ctrl+V</span>
            </MenuItem>
            <MenuItem icon={<DocumentCopy24Regular />} onClick={() => handleAction('duplicate')}>
              Duplicate
              <span className={styles.shortcut}>Ctrl+D</span>
            </MenuItem>
            <MenuItem icon={<Delete24Regular />} onClick={() => handleAction('delete')}>
              Delete
              <span className={styles.shortcut}>Del</span>
            </MenuItem>

            <MenuDivider className={styles.divider} />

            {/* Clip Controls */}
            {clipData && (
              <>
                <MenuGroup>
                  <MenuGroupHeader className={styles.groupHeader}>Clip Controls</MenuGroupHeader>
                  <MenuItem
                    icon={clipData.muted ? <SpeakerOff24Regular /> : <Speaker224Regular />}
                    onClick={() => handleAction('toggleMute')}
                  >
                    {clipData.muted ? 'Unmute' : 'Mute'}
                    <span className={styles.shortcut}>M</span>
                  </MenuItem>
                  <MenuItem
                    icon={clipData.hidden ? <EyeOff24Regular /> : <Eye24Regular />}
                    onClick={() => handleAction('toggleVisibility')}
                  >
                    {clipData.hidden ? 'Show' : 'Hide'}
                    <span className={styles.shortcut}>H</span>
                  </MenuItem>
                </MenuGroup>

                <MenuDivider className={styles.divider} />
              </>
            )}

            {/* Split */}
            <MenuItem onClick={() => handleAction('split')}>
              Split at Playhead
              <span className={styles.shortcut}>S</span>
            </MenuItem>

            {/* Speed & Effects Submenu */}
            <Menu>
              <MenuTrigger>
                <MenuItem icon={<TopSpeed24Regular />}>
                  Speed
                  <ChevronRight24Regular style={{ marginLeft: 'auto' }} />
                </MenuItem>
              </MenuTrigger>
              <MenuPopover className={styles.menu}>
                <MenuList>
                  <MenuItemRadio
                    name="speed"
                    value="0.25"
                    onClick={() => handleAction('setSpeed', 0.25)}
                  >
                    0.25x (Slow Motion)
                  </MenuItemRadio>
                  <MenuItemRadio
                    name="speed"
                    value="0.5"
                    onClick={() => handleAction('setSpeed', 0.5)}
                  >
                    0.5x (Half Speed)
                  </MenuItemRadio>
                  <MenuItemRadio
                    name="speed"
                    value="1"
                    onClick={() => handleAction('setSpeed', 1.0)}
                  >
                    1.0x (Normal)
                  </MenuItemRadio>
                  <MenuItemRadio
                    name="speed"
                    value="1.5"
                    onClick={() => handleAction('setSpeed', 1.5)}
                  >
                    1.5x
                  </MenuItemRadio>
                  <MenuItemRadio
                    name="speed"
                    value="2"
                    onClick={() => handleAction('setSpeed', 2.0)}
                  >
                    2.0x (Double Speed)
                  </MenuItemRadio>
                  <MenuItemRadio
                    name="speed"
                    value="4"
                    onClick={() => handleAction('setSpeed', 4.0)}
                  >
                    4.0x (Fast Forward)
                  </MenuItemRadio>
                </MenuList>
              </MenuPopover>
            </Menu>

            {/* Effects Submenu */}
            <Menu>
              <MenuTrigger>
                <MenuItem icon={<Wand24Regular />}>
                  Apply Effect
                  <ChevronRight24Regular style={{ marginLeft: 'auto' }} />
                </MenuItem>
              </MenuTrigger>
              <MenuPopover className={styles.menu}>
                <MenuList>
                  <MenuItem onClick={() => handleAction('applyEffect', 'fade-in')}>
                    Fade In
                  </MenuItem>
                  <MenuItem onClick={() => handleAction('applyEffect', 'fade-out')}>
                    Fade Out
                  </MenuItem>
                  <MenuItem onClick={() => handleAction('applyEffect', 'blur')}>Blur</MenuItem>
                  <MenuItem onClick={() => handleAction('applyEffect', 'sharpen')}>
                    Sharpen
                  </MenuItem>
                  <MenuItem onClick={() => handleAction('applyEffect', 'grayscale')}>
                    Grayscale
                  </MenuItem>
                  <MenuItem onClick={() => handleAction('applyEffect', 'sepia')}>Sepia</MenuItem>
                  <MenuItem onClick={() => handleAction('applyEffect', 'vignette')}>
                    Vignette
                  </MenuItem>
                  <MenuItem onClick={() => handleAction('applyEffect', 'ken-burns')}>
                    Ken Burns (Zoom)
                  </MenuItem>
                </MenuList>
              </MenuPopover>
            </Menu>

            {/* Color Correction Submenu */}
            <Menu>
              <MenuTrigger>
                <MenuItem icon={<Color24Regular />}>
                  Color Correction
                  <ChevronRight24Regular style={{ marginLeft: 'auto' }} />
                </MenuItem>
              </MenuTrigger>
              <MenuPopover className={styles.menu}>
                <MenuList>
                  <MenuItem onClick={() => handleAction('colorCorrection', 'brightness')}>
                    Adjust Brightness
                  </MenuItem>
                  <MenuItem onClick={() => handleAction('colorCorrection', 'contrast')}>
                    Adjust Contrast
                  </MenuItem>
                  <MenuItem onClick={() => handleAction('colorCorrection', 'saturation')}>
                    Adjust Saturation
                  </MenuItem>
                  <MenuItem onClick={() => handleAction('colorCorrection', 'temperature')}>
                    Color Temperature
                  </MenuItem>
                  <MenuItem onClick={() => handleAction('colorCorrection', 'tint')}>Tint</MenuItem>
                  <MenuDivider className={styles.divider} />
                  <MenuItem onClick={() => handleAction('colorCorrection', 'auto')}>
                    Auto Color Correct
                  </MenuItem>
                  <MenuItem onClick={() => handleAction('colorCorrection', 'reset')}>
                    Reset All
                  </MenuItem>
                </MenuList>
              </MenuPopover>
            </Menu>

            <MenuDivider className={styles.divider} />

            {/* Transitions */}
            <Menu>
              <MenuTrigger>
                <MenuItem>
                  Add Transition
                  <ChevronRight24Regular style={{ marginLeft: 'auto' }} />
                </MenuItem>
              </MenuTrigger>
              <MenuPopover className={styles.menu}>
                <MenuList>
                  <MenuGroup>
                    <MenuGroupHeader className={styles.groupHeader}>Before Clip</MenuGroupHeader>
                    <MenuItem
                      onClick={() =>
                        handleAction('addTransition', { type: 'crossfade', position: 'before' })
                      }
                    >
                      Crossfade
                    </MenuItem>
                    <MenuItem
                      onClick={() =>
                        handleAction('addTransition', { type: 'dip-to-black', position: 'before' })
                      }
                    >
                      Dip to Black
                    </MenuItem>
                    <MenuItem
                      onClick={() =>
                        handleAction('addTransition', { type: 'wipe', position: 'before' })
                      }
                    >
                      Wipe
                    </MenuItem>
                    <MenuItem
                      onClick={() =>
                        handleAction('addTransition', { type: 'slide', position: 'before' })
                      }
                    >
                      Slide
                    </MenuItem>
                  </MenuGroup>
                  <MenuDivider className={styles.divider} />
                  <MenuGroup>
                    <MenuGroupHeader className={styles.groupHeader}>After Clip</MenuGroupHeader>
                    <MenuItem
                      onClick={() =>
                        handleAction('addTransition', { type: 'crossfade', position: 'after' })
                      }
                    >
                      Crossfade
                    </MenuItem>
                    <MenuItem
                      onClick={() =>
                        handleAction('addTransition', { type: 'dip-to-black', position: 'after' })
                      }
                    >
                      Dip to Black
                    </MenuItem>
                    <MenuItem
                      onClick={() =>
                        handleAction('addTransition', { type: 'wipe', position: 'after' })
                      }
                    >
                      Wipe
                    </MenuItem>
                    <MenuItem
                      onClick={() =>
                        handleAction('addTransition', { type: 'slide', position: 'after' })
                      }
                    >
                      Slide
                    </MenuItem>
                  </MenuGroup>
                </MenuList>
              </MenuPopover>
            </Menu>

            <MenuDivider className={styles.divider} />

            {/* Properties */}
            <MenuItem onClick={() => handleAction('properties')}>
              Properties
              <span className={styles.shortcut}>Ctrl+I</span>
            </MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>
    </div>
  );
}

// Hook for easy usage
// eslint-disable-next-line react-refresh/only-export-components -- Hook for TimelineContextMenu component
export function useTimelineContextMenu() {
  const [contextMenu, setContextMenu] = useState<{
    position: { x: number; y: number } | null;
    targetElement: HTMLElement | null;
    clipData?: ClipData;
  }>({
    position: null,
    targetElement: null,
  });

  const showContextMenu = useCallback((event: React.MouseEvent, clipData?: ClipData) => {
    event.preventDefault();
    event.stopPropagation();
    setContextMenu({
      position: { x: event.clientX, y: event.clientY },
      targetElement: event.currentTarget as HTMLElement,
      clipData,
    });
  }, []);

  const hideContextMenu = useCallback(() => {
    setContextMenu({
      position: null,
      targetElement: null,
    });
  }, []);

  // Close context menu on click outside
  useEffect(() => {
    const handleClick = () => {
      if (contextMenu.position) {
        hideContextMenu();
      }
    };

    document.addEventListener('click', handleClick);
    return () => document.removeEventListener('click', handleClick);
  }, [contextMenu.position, hideContextMenu]);

  return {
    contextMenu,
    showContextMenu,
    hideContextMenu,
  };
}
