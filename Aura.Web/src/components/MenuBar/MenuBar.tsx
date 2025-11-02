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
  Toolbar,
  ToolbarButton,
} from '@fluentui/react-components';
import {
  Folder24Regular,
  FolderOpen24Regular,
  Save24Regular,
  ShareAndroid24Regular,
  ArrowUndo24Regular,
  ArrowRedo24Regular,
  Cut24Regular,
  Copy24Regular,
  ClipboardPaste24Regular,
  Question24Regular,
  Keyboard24Regular,
  ArrowMaximize24Regular,
  ArrowMinimize24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useWorkspaceLayoutStore } from '../../state/workspaceLayout';
import { WorkspaceLayoutSwitcher } from '../EditorLayout/WorkspaceLayoutSwitcher';

const useStyles = makeStyles({
  menuBar: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalM}`,
    backgroundColor: tokens.colorNeutralBackground2,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    minHeight: '40px',
    boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08)',
  },
  menuButton: {
    minWidth: 'auto',
    paddingLeft: tokens.spacingHorizontalM,
    paddingRight: tokens.spacingHorizontalM,
    borderRadius: tokens.borderRadiusMedium,
    transition: 'all 0.2s cubic-bezier(0.4, 0, 0.2, 1)',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3,
      transform: 'translateY(-1px)',
    },
  },
  divider: {
    height: '20px',
    width: '1px',
    backgroundColor: tokens.colorNeutralStroke2,
    margin: `0 ${tokens.spacingHorizontalS}`,
  },
  toolbarSection: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    marginLeft: tokens.spacingHorizontalL,
  },
  shortcut: {
    marginLeft: 'auto',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    fontFamily: 'monospace',
    backgroundColor: tokens.colorNeutralBackground3,
    padding: `2px ${tokens.spacingHorizontalXS}`,
    borderRadius: tokens.borderRadiusSmall,
  },
  statusSection: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginLeft: 'auto',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  projectName: {
    fontWeight: 600,
    color: tokens.colorNeutralForeground2,
  },
  statusText: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  statusDot: {
    width: '8px',
    height: '8px',
    borderRadius: '50%',
    display: 'inline-block',
  },
  statusIdle: {
    backgroundColor: tokens.colorNeutralForeground4,
  },
  statusSaving: {
    backgroundColor: tokens.colorPaletteBlueForeground2,
    animation: 'pulse 1s ease-in-out infinite',
  },
  statusSaved: {
    backgroundColor: tokens.colorPaletteGreenForeground1,
  },
  statusError: {
    backgroundColor: tokens.colorPaletteRedForeground1,
  },
});

interface MenuBarProps {
  onImportMedia?: () => void;
  onExportVideo?: () => void;
  onShowKeyboardShortcuts?: () => void;
  onSaveProject?: () => void;
  projectName?: string | null;
  isDirty?: boolean;
  autosaveStatus?: 'idle' | 'saving' | 'saved' | 'error';
  lastSaved?: Date | null;
}

export function MenuBar({
  onImportMedia,
  onExportVideo,
  onShowKeyboardShortcuts,
  onSaveProject,
  projectName,
  isDirty = false,
  autosaveStatus = 'idle',
  lastSaved,
}: MenuBarProps) {
  const styles = useStyles();
  const navigate = useNavigate();
  const [undoStack] = useState<string[]>([]);
  const [redoStack] = useState<string[]>([]);
  const { isFullscreen, toggleFullscreen } = useWorkspaceLayoutStore();

  const handleUndo = () => {
    // Placeholder for undo functionality
  };

  const handleRedo = () => {
    // Placeholder for redo functionality
  };

  const handleCut = () => {
    document.execCommand('cut');
  };

  const handleCopy = () => {
    document.execCommand('copy');
  };

  const handlePaste = () => {
    document.execCommand('paste');
  };

  return (
    <div className={styles.menuBar}>
      {/* File Menu */}
      <Menu>
        <MenuTrigger>
          <Button appearance="subtle" className={styles.menuButton}>
            File
          </Button>
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            <MenuItem icon={<FolderOpen24Regular />} onClick={() => navigate('/projects')}>
              Open Project...
            </MenuItem>
            <MenuItem icon={<Folder24Regular />} onClick={onImportMedia}>
              Import Media...
              <span className={styles.shortcut}>Ctrl+I</span>
            </MenuItem>
            <MenuDivider />
            <MenuItem icon={<Save24Regular />} onClick={onSaveProject}>
              Save
              <span className={styles.shortcut}>Ctrl+S</span>
            </MenuItem>
            <MenuItem>
              Save As...
              <span className={styles.shortcut}>Ctrl+Shift+S</span>
            </MenuItem>
            <MenuDivider />
            <MenuItem icon={<ShareAndroid24Regular />} onClick={onExportVideo}>
              Export Video...
              <span className={styles.shortcut}>Ctrl+E</span>
            </MenuItem>
            <MenuDivider />
            <MenuItem onClick={() => navigate('/settings')}>Settings...</MenuItem>
            <MenuItem onClick={() => navigate('/')}>Exit</MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>

      {/* Edit Menu */}
      <Menu>
        <MenuTrigger>
          <Button appearance="subtle" className={styles.menuButton}>
            Edit
          </Button>
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            <MenuItem
              icon={<ArrowUndo24Regular />}
              onClick={handleUndo}
              disabled={undoStack.length === 0}
            >
              Undo
              <span className={styles.shortcut}>Ctrl+Z</span>
            </MenuItem>
            <MenuItem
              icon={<ArrowRedo24Regular />}
              onClick={handleRedo}
              disabled={redoStack.length === 0}
            >
              Redo
              <span className={styles.shortcut}>Ctrl+Y</span>
            </MenuItem>
            <MenuDivider />
            <MenuItem icon={<Cut24Regular />} onClick={handleCut}>
              Cut
              <span className={styles.shortcut}>Ctrl+X</span>
            </MenuItem>
            <MenuItem icon={<Copy24Regular />} onClick={handleCopy}>
              Copy
              <span className={styles.shortcut}>Ctrl+C</span>
            </MenuItem>
            <MenuItem icon={<ClipboardPaste24Regular />} onClick={handlePaste}>
              Paste
              <span className={styles.shortcut}>Ctrl+V</span>
            </MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>

      {/* View Menu */}
      <Menu>
        <MenuTrigger>
          <Button appearance="subtle" className={styles.menuButton}>
            View
          </Button>
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            <MenuItem onClick={() => navigate('/timeline')}>Timeline</MenuItem>
            <MenuItem onClick={() => navigate('/assets')}>Asset Library</MenuItem>
            <MenuItem onClick={() => navigate('/projects')}>Projects</MenuItem>
            <MenuDivider />
            <MenuItem>Zoom In</MenuItem>
            <MenuItem>Zoom Out</MenuItem>
            <MenuItem>Fit to Window</MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>

      {/* Help Menu */}
      <Menu>
        <MenuTrigger>
          <Button appearance="subtle" className={styles.menuButton}>
            Help
          </Button>
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            <MenuItem icon={<Keyboard24Regular />} onClick={onShowKeyboardShortcuts}>
              Keyboard Shortcuts
              <span className={styles.shortcut}>Ctrl+K</span>
            </MenuItem>
            <MenuItem icon={<Question24Regular />}>Documentation</MenuItem>
            <MenuDivider />
            <MenuItem>About Aura Studio</MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>

      {/* Toolbar Section */}
      <div className={styles.toolbarSection}>
        <div className={styles.divider} />
        <Toolbar>
          <ToolbarButton
            aria-label="Undo"
            icon={<ArrowUndo24Regular />}
            onClick={handleUndo}
            disabled={undoStack.length === 0}
          />
          <ToolbarButton
            aria-label="Redo"
            icon={<ArrowRedo24Regular />}
            onClick={handleRedo}
            disabled={redoStack.length === 0}
          />
        </Toolbar>
        <div className={styles.divider} />
        <WorkspaceLayoutSwitcher />
        <ToolbarButton
          aria-label={isFullscreen ? 'Exit fullscreen' : 'Enter fullscreen'}
          icon={isFullscreen ? <ArrowMinimize24Regular /> : <ArrowMaximize24Regular />}
          onClick={toggleFullscreen}
          title={isFullscreen ? 'Exit Fullscreen (ESC)' : 'Enter Fullscreen (F11)'}
        />
      </div>

      {/* Project Status Section */}
      <div className={styles.statusSection}>
        {projectName && (
          <span className={styles.projectName}>
            {projectName}
            {isDirty ? ' *' : ''}
          </span>
        )}
        {autosaveStatus !== 'idle' && (
          <span className={styles.statusText}>
            <span
              className={`${styles.statusDot} ${
                autosaveStatus === 'saving'
                  ? styles.statusSaving
                  : autosaveStatus === 'saved'
                    ? styles.statusSaved
                    : autosaveStatus === 'error'
                      ? styles.statusError
                      : styles.statusIdle
              }`}
            />
            {autosaveStatus === 'saving' && 'Saving...'}
            {autosaveStatus === 'saved' && 'All changes saved'}
            {autosaveStatus === 'error' && 'Save failed'}
          </span>
        )}
        {lastSaved && autosaveStatus === 'idle' && (
          <span style={{ fontSize: tokens.fontSizeBase200 }}>
            Last saved: {formatLastSaved(lastSaved)}
          </span>
        )}
      </div>
    </div>
  );
}

// Helper function to format last saved time
function formatLastSaved(date: Date): string {
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);

  if (diffMins < 1) return 'just now';
  if (diffMins === 1) return '1 minute ago';
  if (diffMins < 60) return `${diffMins} minutes ago`;

  const diffHours = Math.floor(diffMins / 60);
  if (diffHours === 1) return '1 hour ago';
  if (diffHours < 24) return `${diffHours} hours ago`;

  return date.toLocaleString();
}
