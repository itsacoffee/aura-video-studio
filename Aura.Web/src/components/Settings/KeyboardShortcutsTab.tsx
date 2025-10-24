import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Title2,
  Text,
  Button,
  Card,
  Field,
  Input,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableRow,
  TableHeader,
  TableHeaderCell,
} from '@fluentui/react-components';
import { Save24Regular, ArrowReset24Regular, Keyboard24Regular } from '@fluentui/react-icons';
import { apiUrl } from '../../config/api';

const useStyles = makeStyles({
  section: {
    padding: tokens.spacingVerticalXL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  infoCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    marginBottom: tokens.spacingVerticalL,
  },
  table: {
    width: '100%',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalL,
  },
  shortcutInput: {
    maxWidth: '200px',
  },
  categoryHeader: {
    marginTop: tokens.spacingVerticalXL,
    marginBottom: tokens.spacingVerticalM,
  },
});

interface ShortcutMapping {
  action: string;
  description: string;
  defaultShortcut: string;
  currentShortcut: string;
  category: string;
}

const DEFAULT_SHORTCUTS: ShortcutMapping[] = [
  // Playback
  { action: 'play-pause', description: 'Play/Pause', defaultShortcut: 'Space', currentShortcut: 'Space', category: 'Playback' },
  { action: 'forward', description: 'Forward 5s', defaultShortcut: 'ArrowRight', currentShortcut: 'ArrowRight', category: 'Playback' },
  { action: 'backward', description: 'Backward 5s', defaultShortcut: 'ArrowLeft', currentShortcut: 'ArrowLeft', category: 'Playback' },
  { action: 'frame-forward', description: 'Next Frame', defaultShortcut: 'Period', currentShortcut: 'Period', category: 'Playback' },
  { action: 'frame-backward', description: 'Previous Frame', defaultShortcut: 'Comma', currentShortcut: 'Comma', category: 'Playback' },
  { action: 'speed-faster', description: 'Increase Speed', defaultShortcut: 'L', currentShortcut: 'L', category: 'Playback' },
  { action: 'speed-slower', description: 'Decrease Speed', defaultShortcut: 'J', currentShortcut: 'J', category: 'Playback' },
  { action: 'speed-normal', description: 'Normal Speed', defaultShortcut: 'K', currentShortcut: 'K', category: 'Playback' },
  
  // Timeline
  { action: 'split-clip', description: 'Split Clip', defaultShortcut: 'S', currentShortcut: 'S', category: 'Timeline' },
  { action: 'delete-clip', description: 'Delete Clip', defaultShortcut: 'Delete', currentShortcut: 'Delete', category: 'Timeline' },
  { action: 'undo', description: 'Undo', defaultShortcut: 'Ctrl+Z', currentShortcut: 'Ctrl+Z', category: 'Timeline' },
  { action: 'redo', description: 'Redo', defaultShortcut: 'Ctrl+Y', currentShortcut: 'Ctrl+Y', category: 'Timeline' },
  { action: 'copy', description: 'Copy', defaultShortcut: 'Ctrl+C', currentShortcut: 'Ctrl+C', category: 'Timeline' },
  { action: 'paste', description: 'Paste', defaultShortcut: 'Ctrl+V', currentShortcut: 'Ctrl+V', category: 'Timeline' },
  { action: 'cut', description: 'Cut', defaultShortcut: 'Ctrl+X', currentShortcut: 'Ctrl+X', category: 'Timeline' },
  { action: 'select-all', description: 'Select All', defaultShortcut: 'Ctrl+A', currentShortcut: 'Ctrl+A', category: 'Timeline' },
  { action: 'zoom-in', description: 'Zoom In', defaultShortcut: 'Plus', currentShortcut: 'Plus', category: 'Timeline' },
  { action: 'zoom-out', description: 'Zoom Out', defaultShortcut: 'Minus', currentShortcut: 'Minus', category: 'Timeline' },
  { action: 'zoom-fit', description: 'Zoom to Fit', defaultShortcut: 'Backslash', currentShortcut: 'Backslash', category: 'Timeline' },
  
  // Markers
  { action: 'add-marker', description: 'Add Marker', defaultShortcut: 'M', currentShortcut: 'M', category: 'Markers' },
  { action: 'prev-marker', description: 'Previous Marker', defaultShortcut: 'Ctrl+ArrowLeft', currentShortcut: 'Ctrl+ArrowLeft', category: 'Markers' },
  { action: 'next-marker', description: 'Next Marker', defaultShortcut: 'Ctrl+ArrowRight', currentShortcut: 'Ctrl+ArrowRight', category: 'Markers' },
  
  // General
  { action: 'save', description: 'Save Project', defaultShortcut: 'Ctrl+S', currentShortcut: 'Ctrl+S', category: 'General' },
  { action: 'save-as', description: 'Save As', defaultShortcut: 'Ctrl+Shift+S', currentShortcut: 'Ctrl+Shift+S', category: 'General' },
  { action: 'export', description: 'Export/Render', defaultShortcut: 'Ctrl+M', currentShortcut: 'Ctrl+M', category: 'General' },
  { action: 'new-project', description: 'New Project', defaultShortcut: 'Ctrl+N', currentShortcut: 'Ctrl+N', category: 'General' },
  { action: 'open-project', description: 'Open Project', defaultShortcut: 'Ctrl+O', currentShortcut: 'Ctrl+O', category: 'General' },
  { action: 'shortcuts', description: 'Show Shortcuts', defaultShortcut: 'Ctrl+K', currentShortcut: 'Ctrl+K', category: 'General' },
  { action: 'search', description: 'Search/Command Palette', defaultShortcut: 'Ctrl+P', currentShortcut: 'Ctrl+P', category: 'General' },
  { action: 'fullscreen', description: 'Toggle Fullscreen', defaultShortcut: 'F11', currentShortcut: 'F11', category: 'General' },
];

