import {
  makeStyles,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  tokens,
} from '@fluentui/react-components';
import { Box24Regular } from '@fluentui/react-icons';
import { useEffect, useState } from 'react';
import type { PortableStatusResponse } from '../../services/api/setupApi';
import { setupApi } from '../../services/api/setupApi';

const useStyles = makeStyles({
  banner: {
    marginBottom: tokens.spacingVerticalM,
  },
  icon: {
    marginRight: tokens.spacingHorizontalS,
  },
  pathInfo: {
    fontSize: tokens.fontSizeBase200,
    marginTop: tokens.spacingVerticalXS,
    color: tokens.colorNeutralForeground3,
    fontFamily: 'monospace',
  },
});

interface PortableModeBannerProps {
  /**
   * Whether to show the paths in the banner
   */
  showPaths?: boolean;
  /**
   * Callback when portable status is loaded
   */
  onStatusLoaded?: (status: PortableStatusResponse) => void;
}

/**
 * Displays a banner indicating portable mode status.
 * Shows paths for Tools and Data directories in portable mode.
 */
export function PortableModeBanner({ showPaths = false, onStatusLoaded }: PortableModeBannerProps) {
  const styles = useStyles();
  const [portableStatus, setPortableStatus] = useState<PortableStatusResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const fetchStatus = async () => {
      try {
        const status = await setupApi.getPortableStatus();
        setPortableStatus(status);
        onStatusLoaded?.(status);
      } catch (error) {
        console.warn('[PortableModeBanner] Failed to fetch portable status:', error);
      } finally {
        setIsLoading(false);
      }
    };

    fetchStatus();
  }, [onStatusLoaded]);

  // Don't render anything while loading or if not in portable mode
  if (isLoading || !portableStatus?.isPortableMode) {
    return null;
  }

  return (
    <MessageBar className={styles.banner} intent="info">
      <MessageBarBody>
        <MessageBarTitle>
          <Box24Regular className={styles.icon} />
          Portable Mode Active
        </MessageBarTitle>
        All dependencies and data will be stored in the application folder, making it easy to move
        your installation.
        {showPaths && portableStatus && (
          <div className={styles.pathInfo}>
            <div>Tools: {portableStatus.toolsDirectory}</div>
            <div>Data: {portableStatus.dataDirectory}</div>
          </div>
        )}
      </MessageBarBody>
    </MessageBar>
  );
}
