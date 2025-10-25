import { useState, useMemo } from 'react';
import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  Button,
  Input,
  makeStyles,
  tokens,
  Text,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
} from '@fluentui/react-components';
import {
  Dismiss24Regular,
  Search24Regular,
  Keyboard24Regular,
} from '@fluentui/react-icons';
import { keyboardShortcutManager } from '../../services/keyboardShortcutManager';

const useStyles = makeStyles({
  dialogSurface: {
    maxWidth: '800px',
    minHeight: '600px',
    display: 'flex',
    flexDirection: 'column',
    boxShadow: '0 20px 40px rgba(0, 0, 0, 0.3)',
  },
  searchBox: {
    marginBottom: tokens.spacingVerticalM,
  },
  shortcutList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    maxHeight: '500px',
    overflowY: 'auto',
  },
  shortcutItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground2,
    transition: 'all 0.2s cubic-bezier(0.4, 0, 0.2, 1)',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3,
      transform: 'translateX(4px)',
      boxShadow: tokens.shadow4,
    },
  },
  shortcutKey: {
    fontFamily: 'monospace',
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    boxShadow: '0 2px 0 rgba(0, 0, 0, 0.1)',
    fontWeight: tokens.fontWeightSemibold,
    minWidth: '100px',
    textAlign: 'center',
  },
  shortcutDescription: {
    color: tokens.colorNeutralForeground2,
    flex: 1,
  },
  accordionHeader: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
  contextBadge: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    marginLeft: tokens.spacingHorizontalS,
  },
});

interface KeyboardShortcutsPanelProps {
  isOpen: boolean;
  onClose: () => void;
}

export function KeyboardShortcutsPanel({ isOpen, onClose }: KeyboardShortcutsPanelProps) {
  const styles = useStyles();
  const [searchQuery, setSearchQuery] = useState('');

  // Get all shortcut groups from the manager
  const allGroups = useMemo(() => {
    return keyboardShortcutManager.getShortcutGroups();
  }, [isOpen]); // Refresh when dialog opens

  // Filter shortcuts based on search query
  const filteredGroups = useMemo(() => {
    if (!searchQuery.trim()) {
      return allGroups;
    }

    const query = searchQuery.toLowerCase();
    return allGroups
      .map(group => ({
        ...group,
        shortcuts: group.shortcuts.filter(
          shortcut =>
            shortcut.description.toLowerCase().includes(query) ||
            shortcut.keys.toLowerCase().includes(query)
        ),
      }))
      .filter(group => group.shortcuts.length > 0);
  }, [allGroups, searchQuery]);

  const totalShortcuts = allGroups.reduce((sum, group) => sum + group.shortcuts.length, 0);

  return (
    <Dialog open={isOpen} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className={styles.dialogSurface}>
        <DialogTitle
          action={
            <Button
              appearance="subtle"
              aria-label="close"
              icon={<Dismiss24Regular />}
              onClick={onClose}
            />
          }
        >
          <Keyboard24Regular style={{ marginRight: tokens.spacingHorizontalS }} />
          Keyboard Shortcuts
        </DialogTitle>
        <DialogBody>
          <div className={styles.searchBox}>
            <Input
              placeholder="Search shortcuts..."
              value={searchQuery}
              onChange={(_, data) => setSearchQuery(data.value)}
              contentBefore={<Search24Regular />}
              size="large"
            />
          </div>

          {filteredGroups.length === 0 ? (
            <div className={styles.emptyState}>
              <Text size={400}>No shortcuts found</Text>
              <br />
              <Text size={200}>Try a different search term</Text>
            </div>
          ) : (
            <Accordion multiple collapsible defaultOpenItems={filteredGroups.map(g => g.context)}>
              {filteredGroups.map(group => (
                <AccordionItem key={group.context} value={group.context}>
                  <AccordionHeader className={styles.accordionHeader}>
                    {group.name}
                    <span className={styles.contextBadge}>
                      ({group.shortcuts.length} shortcut{group.shortcuts.length !== 1 ? 's' : ''})
                    </span>
                  </AccordionHeader>
                  <AccordionPanel>
                    <div className={styles.shortcutList}>
                      {group.shortcuts.map((shortcut, index) => (
                        <div key={`${shortcut.id}-${index}`} className={styles.shortcutItem}>
                          <Text className={styles.shortcutDescription}>
                            {shortcut.description}
                          </Text>
                          <Text className={styles.shortcutKey}>{shortcut.keys}</Text>
                        </div>
                      ))}
                    </div>
                  </AccordionPanel>
                </AccordionItem>
              ))}
            </Accordion>
          )}

          <Text size={200} style={{ marginTop: tokens.spacingVerticalL, color: tokens.colorNeutralForeground3 }}>
            {totalShortcuts} total shortcuts available • Press <strong>?</strong> or <strong>Ctrl+K</strong> to open this panel
          </Text>
        </DialogBody>
        <DialogActions>
          <Button appearance="primary" onClick={onClose}>
            Close
          </Button>
        </DialogActions>
      </DialogSurface>
    </Dialog>
  );
}
