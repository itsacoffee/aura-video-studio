import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  Button,
  makeStyles,
  tokens,
  Text,
} from '@fluentui/react-components';
import { Dismiss24Regular, Copy24Regular, Checkmark24Regular } from '@fluentui/react-icons';
import { useState } from 'react';

const useStyles = makeStyles({
  dialogSurface: {
    maxWidth: '600px',
    boxShadow: '0 20px 40px rgba(0, 0, 0, 0.3)',
  },
  shortcutList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
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
  },
  shortcutDescription: {
    color: tokens.colorNeutralForeground2,
  },
});

interface KeyboardShortcutsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

const shortcuts = [
  { key: 'Space', description: 'Play/Pause video preview' },
  { key: 'J', description: 'Rewind (shuttle backward)' },
  { key: 'K', description: 'Pause/Play toggle' },
  { key: 'L', description: 'Fast forward (shuttle forward)' },
  { key: '+', description: 'Zoom in timeline' },
  { key: '-', description: 'Zoom out timeline' },
  { key: 'S', description: 'Split clip at playhead' },
  { key: 'Q', description: 'Ripple trim start' },
  { key: 'W', description: 'Ripple trim end' },
  { key: 'Delete', description: 'Delete selected clip' },
  { key: 'Backspace', description: 'Delete selected clip' },
  { key: 'Home', description: 'Go to beginning of timeline' },
  { key: 'End', description: 'Go to end of timeline' },
  { key: 'Left Arrow', description: 'Previous frame' },
  { key: 'Right Arrow', description: 'Next frame' },
  { key: 'Ctrl+K', description: 'Open keyboard shortcuts (this dialog)' },
  { key: 'Ctrl+S', description: 'Save project' },
  { key: 'Ctrl+Z', description: 'Undo' },
  { key: 'Ctrl+Y', description: 'Redo' },
  { key: 'Ctrl+C', description: 'Copy selected item' },
  { key: 'Ctrl+V', description: 'Paste item' },
  { key: 'Ctrl+X', description: 'Cut selected item' },
  { key: 'Ctrl+A', description: 'Select all clips' },
  { key: 'Ctrl+D', description: 'Duplicate selected clip' },
  { key: 'Esc', description: 'Close dialogs/Cancel' },
];

export function KeyboardShortcutsModal({ isOpen, onClose }: KeyboardShortcutsModalProps) {
  const styles = useStyles();
  const [copied, setCopied] = useState(false);

  const copyToClipboard = () => {
    const cheatsheet = shortcuts.map((s) => `${s.key.padEnd(12)} - ${s.description}`).join('\n');

    const fullText = `Aura Video Studio - Keyboard Shortcuts\n${'='.repeat(45)}\n\n${cheatsheet}`;

    navigator.clipboard.writeText(fullText).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    });
  };

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
          Keyboard Shortcuts
        </DialogTitle>
        <DialogBody>
          <div className={styles.shortcutList}>
            {shortcuts.map((shortcut, index) => (
              <div key={index} className={styles.shortcutItem}>
                <Text className={styles.shortcutDescription}>{shortcut.description}</Text>
                <Text className={styles.shortcutKey}>{shortcut.key}</Text>
              </div>
            ))}
          </div>
        </DialogBody>
        <DialogActions>
          <Button
            appearance="secondary"
            icon={copied ? <Checkmark24Regular /> : <Copy24Regular />}
            onClick={copyToClipboard}
          >
            {copied ? 'Copied!' : 'Copy Cheatsheet to Clipboard'}
          </Button>
          <Button appearance="primary" onClick={onClose}>
            Close
          </Button>
        </DialogActions>
      </DialogSurface>
    </Dialog>
  );
}
