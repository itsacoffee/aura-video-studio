/**
 * TopMenuBar Component
 * Professional desktop-style menu bar matching Adobe Premiere Pro conventions
 */

import {
  makeStyles,
  tokens,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  MenuDivider,
  Button,
} from '@fluentui/react-components';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

const useStyles = makeStyles({
  menuBar: {
    display: 'flex',
    alignItems: 'center',
    height: '32px',
    backgroundColor: 'var(--panel-header-bg, var(--color-surface))',
    borderBottom: `1px solid var(--panel-border, ${tokens.colorNeutralStroke1})`,
    padding: '0 var(--space-1)',
    gap: '2px',
    userSelect: 'none',
  },
  menuButton: {
    minWidth: 'auto',
    height: '28px',
    padding: '0 var(--space-2)',
    fontSize: '13px',
    borderRadius: 'var(--border-radius-sm)',
    transition: 'all var(--transition-button)',
    fontWeight: 400,
    color: 'var(--color-text-primary)',
    ':hover': {
      backgroundColor: 'var(--panel-hover, var(--color-surface))',
    },
  },
  shortcut: {
    marginLeft: 'auto',
    fontSize: '12px',
    color: 'var(--color-text-secondary)',
    fontFamily: 'monospace',
    opacity: 0.7,
  },
  menuItemContent: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    width: '100%',
    gap: 'var(--space-4)',
  },
});

interface TopMenuBarProps {
  onImportMedia?: () => void;
  onExportVideo?: () => void;
  onSaveProject?: () => void;
  onShowKeyboardShortcuts?: () => void;
}

