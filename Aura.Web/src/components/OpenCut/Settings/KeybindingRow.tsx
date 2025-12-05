/**
 * KeybindingRow Component
 *
 * Displays a single keybinding with edit capabilities.
 * Supports recording new shortcuts and conflict detection.
 */

import { makeStyles, tokens, Text, Button, Tooltip } from '@fluentui/react-components';
import { ArrowReset24Regular, Edit24Regular } from '@fluentui/react-icons';
import { useState, useCallback, useEffect, useRef } from 'react';
import type { FC, KeyboardEvent as ReactKeyboardEvent } from 'react';
import type { Keybinding, KeyModifiers } from '../../../stores/opencutKeybindings';

export interface KeybindingRowProps {
  keybinding: Keybinding;
  isConflict?: boolean;
  isDefault: boolean;
  onUpdate: (id: string, updates: Partial<Keybinding>) => void;
  onReset: (id: string) => void;
}

const useStyles = makeStyles({
  row: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    borderRadius: tokens.borderRadiusMedium,
    transition: 'background-color 0.15s ease',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3,
    },
  },
  rowDisabled: {
    opacity: 0.5,
  },
  description: {
    flex: 1,
    minWidth: 0,
    marginRight: tokens.spacingHorizontalM,
  },
  descriptionText: {
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  shortcutContainer: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  shortcutKey: {
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusSmall,
    border: `1px solid ${tokens.colorNeutralStroke3}`,
    fontSize: tokens.fontSizeBase200,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    minWidth: '24px',
    minHeight: '24px',
    color: tokens.colorNeutralForeground1,
  },
  shortcutKeyRecording: {
    backgroundColor: tokens.colorBrandBackground2,
    border: `1px solid ${tokens.colorBrandStroke1}`,
    animation: 'pulse 1s infinite',
  },
  shortcutKeyConflict: {
    backgroundColor: tokens.colorPaletteRedBackground2,
    border: `1px solid ${tokens.colorPaletteRedBorder1}`,
  },
  plus: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase100,
  },
  actions: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    marginLeft: tokens.spacingHorizontalS,
  },
  actionButton: {
    minWidth: '28px',
    minHeight: '28px',
    padding: '4px',
  },
  recordingMessage: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    fontStyle: 'italic',
  },
});

/**
 * Format a key for display
 */
function formatKey(key: string): string {
  // Handle special keys
  const specialKeys: Record<string, string> = {
    ' ': 'Space',
    ArrowUp: '↑',
    ArrowDown: '↓',
    ArrowLeft: '←',
    ArrowRight: '→',
    Escape: 'Esc',
    Backspace: '⌫',
    Delete: 'Del',
    Enter: '↵',
    Tab: '⇥',
    Home: 'Home',
    End: 'End',
    PageUp: 'PgUp',
    PageDown: 'PgDn',
  };

  return specialKeys[key] || key.toUpperCase();
}

/**
 * Format modifiers for display
 */
function formatModifiers(modifiers: KeyModifiers): string[] {
  const result: string[] = [];
  if (modifiers.ctrl) result.push('Ctrl');
  if (modifiers.alt) result.push('Alt');
  if (modifiers.shift) result.push('Shift');
  if (modifiers.meta) result.push('⌘');
  return result;
}

export const KeybindingRow: FC<KeybindingRowProps> = ({
  keybinding,
  isConflict = false,
  isDefault,
  onUpdate,
  onReset,
}) => {
  const styles = useStyles();
  const [isRecording, setIsRecording] = useState(false);
  const inputRef = useRef<HTMLDivElement>(null);

  const modifierLabels = formatModifiers(keybinding.modifiers);
  const keyLabel = formatKey(keybinding.key);

  const handleStartRecording = useCallback(() => {
    setIsRecording(true);
  }, []);

  const handleKeyDown = useCallback(
    (event: ReactKeyboardEvent<HTMLDivElement>) => {
      if (!isRecording) return;

      event.preventDefault();
      event.stopPropagation();

      const key = event.key;

      // Ignore modifier-only presses
      if (['Control', 'Alt', 'Shift', 'Meta'].includes(key)) {
        return;
      }

      // Cancel on Escape
      if (key === 'Escape' && !event.ctrlKey && !event.altKey && !event.shiftKey) {
        setIsRecording(false);
        return;
      }

      const newModifiers: KeyModifiers = {
        ctrl: event.ctrlKey,
        alt: event.altKey,
        shift: event.shiftKey,
        meta: event.metaKey,
      };

      onUpdate(keybinding.id, {
        key,
        modifiers: newModifiers,
      });

      setIsRecording(false);
    },
    [isRecording, keybinding.id, onUpdate]
  );

  const handleBlur = useCallback(() => {
    setIsRecording(false);
  }, []);

  const handleReset = useCallback(() => {
    onReset(keybinding.id);
  }, [keybinding.id, onReset]);

  // Focus the div when recording starts
  useEffect(() => {
    if (isRecording && inputRef.current) {
      inputRef.current.focus();
    }
  }, [isRecording]);

  return (
    <div
      className={`${styles.row} ${!keybinding.enabled ? styles.rowDisabled : ''}`}
      ref={inputRef}
      role="button"
      tabIndex={isRecording ? 0 : undefined}
      onKeyDown={handleKeyDown}
      onBlur={handleBlur}
    >
      <div className={styles.description}>
        <Text size={200} className={styles.descriptionText}>
          {keybinding.description}
        </Text>
      </div>

      <div className={styles.shortcutContainer}>
        {isRecording ? (
          <Text className={styles.recordingMessage}>Press keys...</Text>
        ) : (
          <>
            {modifierLabels.map((mod) => (
              <span key={mod}>
                <span
                  className={`${styles.shortcutKey} ${isConflict ? styles.shortcutKeyConflict : ''}`}
                >
                  {mod}
                </span>
                <span className={styles.plus}>+</span>
              </span>
            ))}
            <span
              className={`${styles.shortcutKey} ${isConflict ? styles.shortcutKeyConflict : ''}`}
            >
              {keyLabel}
            </span>
          </>
        )}
      </div>

      <div className={styles.actions}>
        <Tooltip content="Edit shortcut" relationship="label">
          <Button
            appearance="subtle"
            size="small"
            icon={<Edit24Regular />}
            className={styles.actionButton}
            onClick={handleStartRecording}
            aria-label="Edit shortcut"
          />
        </Tooltip>
        {!isDefault && (
          <Tooltip content="Reset to default" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<ArrowReset24Regular />}
              className={styles.actionButton}
              onClick={handleReset}
              aria-label="Reset to default"
            />
          </Tooltip>
        )}
      </div>
    </div>
  );
};

export default KeybindingRow;
