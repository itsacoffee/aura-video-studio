/**
 * Keyboard Shortcuts Cheat Sheet
 * 
 * A comprehensive overlay showing all available keyboard shortcuts,
 * organized by category with search functionality.
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
  Text,
  Divider,
} from '@fluentui/react-components';
import { Dismiss24Regular, Search24Regular, Keyboard24Regular } from '@fluentui/react-icons';
import { useState, useMemo } from 'react';
import { keyboardShortcutManager } from '../../services/keyboardShortcutManager';
import { useKeybindingsStore, shortcutMetadata } from '../../state/keybindings';
import type { ShortcutKey } from '../../types/keybinding';
import { formatShortcutForDisplay, getCategoryDescription } from '../../utils/keybinding-utils';
import { navigateToRoute } from '@/utils/navigation';

const useStyles = makeStyles({
  surface: {
    maxWidth: '900px',
    width: '90vw',
    maxHeight: '85vh',
    display: 'flex',
    flexDirection: 'column',
  },
  body: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalM),
    overflowY: 'auto',
  },
  searchBox: {
    width: '100%',
  },
  section: {
    marginBottom: tokens.spacingVerticalL,
  },
  sectionTitle: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
    marginBottom: tokens.spacingVerticalS,
    display: 'flex',
    alignItems: 'center',
    ...shorthands.gap(tokens.spacingHorizontalS),
  },
  categoryBadge: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    backgroundColor: tokens.colorNeutralBackground3,
    ...shorthands.padding(tokens.spacingVerticalXXS, tokens.spacingHorizontalXS),
    ...shorthands.borderRadius(tokens.borderRadiusSmall),
  },
  shortcutGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
    ...shorthands.gap(tokens.spacingVerticalS, tokens.spacingHorizontalM),
  },
  shortcutItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    ...shorthands.padding(tokens.spacingVerticalXS, tokens.spacingHorizontalS),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    backgroundColor: tokens.colorNeutralBackground2,
    ...shorthands.transition('all', '0.2s', 'ease'),
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3,
      transform: 'translateX(4px)',
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
    whiteSpace: 'nowrap',
  },
  emptyState: {
    textAlign: 'center',
    ...shorthands.padding(tokens.spacingVerticalXXL),
    color: tokens.colorNeutralForeground3,
  },
  footer: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    ...shorthands.padding(tokens.spacingVerticalM, '0'),
    ...shorthands.borderTop('1px', 'solid', tokens.colorNeutralStroke2),
  },
  customizeLink: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorBrandForeground1,
    textDecoration: 'none',
    ':hover': {
      textDecoration: 'underline',
    },
  },
});

interface KeyboardShortcutsCheatSheetProps {
  open: boolean;
  onClose: () => void;
}

export function KeyboardShortcutsCheatSheet({ open, onClose }: KeyboardShortcutsCheatSheetProps) {
  const styles = useStyles();
  const [searchTerm, setSearchTerm] = useState('');
  const { keybindings } = useKeybindingsStore();

  // Get all shortcuts from both systems
  const legacyShortcuts = useMemo(() => {
    return keyboardShortcutManager.getShortcutGroups();
  }, []);

  const timelineShortcuts = useMemo(() => {
    return shortcutMetadata.map((shortcut) => {
      // Find all keys assigned to this action
      const assignedKeys = Object.keys(keybindings).filter(
        (key) => keybindings[key as ShortcutKey] === shortcut.action
      ) as ShortcutKey[];

      return {
        ...shortcut,
        keys: assignedKeys.map((k) => formatShortcutForDisplay(k)).join(', ') || 'Unassigned',
      };
    });
  }, [keybindings]);

  // Filter shortcuts based on search
  const filteredLegacyShortcuts = useMemo(() => {
    if (!searchTerm) return legacyShortcuts;

    const query = searchTerm.toLowerCase();
    return legacyShortcuts
      .map((group) => ({
        ...group,
        shortcuts: group.shortcuts.filter(
          (shortcut) =>
            shortcut.description.toLowerCase().includes(query) ||
            shortcut.keys.toLowerCase().includes(query)
        ),
      }))
      .filter((group) => group.shortcuts.length > 0);
  }, [legacyShortcuts, searchTerm]);

  const filteredTimelineShortcuts = useMemo(() => {
    if (!searchTerm) return timelineShortcuts;

    const query = searchTerm.toLowerCase();
    return timelineShortcuts.filter(
      (shortcut) =>
        shortcut.description.toLowerCase().includes(query) ||
        shortcut.keys.toLowerCase().includes(query) ||
        shortcut.category.toLowerCase().includes(query)
    );
  }, [timelineShortcuts, searchTerm]);

  // Group timeline shortcuts by category
  const groupedTimelineShortcuts = useMemo(() => {
    const groups: Record<string, typeof filteredTimelineShortcuts> = {};
    filteredTimelineShortcuts.forEach((shortcut) => {
      if (!groups[shortcut.category]) {
        groups[shortcut.category] = [];
      }
      groups[shortcut.category].push(shortcut);
    });
    return groups;
  }, [filteredTimelineShortcuts]);

  const handleCustomizeClick = () => {
    onClose();
    // Navigate to settings page
    navigateToRoute('/settings#keyboard-shortcuts');
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className={styles.surface}>
        <DialogBody className={styles.body}>
          <DialogTitle
            action={
              <Button
                appearance="subtle"
                aria-label="Close keyboard shortcuts"
                icon={<Dismiss24Regular />}
                onClick={onClose}
              />
            }
          >
            <Keyboard24Regular />
            <span style={{ marginLeft: tokens.spacingHorizontalS }}>Keyboard Shortcuts</span>
          </DialogTitle>

          <DialogContent>
            <div className={styles.searchBox}>
              <Input
                placeholder="Search shortcuts..."
                value={searchTerm}
                onChange={(_, data) => setSearchTerm(data.value)}
                contentBefore={<Search24Regular />}
                size="large"
                aria-label="Search keyboard shortcuts"
              />
            </div>

            {filteredLegacyShortcuts.length === 0 && Object.keys(groupedTimelineShortcuts).length === 0 ? (
              <div className={styles.emptyState}>
                <Text size={400}>No shortcuts found</Text>
                <br />
                <Text size={200}>Try a different search term</Text>
              </div>
            ) : (
              <>
                {/* Global & Page Shortcuts */}
                {filteredLegacyShortcuts.map((group) => (
                  <div key={group.context} className={styles.section}>
                    <div className={styles.sectionTitle}>
                      {group.name}
                      <span className={styles.categoryBadge}>
                        {group.shortcuts.length} shortcut{group.shortcuts.length !== 1 ? 's' : ''}
                      </span>
                    </div>
                    <div className={styles.shortcutGrid}>
                      {group.shortcuts.map((shortcut, index) => (
                        <div key={`${shortcut.id}-${index}`} className={styles.shortcutItem}>
                          <Text className={styles.shortcutDescription}>{shortcut.description}</Text>
                          <Text className={styles.shortcutKey}>{shortcut.keys}</Text>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}

                {filteredLegacyShortcuts.length > 0 && Object.keys(groupedTimelineShortcuts).length > 0 && (
                  <Divider />
                )}

                {/* Timeline Shortcuts */}
                {Object.entries(groupedTimelineShortcuts).map(([category, shortcuts]) => (
                  <div key={category} className={styles.section}>
                    <div className={styles.sectionTitle}>
                      {getCategoryDescription(category)}
                      <span className={styles.categoryBadge}>
                        {shortcuts.length} shortcut{shortcuts.length !== 1 ? 's' : ''}
                      </span>
                    </div>
                    <div className={styles.shortcutGrid}>
                      {shortcuts.map((shortcut) => (
                        <div key={shortcut.action} className={styles.shortcutItem}>
                          <Text className={styles.shortcutDescription}>{shortcut.description}</Text>
                          <Text className={styles.shortcutKey}>{shortcut.keys}</Text>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </>
            )}

            <div className={styles.footer}>
              <Button appearance="subtle" onClick={handleCustomizeClick}>
                Customize Shortcuts
              </Button>
              <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                Press <strong>?</strong> or <strong>Ctrl+/</strong> to toggle this panel
              </Text>
            </div>
          </DialogContent>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
