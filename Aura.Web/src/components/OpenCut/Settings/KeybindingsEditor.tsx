/**
 * KeybindingsEditor Component
 *
 * UI for customizing keyboard shortcuts. Shows categorized list of all shortcuts
 * with click-to-record functionality and conflict detection.
 */

import {
  makeStyles,
  tokens,
  Text,
  Button,
  Input,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
  Tooltip,
} from '@fluentui/react-components';
import { Search24Regular, ArrowReset24Regular, Keyboard24Regular } from '@fluentui/react-icons';
import { useState, useCallback, useMemo } from 'react';
import type { FC } from 'react';
import {
  useOpenCutKeybindingsStore,
  DEFAULT_KEYBINDINGS,
  type KeybindingCategory,
  type Keybinding,
} from '../../../stores/opencutKeybindings';
import { KeybindingRow } from './KeybindingRow';

export interface KeybindingsEditorProps {
  className?: string;
}

const CATEGORY_LABELS: Record<KeybindingCategory, string> = {
  playback: 'Playback',
  editing: 'Editing',
  navigation: 'Navigation',
  selection: 'Selection',
  markers: 'Markers',
  view: 'View',
  file: 'File',
};

const CATEGORY_ORDER: KeybindingCategory[] = [
  'playback',
  'navigation',
  'editing',
  'selection',
  'markers',
  'view',
  'file',
];

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalM} ${tokens.spacingHorizontalL}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    gap: tokens.spacingHorizontalS,
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  headerIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '20px',
  },
  toolbar: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalL}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  searchInput: {
    flex: 1,
    maxWidth: '300px',
  },
  resetAllButton: {
    marginLeft: 'auto',
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingHorizontalL,
  },
  accordion: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  accordionItem: {
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    overflow: 'hidden',
  },
  accordionHeader: {
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3Hover,
    },
  },
  accordionPanel: {
    padding: tokens.spacingVerticalS,
  },
  categoryCount: {
    marginLeft: tokens.spacingHorizontalS,
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase100,
  },
  emptySearch: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXL,
    color: tokens.colorNeutralForeground3,
  },
  conflictWarning: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingHorizontalM,
    backgroundColor: tokens.colorPaletteYellowBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalM,
    color: tokens.colorNeutralForeground1,
  },
});

export const KeybindingsEditor: FC<KeybindingsEditorProps> = ({ className }) => {
  const styles = useStyles();
  const [searchQuery, setSearchQuery] = useState('');
  const [openCategories, setOpenCategories] = useState<string[]>(['playback']);

  const keybindingsStore = useOpenCutKeybindingsStore();
  const { keybindings, updateKeybinding, resetKeybinding, resetAllKeybindings } = keybindingsStore;

  // Filter keybindings by search
  const filteredKeybindings = useMemo(() => {
    if (!searchQuery) return keybindings;

    const query = searchQuery.toLowerCase();
    return keybindings.filter(
      (kb) =>
        kb.description.toLowerCase().includes(query) ||
        kb.action.toLowerCase().includes(query) ||
        kb.key.toLowerCase().includes(query)
    );
  }, [keybindings, searchQuery]);

  // Group by category
  const groupedKeybindings = useMemo(() => {
    const groups: Record<string, Keybinding[]> = {};

    CATEGORY_ORDER.forEach((cat) => {
      const catBindings = filteredKeybindings.filter((kb) => kb.category === cat);
      if (catBindings.length > 0) {
        groups[cat] = catBindings;
      }
    });

    return groups;
  }, [filteredKeybindings]);

  // Check for conflicts
  const conflictMap = useMemo(() => {
    const conflicts = new Map<string, boolean>();
    const keyMap = new Map<string, string[]>();

    keybindings.forEach((kb) => {
      if (!kb.enabled) return;

      const keyStr = `${kb.modifiers.ctrl ? 'ctrl+' : ''}${kb.modifiers.alt ? 'alt+' : ''}${kb.modifiers.shift ? 'shift+' : ''}${kb.modifiers.meta ? 'meta+' : ''}${kb.key.toLowerCase()}`;

      const existing = keyMap.get(keyStr) || [];
      existing.push(kb.id);
      keyMap.set(keyStr, existing);
    });

    keyMap.forEach((ids) => {
      if (ids.length > 1) {
        ids.forEach((id) => conflicts.set(id, true));
      }
    });

    return conflicts;
  }, [keybindings]);

  const hasConflicts = conflictMap.size > 0;

  // Check if a keybinding is at default
  const isDefault = useCallback((kb: Keybinding): boolean => {
    const defaultKb = DEFAULT_KEYBINDINGS.find((d) => d.id === kb.id);
    if (!defaultKb) return false;
    return (
      kb.key === defaultKb.key &&
      !!kb.modifiers.ctrl === !!defaultKb.modifiers.ctrl &&
      !!kb.modifiers.alt === !!defaultKb.modifiers.alt &&
      !!kb.modifiers.shift === !!defaultKb.modifiers.shift &&
      !!kb.modifiers.meta === !!defaultKb.modifiers.meta
    );
  }, []);

  const handleOpenChange = useCallback((_: unknown, data: { openItems: string[] }) => {
    setOpenCategories(data.openItems);
  }, []);

  return (
    <div className={`${styles.container} ${className || ''}`}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Keyboard24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={400}>
            Keyboard Shortcuts
          </Text>
        </div>
      </div>

      <div className={styles.toolbar}>
        <Input
          className={styles.searchInput}
          contentBefore={<Search24Regular />}
          placeholder="Search shortcuts..."
          size="small"
          value={searchQuery}
          onChange={(_, data) => setSearchQuery(data.value)}
        />
        <Tooltip content="Reset all shortcuts to defaults" relationship="label">
          <Button
            appearance="subtle"
            size="small"
            icon={<ArrowReset24Regular />}
            className={styles.resetAllButton}
            onClick={resetAllKeybindings}
          >
            Reset All
          </Button>
        </Tooltip>
      </div>

      <div className={styles.content}>
        {hasConflicts && (
          <div className={styles.conflictWarning}>
            <Text size={200}>
              ⚠️ Some keyboard shortcuts have conflicts. Conflicting shortcuts are highlighted in
              red.
            </Text>
          </div>
        )}

        {Object.keys(groupedKeybindings).length === 0 ? (
          <div className={styles.emptySearch}>
            <Text size={300}>No shortcuts match your search</Text>
          </div>
        ) : (
          <Accordion
            className={styles.accordion}
            multiple
            collapsible
            openItems={openCategories}
            onToggle={handleOpenChange}
          >
            {Object.entries(groupedKeybindings).map(([category, bindings]) => (
              <AccordionItem key={category} value={category} className={styles.accordionItem}>
                <AccordionHeader className={styles.accordionHeader}>
                  <Text weight="semibold">
                    {CATEGORY_LABELS[category as KeybindingCategory] || category}
                  </Text>
                  <Text className={styles.categoryCount}>({bindings.length})</Text>
                </AccordionHeader>
                <AccordionPanel className={styles.accordionPanel}>
                  {bindings.map((kb) => (
                    <KeybindingRow
                      key={kb.id}
                      keybinding={kb}
                      isConflict={conflictMap.has(kb.id)}
                      isDefault={isDefault(kb)}
                      onUpdate={updateKeybinding}
                      onReset={resetKeybinding}
                    />
                  ))}
                </AccordionPanel>
              </AccordionItem>
            ))}
          </Accordion>
        )}
      </div>
    </div>
  );
};

export default KeybindingsEditor;
