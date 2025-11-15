/**
 * Keyboard Shortcuts Help Modal
 *
 * Displays all available keyboard shortcuts organized by category.
 * Accessible via ? key or Help menu.
 */

import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  makeStyles,
  Text,
  Button,
  Field,
  Input,
} from '@fluentui/react-components';
import { Dismiss24Regular } from '@fluentui/react-icons';
import { useState, useMemo } from 'react';
import { keyboardShortcutManager } from '../../services/keyboardShortcutManager';
import '../../styles/video-editor-theme.css';

const useStyles = makeStyles({
  surface: {
    maxWidth: '800px',
    width: '90vw',
    maxHeight: '80vh',
    backgroundColor: 'var(--editor-panel-bg)',
    border: `1px solid var(--editor-panel-border)`,
    borderRadius: 'var(--editor-radius-lg)',
    boxShadow: 'var(--editor-shadow-xl)',
    animation: 'editorFadeIn var(--editor-transition-base) ease-out',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: 'var(--editor-space-lg)',
    paddingTop: 'var(--editor-space-md)',
  },
  searchField: {
    marginBottom: 'var(--editor-space-md)',
  },
  categoriesContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: 'var(--editor-space-lg)',
    overflowY: 'auto',
    maxHeight: '60vh',
  },
  category: {
    display: 'flex',
    flexDirection: 'column',
    gap: 'var(--editor-space-sm)',
    animation: 'editorSlideIn var(--editor-transition-base) ease-out',
  },
  categoryTitle: {
    fontSize: 'var(--editor-font-size-lg)',
    fontWeight: 'var(--editor-font-weight-semibold)',
    color: 'var(--editor-accent)',
    marginBottom: 'var(--editor-space-xs)',
    textTransform: 'uppercase',
    letterSpacing: '0.5px',
    padding: 'var(--editor-space-sm) 0',
    borderBottom: `2px solid var(--editor-panel-border)`,
  },
  shortcutsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: 'var(--editor-space-xs)',
  },
  shortcutItem: {
    display: 'grid',
    gridTemplateColumns: '200px 1fr',
    gap: 'var(--editor-space-lg)',
    alignItems: 'center',
    paddingTop: 'var(--editor-space-xs)',
    paddingBottom: 'var(--editor-space-xs)',
    paddingLeft: 'var(--editor-space-sm)',
    paddingRight: 'var(--editor-space-sm)',
    borderRadius: 'var(--editor-radius-sm)',
    transition: 'all var(--editor-transition-fast)',
    '&:hover': {
      backgroundColor: 'var(--editor-panel-hover)',
    },
  },
  shortcutKeys: {
    display: 'flex',
    gap: 'var(--editor-space-xs)',
    flexWrap: 'wrap',
  },
  key: {
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 'var(--editor-space-xs) var(--editor-space-sm)',
    backgroundColor: 'var(--editor-bg-elevated)',
    border: `1px solid var(--editor-panel-border)`,
    borderRadius: 'var(--editor-radius-sm)',
    fontSize: 'var(--editor-font-size-sm)',
    fontFamily: 'monospace',
    fontWeight: 'var(--editor-font-weight-semibold)',
    color: 'var(--editor-text-primary)',
    minWidth: '28px',
    boxShadow: 'var(--editor-shadow-sm)',
    transition: 'all var(--editor-transition-fast)',
  },
  description: {
    fontSize: 'var(--editor-font-size-base)',
    color: 'var(--editor-text-secondary)',
  },
  noResults: {
    textAlign: 'center',
    padding: 'var(--editor-space-2xl)',
    color: 'var(--editor-text-tertiary)',
    fontSize: 'var(--editor-font-size-base)',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingBottom: 'var(--editor-space-md)',
    borderBottom: `1px solid var(--editor-panel-border)`,
  },
  title: {
    fontSize: 'var(--editor-font-size-xl)',
    fontWeight: 'var(--editor-font-weight-bold)',
    color: 'var(--editor-text-primary)',
  },
  closeButton: {
    minWidth: 'auto',
    transition: 'all var(--editor-transition-fast)',
    '&:hover': {
      backgroundColor: 'var(--editor-panel-hover)',
      color: 'var(--editor-accent)',
      transform: 'scale(1.1)',
    },
    '&:active': {
      transform: 'scale(0.95)',
    },
  },
});

interface KeyboardShortcutsHelpProps {
  open: boolean;
  onClose: () => void;
}

export function KeyboardShortcutsHelp({ open, onClose }: KeyboardShortcutsHelpProps) {
  const styles = useStyles();
  const [searchQuery, setSearchQuery] = useState('');

  // Get all shortcuts grouped by context
  const shortcutGroups = useMemo(() => {
    return keyboardShortcutManager.getShortcutGroups();
  }, []);

  // Filter shortcuts based on search query
  const filteredGroups = useMemo(() => {
    if (!searchQuery.trim()) {
      return shortcutGroups;
    }

    const query = searchQuery.toLowerCase();
    return shortcutGroups
      .map((group) => ({
        ...group,
        shortcuts: group.shortcuts.filter(
          (shortcut) =>
            shortcut.description.toLowerCase().includes(query) ||
            shortcut.keys.toLowerCase().includes(query)
        ),
      }))
      .filter((group) => group.shortcuts.length > 0);
  }, [shortcutGroups, searchQuery]);

  const hasResults = filteredGroups.length > 0;

  // Parse key combination string into individual keys
  const parseKeys = (keyCombo: string): string[] => {
    return keyCombo.split('+').map((k) => k.trim());
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className={styles.surface}>
        <DialogBody>
          <div className={styles.header}>
            <DialogTitle className={styles.title}>Keyboard Shortcuts</DialogTitle>
            <Button
              appearance="subtle"
              icon={<Dismiss24Regular />}
              onClick={onClose}
              className={styles.closeButton}
              aria-label="Close"
            />
          </div>
          <DialogContent className={styles.content}>
            <Field className={styles.searchField}>
              <Input
                placeholder="Search shortcuts..."
                value={searchQuery}
                onChange={(_, data) => setSearchQuery(data.value)}
                aria-label="Search shortcuts"
              />
            </Field>

            {hasResults ? (
              <div className={styles.categoriesContainer}>
                {filteredGroups.map((group) => (
                  <div key={group.context} className={styles.category}>
                    <Text className={styles.categoryTitle}>{group.name}</Text>
                    <div className={styles.shortcutsList}>
                      {group.shortcuts.map((shortcut) => (
                        <div key={shortcut.id} className={styles.shortcutItem}>
                          <div className={styles.shortcutKeys}>
                            {parseKeys(shortcut.keys).map((key, index) => (
                              <span key={index} className={styles.key}>
                                {key}
                              </span>
                            ))}
                          </div>
                          <Text className={styles.description}>{shortcut.description}</Text>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className={styles.noResults}>
                <Text>No shortcuts found matching &quot;{searchQuery}&quot;</Text>
              </div>
            )}
          </DialogContent>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
