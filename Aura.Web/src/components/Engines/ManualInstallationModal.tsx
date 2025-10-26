import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Text,
  Link,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { Dismiss24Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  stepsList: {
    marginLeft: tokens.spacingHorizontalXL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  stepItem: {
    marginBottom: tokens.spacingVerticalXS,
  },
  codeBlock: {
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    wordBreak: 'break-all',
  },
  linkSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
});

interface ManualInstallationModalProps {
  open: boolean;
  onClose: () => void;
  onVerify?: () => void;
}

export function ManualInstallationModal({ open, onClose, onVerify }: ManualInstallationModalProps) {
  const styles = useStyles();

  const handleOpenDownloadPage = () => {
    window.open('https://www.gyan.dev/ffmpeg/builds/', '_blank');
  };

  const handleOpenOfficialSite = () => {
    window.open('https://ffmpeg.org/download.html', '_blank');
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface style={{ maxWidth: '700px' }}>
        <DialogBody>
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
            Manual FFmpeg Installation Guide
          </DialogTitle>
          <DialogContent className={styles.content}>
            <Text>
              Follow these steps to manually install FFmpeg and configure it in Aura Video Studio:
            </Text>

            <div className={styles.section}>
              <Text weight="semibold" size={400}>
                Step 1: Download FFmpeg
              </Text>
              <div className={styles.linkSection}>
                <Text size={300}>Recommended source for Windows:</Text>
                <Link onClick={handleOpenDownloadPage}>https://www.gyan.dev/ffmpeg/builds/</Link>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  Download the &quot;ffmpeg-release-essentials.zip&quot; file
                </Text>
              </div>
              <div className={styles.linkSection}>
                <Text size={300}>Official FFmpeg downloads (all platforms):</Text>
                <Link onClick={handleOpenOfficialSite}>https://ffmpeg.org/download.html</Link>
              </div>
            </div>

            <div className={styles.section}>
              <Text weight="semibold" size={400}>
                Step 2: Extract the Archive
              </Text>
              <div className={styles.stepsList}>
                <Text className={styles.stepItem}>
                  • Extract the downloaded ZIP file to a permanent location (e.g.,{' '}
                  <code>C:\ffmpeg</code>)
                </Text>
                <Text className={styles.stepItem}>
                  • The extracted folder should contain a <code>bin</code> subdirectory
                </Text>
                <Text className={styles.stepItem}>
                  • Inside the <code>bin</code> folder, you should find <code>ffmpeg.exe</code> and{' '}
                  <code>ffprobe.exe</code>
                </Text>
              </div>
            </div>

            <div className={styles.section}>
              <Text weight="semibold" size={400}>
                Step 3: Configure in Aura Video Studio
              </Text>
              <div className={styles.stepsList}>
                <Text className={styles.stepItem}>
                  • Click &quot;Attach Existing...&quot; button in the FFmpeg card
                </Text>
                <Text className={styles.stepItem}>
                  • Enter the path to either:
                  <div
                    style={{
                      marginLeft: tokens.spacingHorizontalL,
                      marginTop: tokens.spacingVerticalXXS,
                    }}
                  >
                    - The <code>ffmpeg.exe</code> file (e.g., <code>C:\ffmpeg\bin\ffmpeg.exe</code>)
                    <br />- Or the folder containing it (e.g., <code>C:\ffmpeg</code> or{' '}
                    <code>C:\ffmpeg\bin</code>)
                  </div>
                </Text>
                <Text className={styles.stepItem}>
                  • Click &quot;Attach&quot; to verify and configure
                </Text>
              </div>
            </div>

            <div className={styles.section}>
              <Text weight="semibold" size={400}>
                Alternative: Add to System PATH (Optional)
              </Text>
              <div className={styles.stepsList}>
                <Text className={styles.stepItem}>
                  • Add the FFmpeg <code>bin</code> directory to your system PATH environment
                  variable
                </Text>
                <Text className={styles.stepItem}>
                  • After adding to PATH, click the &quot;Rescan&quot; button to auto-detect
                </Text>
                <Text className={styles.stepItem}>
                  • This allows FFmpeg to be used by other applications too
                </Text>
              </div>
            </div>

            <div className={styles.section}>
              <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
                <strong>Troubleshooting:</strong> If attachment fails, ensure you&apos;ve selected
                the correct file or folder and that ffmpeg.exe exists in the location.
              </Text>
            </div>
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose}>
              Close
            </Button>
            {onVerify && (
              <Button appearance="primary" onClick={onVerify}>
                Verify Installation
              </Button>
            )}
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
