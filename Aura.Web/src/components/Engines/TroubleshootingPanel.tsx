import {
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
  Button,
  Text,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { ChevronRight24Regular, Copy24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import { useNotifications } from '../Notifications/Toasts';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  codeBlock: {
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    wordBreak: 'break-all',
    position: 'relative',
  },
  copyButton: {
    position: 'absolute',
    top: tokens.spacingVerticalXXS,
    right: tokens.spacingVerticalXXS,
  },
  stepsList: {
    marginLeft: tokens.spacingHorizontalXL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
});

export function TroubleshootingPanel() {
  const styles = useStyles();
  const { showSuccessToast } = useNotifications();
  const [openItems, setOpenItems] = useState<string[]>([]);

  const handleToggle = (value: string) => {
    setOpenItems((prev) =>
      prev.includes(value) ? prev.filter((item) => item !== value) : [...prev, value]
    );
  };

  const copyToClipboard = async (text: string) => {
    try {
      await navigator.clipboard.writeText(text);
      showSuccessToast({
        title: 'Copied',
        message: 'Text copied to clipboard',
      });
    } catch (error) {
      console.error('Failed to copy to clipboard:', error);
      showSuccessToast({
        title: 'Copy Failed',
        message: 'Please copy the text manually',
      });
    }
  };

  return (
    <div className={styles.container}>
      <Text size={500} weight="semibold">
        FFmpeg Troubleshooting Guide
      </Text>

      <Accordion
        openItems={openItems}
        onToggle={(_, data) => handleToggle(data.value as string)}
        multiple
      >
        <AccordionItem value="json-error">
          <AccordionHeader icon={<ChevronRight24Regular />}>
            <Text weight="semibold">&quot;Unexpected token &apos;&lt;&apos;&quot; JSON Error</Text>
          </AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <Text>
                This error occurs when the server returns HTML instead of JSON. Common causes:
              </Text>
              <div className={styles.stepsList}>
                <Text>• The API server is not running</Text>
                <Text>• A 404 error page is being returned</Text>
                <Text>• The endpoint path is incorrect</Text>
                <Text>• CORS or proxy configuration issues</Text>
              </div>
              <Text weight="semibold" style={{ marginTop: tokens.spacingVerticalS }}>
                Solutions:
              </Text>
              <div className={styles.stepsList}>
                <Text>
                  1. Verify the API server is running (check port 5000 or configured port)
                </Text>
                <Text>2. Check browser console for actual error responses</Text>
                <Text>3. Try refreshing the page after ensuring the API is running</Text>
                <Text>4. Use the Manual Install Guide button to bypass automatic installation</Text>
              </div>
            </div>
          </AccordionPanel>
        </AccordionItem>

        <AccordionItem value="not-found">
          <AccordionHeader icon={<ChevronRight24Regular />}>
            <Text weight="semibold">FFmpeg Not Found After Installation</Text>
          </AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <Text>If FFmpeg installation reports success but isn&apos;t detected:</Text>
              <div className={styles.stepsList}>
                <Text>1. Click the &quot;Rescan&quot; button to re-check all paths</Text>
                <Text>2. Verify FFmpeg is in one of these locations:</Text>
                <div style={{ marginLeft: tokens.spacingHorizontalXL }}>
                  <div className={styles.codeBlock}>
                    %LOCALAPPDATA%\Aura\Tools\ffmpeg\bin\ffmpeg.exe
                    <Button
                      className={styles.copyButton}
                      appearance="subtle"
                      size="small"
                      icon={<Copy24Regular />}
                      onClick={() =>
                        copyToClipboard('%LOCALAPPDATA%\\Aura\\Tools\\ffmpeg\\bin\\ffmpeg.exe')
                      }
                    />
                  </div>
                  <div
                    className={styles.codeBlock}
                    style={{ marginTop: tokens.spacingVerticalXXS }}
                  >
                    %LOCALAPPDATA%\Aura\dependencies\bin\ffmpeg.exe
                    <Button
                      className={styles.copyButton}
                      appearance="subtle"
                      size="small"
                      icon={<Copy24Regular />}
                      onClick={() =>
                        copyToClipboard('%LOCALAPPDATA%\\Aura\\dependencies\\bin\\ffmpeg.exe')
                      }
                    />
                  </div>
                </div>
                <Text>3. Check system PATH environment variable</Text>
                <Text>4. Use &quot;Attach Existing&quot; to manually specify the path</Text>
              </div>
            </div>
          </AccordionPanel>
        </AccordionItem>

        <AccordionItem value="install-failed">
          <AccordionHeader icon={<ChevronRight24Regular />}>
            <Text weight="semibold">Installation Failed Error</Text>
          </AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <Text>Common reasons for installation failure:</Text>
              <div className={styles.stepsList}>
                <Text>• Network connection issues</Text>
                <Text>• Download server temporarily unavailable</Text>
                <Text>• Insufficient disk space</Text>
                <Text>• Antivirus blocking the download</Text>
                <Text>• Permission issues in target directory</Text>
              </div>
              <Text weight="semibold" style={{ marginTop: tokens.spacingVerticalS }}>
                Solutions:
              </Text>
              <div className={styles.stepsList}>
                <Text>1. Check your internet connection</Text>
                <Text>2. Temporarily disable antivirus and try again</Text>
                <Text>3. Ensure you have write permissions to %LOCALAPPDATA%\Aura</Text>
                <Text>4. Use the Manual Install Guide button to download and install manually</Text>
                <Text>5. Try a different mirror using the custom URL option (advanced)</Text>
              </div>
            </div>
          </AccordionPanel>
        </AccordionItem>

        <AccordionItem value="path-issues">
          <AccordionHeader icon={<ChevronRight24Regular />}>
            <Text weight="semibold">Path Detection Issues</Text>
          </AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <Text>If you have FFmpeg but it&apos;s not being detected:</Text>
              <div className={styles.stepsList}>
                <Text>1. Ensure ffmpeg.exe exists (not just the folder)</Text>
                <Text>2. Check that the file isn&apos;t corrupted</Text>
                <Text>3. Verify you have execute permissions on the file</Text>
                <Text>4. Try running ffmpeg from command line: ffmpeg -version</Text>
              </div>
              <Text weight="semibold" style={{ marginTop: tokens.spacingVerticalS }}>
                Manual PATH Check:
              </Text>
              <Text size={300}>Open Command Prompt and run:</Text>
              <div className={styles.codeBlock}>
                where ffmpeg
                <Button
                  className={styles.copyButton}
                  appearance="subtle"
                  size="small"
                  icon={<Copy24Regular />}
                  onClick={() => copyToClipboard('where ffmpeg')}
                />
              </div>
              <Text size={300} style={{ marginTop: tokens.spacingVerticalXXS }}>
                If this shows a path, use &quot;Attach Existing&quot; with that path.
              </Text>
            </div>
          </AccordionPanel>
        </AccordionItem>

        <AccordionItem value="verify-failed">
          <AccordionHeader icon={<ChevronRight24Regular />}>
            <Text weight="semibold">Verification Failed</Text>
          </AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <Text>If FFmpeg is found but verification fails:</Text>
              <div className={styles.stepsList}>
                <Text>• The binary might be corrupted</Text>
                <Text>• Wrong architecture (x86 vs x64)</Text>
                <Text>• Missing dependencies (rare on Windows)</Text>
                <Text>• File permissions issues</Text>
              </div>
              <Text weight="semibold" style={{ marginTop: tokens.spacingVerticalS }}>
                Solutions:
              </Text>
              <div className={styles.stepsList}>
                <Text>1. Click &quot;Repair&quot; to reinstall</Text>
                <Text>2. Delete existing FFmpeg folder and install fresh</Text>
                <Text>3. Download the &quot;essentials&quot; build from official sources</Text>
                <Text>4. Ensure you&apos;re using a 64-bit build on 64-bit Windows</Text>
              </div>
            </div>
          </AccordionPanel>
        </AccordionItem>

        <AccordionItem value="general-tips">
          <AccordionHeader icon={<ChevronRight24Regular />}>
            <Text weight="semibold">General Tips and Best Practices</Text>
          </AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <div className={styles.stepsList}>
                <Text>
                  • Always use the &quot;essentials&quot; build unless you need GPL features
                </Text>
                <Text>
                  • Download from trusted sources (gyan.dev, ffmpeg.org, or official GitHub)
                </Text>
                <Text>• Keep FFmpeg updated for latest codec support</Text>
                <Text>• After manual installation, always click &quot;Rescan&quot;</Text>
                <Text>• Check antivirus logs if downloads or installations fail</Text>
                <Text>• The portable version includes FFmpeg, consider using that</Text>
              </div>
              <Text weight="semibold" style={{ marginTop: tokens.spacingVerticalS }}>
                Still Need Help?
              </Text>
              <Text>
                If none of these solutions work, try the Manual Install Guide for step-by-step
                instructions, or check the project documentation for additional support resources.
              </Text>
            </div>
          </AccordionPanel>
        </AccordionItem>
      </Accordion>
    </div>
  );
}
