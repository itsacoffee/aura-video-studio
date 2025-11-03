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
  tokens,
  Text,
  Button,
  Field,
  Input,
} from '@fluentui/react-components';
import { Dismiss24Regular } from '@fluentui/react-icons';
import { useState, useMemo } from 'react';
import { keyboardShortcutManager } from '../../services/keyboardShortcutManager';

const useStyles = makeStyles({
  surface: {
    maxWidth: '800px',
    width: '90vw',
    maxHeight: '80vh',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    paddingTop: tokens.spacingVerticalM,
  },
  searchField: {
    marginBottom: tokens.spacingVerticalM,
  },
  categoriesContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    overflowY: 'auto',
    maxHeight: '60vh',
  },
  category: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  categoryTitle: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
    marginBottom: tokens.spacingVerticalXS,
  },
  shortcutsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  shortcutItem: {
    display: 'grid',
    gridTemplateColumns: '200px 1fr',
    gap: tokens.spacingHorizontalL,
    alignItems: 'center',
    paddingTop: tokens.spacingVerticalXS,
    paddingBottom: tokens.spacingVerticalXS,
  },
  shortcutKeys: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    flexWrap: 'wrap',
  },
  key: {
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalSNudge}`,
    backgroundColor: tokens.colorNeutralBackground3,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase200,
    fontFamily: 'monospace',
    minWidth: '24px',
    boxShadow: `0 1px 2px ${tokens.colorNeutralShadowAmbient}`,
  },
  description: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground2,
  },
  noResults: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  closeButton: {
    minWidth: 'auto',
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
            <DialogTitle>Keyboard Shortcuts</DialogTitle>
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