export function TopMenuBar({
  onImportMedia,
  onExportVideo,
  onSaveProject,
  onShowKeyboardShortcuts,
}: TopMenuBarProps) {
  const styles = useStyles();
  const navigate = useNavigate();
  const [openMenus, setOpenMenus] = useState<Record<string, boolean>>({});

  const handleMenuToggle = (menuId: string, isOpen: boolean) => {
    setOpenMenus({ ...openMenus, [menuId]: isOpen });
  };

  return (
    <div className={styles.menuBar}>
      {/* File Menu */}
      <Menu open={openMenus.file} onOpenChange={(_, data) => handleMenuToggle('file', data.open)}>
        <MenuTrigger>
          <Button appearance="subtle" className={styles.menuButton}>
            File
          </Button>
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            <MenuItem onClick={() => navigate('/create')}>
              <div className={styles.menuItemContent}>
                <span>New Project...</span>
                <span className={styles.shortcut}>Ctrl+N</span>
              </div>
            </MenuItem>
            <MenuItem onClick={() => navigate('/projects')}>
              <div className={styles.menuItemContent}>
                <span>Open Project...</span>
                <span className={styles.shortcut}>Ctrl+O</span>
              </div>
            </MenuItem>
            <MenuDivider />
            <MenuItem onClick={onSaveProject}>
              <div className={styles.menuItemContent}>
                <span>Save</span>
                <span className={styles.shortcut}>Ctrl+S</span>
              </div>
            </MenuItem>
            <MenuItem>
              <div className={styles.menuItemContent}>
                <span>Save As...</span>
                <span className={styles.shortcut}>Ctrl+Shift+S</span>
              </div>
            </MenuItem>
            <MenuDivider />
            <MenuItem onClick={onImportMedia}>
              <div className={styles.menuItemContent}>
                <span>Import Media...</span>
                <span className={styles.shortcut}>Ctrl+I</span>
              </div>
            </MenuItem>
            <MenuItem onClick={onExportVideo}>
              <div className={styles.menuItemContent}>
                <span>Export...</span>
                <span className={styles.shortcut}>Ctrl+M</span>
              </div>
            </MenuItem>
            <MenuDivider />
            <MenuItem onClick={() => navigate('/export-history')}>Export History</MenuItem>
            <MenuItem onClick={() => navigate('/projects')}>Project Settings</MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>

      {/* Edit Menu */}
      <Menu open={openMenus.edit} onOpenChange={(_, data) => handleMenuToggle('edit', data.open)}>
        <MenuTrigger>
          <Button appearance="subtle" className={styles.menuButton}>
            Edit
          </Button>
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            <MenuItem>
              <div className={styles.menuItemContent}>
                <span>Undo</span>
                <span className={styles.shortcut}>Ctrl+Z</span>
              </div>
            </MenuItem>
            <MenuItem>
              <div className={styles.menuItemContent}>
                <span>Redo</span>
                <span className={styles.shortcut}>Ctrl+Y</span>
              </div>
            </MenuItem>
            <MenuDivider />
            <MenuItem>
              <div className={styles.menuItemContent}>
                <span>Cut</span>
                <span className={styles.shortcut}>Ctrl+X</span>
              </div>
            </MenuItem>
            <MenuItem>
              <div className={styles.menuItemContent}>
                <span>Copy</span>
                <span className={styles.shortcut}>Ctrl+C</span>
              </div>
            </MenuItem>
            <MenuItem>
              <div className={styles.menuItemContent}>
                <span>Paste</span>
                <span className={styles.shortcut}>Ctrl+V</span>
              </div>
            </MenuItem>
            <MenuDivider />
            <MenuItem onClick={() => navigate('/settings')}>
              <div className={styles.menuItemContent}>
                <span>Preferences...</span>
                <span className={styles.shortcut}>Ctrl+,</span>
              </div>
            </MenuItem>
            <MenuItem onClick={onShowKeyboardShortcuts}>
              <div className={styles.menuItemContent}>
                <span>Keyboard Shortcuts...</span>
                <span className={styles.shortcut}>Ctrl+K</span>
              </div>
            </MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>

      {/* View Menu */}
      <Menu open={openMenus.view} onOpenChange={(_, data) => handleMenuToggle('view', data.open)}>
        <MenuTrigger>
          <Button appearance="subtle" className={styles.menuButton}>
            View
          </Button>
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            <MenuItem>Zoom In</MenuItem>
            <MenuItem>Zoom Out</MenuItem>
            <MenuItem>
              <div className={styles.menuItemContent}>
                <span>Fit to Screen</span>
                <span className={styles.shortcut}>Shift+Z</span>
              </div>
            </MenuItem>
            <MenuDivider />
            <MenuItem>Show Timeline</MenuItem>
            <MenuItem>Show Properties Panel</MenuItem>
            <MenuItem>Show Media Library</MenuItem>
            <MenuItem>Show Effects Library</MenuItem>
            <MenuDivider />
            <MenuItem>
              <div className={styles.menuItemContent}>
                <span>Full Screen</span>
                <span className={styles.shortcut}>F11</span>
              </div>
            </MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>

      {/* Window Menu */}
      <Menu
        open={openMenus.window}
        onOpenChange={(_, data) => handleMenuToggle('window', data.open)}
      >
        <MenuTrigger>
          <Button appearance="subtle" className={styles.menuButton}>
            Window
          </Button>
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            <MenuItem>Workspace: Editing</MenuItem>
            <MenuItem>Workspace: Color</MenuItem>
            <MenuItem>Workspace: Audio</MenuItem>
            <MenuItem>Workspace: Effects</MenuItem>
            <MenuDivider />
            <MenuItem>Save Workspace...</MenuItem>
            <MenuItem>Reset Workspace</MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>

      {/* Help Menu */}
      <Menu open={openMenus.help} onOpenChange={(_, data) => handleMenuToggle('help', data.open)}>
        <MenuTrigger>
          <Button appearance="subtle" className={styles.menuButton}>
            Help
          </Button>
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            <MenuItem onClick={onShowKeyboardShortcuts}>Keyboard Shortcuts</MenuItem>
            <MenuItem
              onClick={() =>
                window.open('https://github.com/Saiyan9001/aura-video-studio', '_blank')
              }
            >
              Documentation
            </MenuItem>
            <MenuDivider />
            <MenuItem onClick={() => navigate('/health')}>System Health</MenuItem>
            <MenuItem>About Aura Studio</MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>
    </div>
  );
}