export function KeyboardShortcutsTab() {
  const styles = useStyles();
  const [modified, setModified] = useState(false);
  const [saving, setSaving] = useState(false);
  const [shortcuts, setShortcuts] = useState<ShortcutMapping[]>(DEFAULT_SHORTCUTS);
  const [enableKeyboardShortcuts, setEnableKeyboardShortcuts] = useState(true);
  const [editingShortcut, setEditingShortcut] = useState<string | null>(null);

  useEffect(() => {
    fetchShortcuts();
  }, []);

  const fetchShortcuts = async () => {
    try {
      const response = await fetch(apiUrl('/api/settings/shortcuts'));
      if (response.ok) {
        const data = await response.json();
        if (data.shortcuts) {
          setShortcuts(data.shortcuts);
        }
        setEnableKeyboardShortcuts(data.enabled !== false);
      }
    } catch (error) {
      console.error('Error fetching shortcuts:', error);
    }
  };

  const saveShortcuts = async () => {
    setSaving(true);
    try {
      const response = await fetch('/api/settings/shortcuts', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          enabled: enableKeyboardShortcuts,
          shortcuts,
        }),
      });
      if (response.ok) {
        alert('Keyboard shortcuts saved successfully');
        setModified(false);
      } else {
        alert('Error saving keyboard shortcuts');
      }
    } catch (error) {
      console.error('Error saving shortcuts:', error);
      alert('Error saving keyboard shortcuts');
    } finally {
      setSaving(false);
    }
  };

  const updateShortcut = (action: string, newShortcut: string) => {
    setShortcuts((prev) =>
      prev.map((s) => (s.action === action ? { ...s, currentShortcut: newShortcut } : s))
    );
    setModified(true);
  };

  const resetToDefaults = () => {
    if (confirm('Reset all keyboard shortcuts to defaults?')) {
      setShortcuts(DEFAULT_SHORTCUTS);
      setModified(true);
    }
  };

  const resetSingleShortcut = (action: string) => {
    const defaultShortcut = DEFAULT_SHORTCUTS.find((s) => s.action === action);
    if (defaultShortcut) {
      updateShortcut(action, defaultShortcut.defaultShortcut);
    }
  };

  const groupedShortcuts = shortcuts.reduce((acc, shortcut) => {
    if (!acc[shortcut.category]) {
      acc[shortcut.category] = [];
    }
    acc[shortcut.category].push(shortcut);
    return acc;
  }, {} as Record<string, ShortcutMapping[]>);

  return (
    <Card className={styles.section}>
      <Title2>
        <Keyboard24Regular style={{ marginRight: tokens.spacingHorizontalS, verticalAlign: 'middle' }} />
        Keyboard Shortcuts
      </Title2>
      <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
        Customize keyboard shortcuts for faster workflow
      </Text>

      <Card className={styles.infoCard}>
        <Text weight="semibold" size={300}>
          💡 Tips
        </Text>
        <ul style={{ marginTop: tokens.spacingVerticalS, marginLeft: tokens.spacingHorizontalL }}>
          <li>
            <Text size={200}>Click on a shortcut to edit it</Text>
          </li>
          <li>
            <Text size={200}>Press the key combination you want to use</Text>
          </li>
          <li>
            <Text size={200}>Modifiers: Ctrl, Alt, Shift can be combined</Text>
          </li>
          <li>
            <Text size={200}>Press Escape to cancel editing</Text>
          </li>
        </ul>
      </Card>

      <div className={styles.form}>
        <Field label="Enable Keyboard Shortcuts">
          <Switch
            checked={enableKeyboardShortcuts}
            onChange={(_, data) => {
              setEnableKeyboardShortcuts(data.checked);
              setModified(true);
            }}
          />
          <Text size={200}>
            {enableKeyboardShortcuts ? 'Enabled' : 'Disabled'}
          </Text>
        </Field>

        {Object.entries(groupedShortcuts).map(([category, categoryShortcuts]) => (
          <div key={category}>
            <Text size={400} weight="semibold" className={styles.categoryHeader}>
              {category}
            </Text>
            <Table className={styles.table}>
              <TableHeader>
                <TableRow>
                  <TableHeaderCell>Action</TableHeaderCell>
                  <TableHeaderCell>Shortcut</TableHeaderCell>
                  <TableHeaderCell>Actions</TableHeaderCell>
                </TableRow>
              </TableHeader>
              <TableBody>
                {categoryShortcuts.map((shortcut) => (
                  <TableRow key={shortcut.action}>
                    <TableCell>{shortcut.description}</TableCell>
                    <TableCell>
                      {editingShortcut === shortcut.action ? (
                        <Input
                          className={styles.shortcutInput}
                          value={shortcut.currentShortcut}
                          onChange={(e) => updateShortcut(shortcut.action, e.target.value)}
                          onBlur={() => setEditingShortcut(null)}
                          onKeyDown={(e) => {
                            if (e.key === 'Escape') {
                              setEditingShortcut(null);
                            } else if (e.key === 'Enter') {
                              setEditingShortcut(null);
                            }
                          }}
                          autoFocus
                        />
                      ) : (
                        <Button
                          appearance="subtle"
                          size="small"
                          onClick={() => setEditingShortcut(shortcut.action)}
                        >
                          {shortcut.currentShortcut}
                        </Button>
                      )}
                    </TableCell>
                    <TableCell>
                      <Button
                        appearance="subtle"
                        size="small"
                        icon={<ArrowReset24Regular />}
                        onClick={() => resetSingleShortcut(shortcut.action)}
                        disabled={shortcut.currentShortcut === shortcut.defaultShortcut}
                      >
                        Reset
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        ))}

        {modified && (
          <Text size={200} style={{ color: tokens.colorPaletteYellowForeground1 }}>
            ⚠️ You have unsaved changes
          </Text>
        )}

        <div className={styles.actions}>
          <Button
            appearance="secondary"
            icon={<ArrowReset24Regular />}
            onClick={resetToDefaults}
          >
            Reset All to Defaults
          </Button>
          <Button
            appearance="primary"
            icon={<Save24Regular />}
            onClick={saveShortcuts}
            disabled={!modified || saving}
          >
            {saving ? 'Saving...' : 'Save Shortcuts'}
          </Button>
        </div>
      </div>
    </Card>
  );
}
