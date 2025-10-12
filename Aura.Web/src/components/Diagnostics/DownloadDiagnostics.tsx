import { useState } from 'react';
import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Text,
  Badge,
  Field,
  Input,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { Warning24Regular, Folder24Regular, Link24Regular, Document24Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  errorSection: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteRedBackground1,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
  },
  errorCode: {
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightBold,
  },
  fixOption: {
    padding: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  fixButton: {
    alignSelf: 'flex-start',
  },
});

export interface DownloadError {
  engineId: string;
  engineName: string;
  errorCode: 'E-DL-404' | 'E-DL-CHECKSUM' | 'E-HEALTH-TIMEOUT' | 'E-DL-NETWORK';
  errorMessage: string;
  lastAttemptedUrl?: string;
  expectedChecksum?: string;
  actualChecksum?: string;
}

interface DownloadDiagnosticsProps {
  error: DownloadError;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onPickExistingPath?: () => void;
  onInstallFromLocal?: (filePath: string) => Promise<void>;
  onUseCustomUrl?: (url: string) => Promise<void>;
  onTryMirror?: () => Promise<void>;
}

export function DownloadDiagnostics({
  error,
  open,
  onOpenChange,
  onPickExistingPath,
  onInstallFromLocal,
  onUseCustomUrl,
  onTryMirror,
}: DownloadDiagnosticsProps) {
  const styles = useStyles();
  const [customUrl, setCustomUrl] = useState('');
  const [localFilePath, setLocalFilePath] = useState('');
  const [isProcessing, setIsProcessing] = useState(false);

  const getErrorExplanation = (code: string): string => {
    switch (code) {
      case 'E-DL-404':
        return 'The download file was not found at the expected URL. This could mean the mirror is down or the file has been moved.';
      case 'E-DL-CHECKSUM':
        return 'The downloaded file\'s checksum does not match the expected value. This could indicate a corrupted download or a tampered file.';
      case 'E-HEALTH-TIMEOUT':
        return 'The engine failed to start or respond to health checks. This could be due to missing dependencies or configuration issues.';
      case 'E-DL-NETWORK':
        return 'Network connection failed. Check your internet connection and firewall settings.';
      default:
        return 'An unknown error occurred during the download or installation process.';
    }
  };

  const handleInstallFromLocal = async () => {
    if (!localFilePath.trim() || !onInstallFromLocal) return;
    
    setIsProcessing(true);
    try {
      await onInstallFromLocal(localFilePath.trim());
      onOpenChange(false);
    } catch (err) {
      console.error('Failed to install from local file:', err);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleUseCustomUrl = async () => {
    if (!customUrl.trim() || !onUseCustomUrl) return;
    
    setIsProcessing(true);
    try {
      await onUseCustomUrl(customUrl.trim());
      onOpenChange(false);
    } catch (err) {
      console.error('Failed to use custom URL:', err);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleTryMirror = async () => {
    if (!onTryMirror) return;
    
    setIsProcessing(true);
    try {
      await onTryMirror();
      onOpenChange(false);
    } catch (err) {
      console.error('Failed to try mirror:', err);
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => onOpenChange(data.open)}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Download Failed - {error.engineName}</DialogTitle>
          <DialogContent className={styles.content}>
            {/* Error Summary */}
            <div className={styles.errorSection}>
              <Warning24Regular style={{ color: tokens.colorPaletteRedForeground1, fontSize: '32px', flexShrink: 0 }} />
              <div style={{ flex: 1 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS, marginBottom: tokens.spacingVerticalS }}>
                  <Badge appearance="filled" color="danger" className={styles.errorCode}>
                    {error.errorCode}
                  </Badge>
                  <Text weight="semibold">{error.errorMessage}</Text>
                </div>
                <Text size={200}>{getErrorExplanation(error.errorCode)}</Text>
                {error.lastAttemptedUrl && (
                  <Text size={200} style={{ marginTop: tokens.spacingVerticalS, fontFamily: 'monospace', wordBreak: 'break-all' }}>
                    <strong>URL:</strong> {error.lastAttemptedUrl}
                  </Text>
                )}
                {error.errorCode === 'E-DL-CHECKSUM' && error.expectedChecksum && (
                  <>
                    <Text size={200} style={{ marginTop: tokens.spacingVerticalS, fontFamily: 'monospace', wordBreak: 'break-all' }}>
                      <strong>Expected:</strong> {error.expectedChecksum}
                    </Text>
                    {error.actualChecksum && (
                      <Text size={200} style={{ fontFamily: 'monospace', wordBreak: 'break-all' }}>
                        <strong>Actual:</strong> {error.actualChecksum}
                      </Text>
                    )}
                  </>
                )}
              </div>
            </div>

            <Text weight="semibold" size={400}>Available Solutions:</Text>

            {/* Fix Option 1: Pick Existing Path */}
            {onPickExistingPath && (
              <div className={styles.fixOption}>
                <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                  <Folder24Regular />
                  <Text weight="semibold">Use Existing Installation</Text>
                </div>
                <Text size={200}>
                  If you already have {error.engineName} installed on your system, you can point Aura to that location instead of downloading.
                </Text>
                <Button
                  appearance="secondary"
                  className={styles.fixButton}
                  onClick={onPickExistingPath}
                  disabled={isProcessing}
                >
                  Pick Existing Path...
                </Button>
              </div>
            )}

            {/* Fix Option 2: Install from Local File */}
            {onInstallFromLocal && (
              <div className={styles.fixOption}>
                <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                  <Document24Regular />
                  <Text weight="semibold">Install from Local File</Text>
                </div>
                <Text size={200}>
                  If you've manually downloaded the archive, provide the path to install it.
                </Text>
                <Field label="Local File Path">
                  <Input
                    value={localFilePath}
                    onChange={(e) => setLocalFilePath(e.target.value)}
                    placeholder="C:\Downloads\engine.zip or /home/user/downloads/engine.tar.gz"
                  />
                </Field>
                <Button
                  appearance="secondary"
                  className={styles.fixButton}
                  onClick={handleInstallFromLocal}
                  disabled={!localFilePath.trim() || isProcessing}
                >
                  {isProcessing ? 'Installing...' : 'Install from File'}
                </Button>
              </div>
            )}

            {/* Fix Option 3: Custom URL */}
            {onUseCustomUrl && (
              <div className={styles.fixOption}>
                <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                  <Link24Regular />
                  <Text weight="semibold">Use Custom Download URL</Text>
                </div>
                <Text size={200}>
                  If you have an alternative download source or mirror, paste the URL here.
                </Text>
                <Field label="Custom URL">
                  <Input
                    value={customUrl}
                    onChange={(e) => setCustomUrl(e.target.value)}
                    placeholder="https://mirror.example.com/engine.zip"
                  />
                </Field>
                <Button
                  appearance="secondary"
                  className={styles.fixButton}
                  onClick={handleUseCustomUrl}
                  disabled={!customUrl.trim() || isProcessing}
                >
                  {isProcessing ? 'Downloading...' : 'Download from URL'}
                </Button>
              </div>
            )}

            {/* Fix Option 4: Try Another Mirror */}
            {onTryMirror && (
              <div className={styles.fixOption}>
                <Text weight="semibold">Try Another Mirror</Text>
                <Text size={200}>
                  Attempt to download from an alternative mirror or fallback source.
                </Text>
                <Button
                  appearance="secondary"
                  className={styles.fixButton}
                  onClick={handleTryMirror}
                  disabled={isProcessing}
                >
                  {isProcessing ? 'Retrying...' : 'Try Another Mirror'}
                </Button>
              </div>
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="primary" onClick={() => onOpenChange(false)} disabled={isProcessing}>
              Close
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
