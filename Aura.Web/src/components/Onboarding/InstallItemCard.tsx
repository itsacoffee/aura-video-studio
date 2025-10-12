import { useState } from 'react';
import {
  Card,
  Button,
  Text,
  Badge,
  Spinner,
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  Field,
  Input,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { Checkmark24Regular, Folder24Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  card: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
  },
  statusIcon: {
    width: '24px',
    flexShrink: 0,
  },
  content: {
    flex: 1,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
  },
  dialogContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
});

export interface InstallItem {
  id: string;
  name: string;
  required: boolean;
  installed: boolean;
  installing: boolean;
}

interface InstallItemCardProps {
  item: InstallItem;
  onInstall: () => void;
  onAttachExisting?: (installPath: string, executablePath?: string) => Promise<void>;
  onSkip?: () => void;
}

export function InstallItemCard({ item, onInstall, onAttachExisting, onSkip }: InstallItemCardProps) {
  const styles = useStyles();
  const [showAttachDialog, setShowAttachDialog] = useState(false);
  const [installPath, setInstallPath] = useState('');
  const [executablePath, setExecutablePath] = useState('');
  const [isAttaching, setIsAttaching] = useState(false);
  const [attachError, setAttachError] = useState<string | null>(null);

  const handleAttach = async () => {
    if (!installPath.trim() || !onAttachExisting) return;

    setAttachError(null);
    setIsAttaching(true);
    try {
      await onAttachExisting(installPath.trim(), executablePath.trim() || undefined);
      setShowAttachDialog(false);
      setInstallPath('');
      setExecutablePath('');
    } catch (err) {
      setAttachError(err instanceof Error ? err.message : 'Failed to attach existing installation');
    } finally {
      setIsAttaching(false);
    }
  };

  return (
    <>
      <Card className={styles.card}>
        <div className={styles.statusIcon}>
          {item.installed ? (
            <Checkmark24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />
          ) : item.installing ? (
            <Spinner size="tiny" />
          ) : null}
        </div>
        
        <div className={styles.content}>
          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
            <Text weight="semibold">{item.name}</Text>
            {item.required && <Badge size="small" color="danger">Required</Badge>}
          </div>
        </div>

        {!item.installed && !item.installing && (
          <div className={styles.actions}>
            <Button
              size="small"
              appearance="primary"
              onClick={onInstall}
            >
              Install
            </Button>
            
            {onAttachExisting && (
              <Button
                size="small"
                appearance="secondary"
                icon={<Folder24Regular />}
                onClick={() => setShowAttachDialog(true)}
              >
                Use Existing
              </Button>
            )}
            
            {!item.required && onSkip && (
              <Button
                size="small"
                appearance="subtle"
                onClick={onSkip}
              >
                Skip
              </Button>
            )}
          </div>
        )}
      </Card>

      {/* Attach Existing Installation Dialog */}
      <Dialog open={showAttachDialog} onOpenChange={(_, data) => setShowAttachDialog(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Use Existing {item.name}</DialogTitle>
            <DialogContent className={styles.dialogContent}>
              <Text>
                Point Aura to your existing installation of {item.name}. The path will be validated before proceeding.
              </Text>

              <Field label="Install Path" required hint="Full path to the installation directory">
                <Input
                  value={installPath}
                  onChange={(e) => setInstallPath(e.target.value)}
                  placeholder="e.g., C:\Tools\ffmpeg or /usr/local/bin"
                />
              </Field>

              <Field label="Executable Path (optional)" hint="Path to the main executable (if not in standard location)">
                <Input
                  value={executablePath}
                  onChange={(e) => setExecutablePath(e.target.value)}
                  placeholder="e.g., ffmpeg.exe or /usr/local/bin/ffmpeg"
                />
              </Field>

              {attachError && (
                <Text style={{ color: tokens.colorPaletteRedForeground1 }}>
                  {attachError}
                </Text>
              )}

              <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                💡 After attaching, Aura will validate that the installation works correctly.
              </Text>
            </DialogContent>
            <DialogActions>
              <Button
                appearance="secondary"
                onClick={() => {
                  setShowAttachDialog(false);
                  setAttachError(null);
                }}
                disabled={isAttaching}
              >
                Cancel
              </Button>
              <Button
                appearance="primary"
                onClick={handleAttach}
                disabled={!installPath.trim() || isAttaching}
              >
                {isAttaching ? 'Validating...' : 'Attach & Validate'}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </>
  );
}
