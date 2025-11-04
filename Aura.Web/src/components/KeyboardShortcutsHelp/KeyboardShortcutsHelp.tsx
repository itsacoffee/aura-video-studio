/**
 * Keyboard shortcuts help dialog
 * Displays all available keyboard shortcuts organized by category
 */

import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  Button,
  Input,
  makeStyles,
  tokens,
  shorthands,
} from '@fluentui/react-components';
import { Dismiss24Regular, Search24Regular } from '@fluentui/react-icons';
import React, { useMemo, useState } from 'react';
import { useKeybindingsStore, shortcutMetadata } from '../../state/keybindings';
import type { ShortcutMetadata } from '../../types/keybinding';
import { formatShortcutForDisplay, getCategoryDescription } from '../../utils/keybinding-utils';

const useStyles = makeStyles({
  dialogSurface: {
    maxWidth: '800px',
    width: '90vw',
    maxHeight: '80vh',
  },
  dialogBody: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalM),
  },
  searchContainer: {
    marginBottom: tokens.spacingVerticalM,
  },
  categoryContainer: {
    marginBottom: tokens.spacingVerticalL,
  },
  categoryTitle: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
    marginBottom: tokens.spacingVerticalS,
    ...shorthands.padding(tokens.spacingVerticalXS, '0'),
    ...shorthands.borderBottom('2px', 'solid', tokens.colorNeutralStroke2),
  },
  shortcutList: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalXS),
  },
  shortcutItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    ...shorthands.padding(tokens.spacingVerticalXS, tokens.spacingHorizontalS),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  shortcutDescription: {
    flex: 1,
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground1,
  },
  shortcutKey: {
    fontSize: tokens.fontSizeBase200,
    fontFamily: tokens.fontFamilyMonospace,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
    backgroundColor: tokens.colorNeutralBackground3,
    ...shorthands.padding(tokens.spacingVerticalXXS, tokens.spacingHorizontalXS),
    ...shorthands.borderRadius(tokens.borderRadiusSmall),
    ...shorthands.border('1px', 'solid', tokens.colorNeutralStroke2),
  },
  emptyState: {
    textAlign: 'center',
    ...shorthands.padding(tokens.spacingVerticalXXL),
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase300,
  },
  footer: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    ...shorthands.padding(tokens.spacingVerticalM, '0'),
    ...shorthands.borderTop('1px', 'solid', tokens.colorNeutralStroke2),
    marginTop: tokens.spacingVerticalM,
  },
  resetButton: {
    marginRight: 'auto',
  },
});

interface KeyboardShortcutsHelpProps {
  open: boolean;
  onClose: () => void;
}

export const KeyboardShortcutsHelp: React.FC<KeyboardShortcutsHelpProps> = ({ open, onClose }) => {
  const styles = useStyles();
  const [searchTerm, setSearchTerm] = useState('');
  const { keybindings, resetToDefaults, isCustomized } = useKeybindingsStore();

  const filteredShortcuts = useMemo(() => {
    if (!searchTerm) return shortcutMetadata;

    const lowerSearch = searchTerm.toLowerCase();
    return shortcutMetadata.filter(
      (shortcut) =>
        shortcut.description.toLowerCase().includes(lowerSearch) ||
        shortcut.action.toLowerCase().includes(lowerSearch) ||
        formatShortcutForDisplay(shortcut.key).toLowerCase().includes(lowerSearch)
    );
  }, [searchTerm]);

  const groupedShortcuts = useMemo(() => {
    const groups: Record<string, ShortcutMetadata[]> = {};

    filteredShortcuts.forEach((shortcut) => {
      const category = shortcut.category;
      if (!groups[category]) {
        groups[category] = [];
      }
      groups[category].push(shortcut);
    });

    return groups;
  }, [filteredShortcuts]);

  const handleReset = () => {
    if (window.confirm('Reset all keyboard shortcuts to defaults?')) {
      resetToDefaults();
    }
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className={styles.dialogSurface}>
        <DialogBody className={styles.dialogBody}>
          <DialogTitle
            action={<Button appearance="subtle" icon={<Dismiss24Regular />} onClick={onClose} />}
          >
            Keyboard Shortcuts
          </DialogTitle>
          <DialogContent>
            <div className={styles.searchContainer}>
              <Input
                placeholder="Search shortcuts..."
                value={searchTerm}
                onChange={(_, data) => setSearchTerm(data.value)}
                contentBefore={<Search24Regular />}
              />
            </div>

            {Object.keys(groupedShortcuts).length === 0 ? (
              <div className={styles.emptyState}>
                No shortcuts found matching &ldquo;{searchTerm}&rdquo;
              </div>
            ) : (
              Object.entries(groupedShortcuts).map(([category, shortcuts]) => (
                <div key={category} className={styles.categoryContainer}>
                  <div className={styles.categoryTitle}>{getCategoryDescription(category)}</div>
                  <div className={styles.shortcutList}>
                    {shortcuts.map((shortcut) => {
                      const assignedKey = keybindings[shortcut.key];
                      const currentKey = assignedKey === shortcut.action ? shortcut.key : null;

                      return (
                        <div key={shortcut.action} className={styles.shortcutItem}>
                          <div className={styles.shortcutDescription}>{shortcut.description}</div>
                          {currentKey && (
                            <div className={styles.shortcutKey}>
                              {formatShortcutForDisplay(currentKey)}
                            </div>
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              ))
            )}

            <div className={styles.footer}>
              {isCustomized && (
                <Button appearance="subtle" className={styles.resetButton} onClick={handleReset}>
                  Reset to Defaults
                </Button>
              )}
              <Button appearance="primary" onClick={onClose}>
                Close
              </Button>
            </div>
          </DialogContent>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};
