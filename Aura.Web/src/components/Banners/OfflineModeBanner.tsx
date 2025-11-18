import {
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  MessageBarActions,
  Button,
  Link,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { CloudOff24Regular, Info24Regular, Settings24Regular } from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiUrl } from '../../config/api';

const useStyles = makeStyles({
  banner: {
    marginBottom: tokens.spacingVerticalM,
  },
  capabilities: {
    marginTop: tokens.spacingVerticalS,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  capability: {
    fontSize: tokens.fontSizeBase200,
  },
});

interface OfflineCapabilities {
  hasTtsProvider: boolean;
  hasLlmProvider: boolean;
  hasImageProvider: boolean;
  isFullyOperational: boolean;
  missingProviders: string[];
}

interface OfflineModeBannerProps {
  show?: boolean;
  compact?: boolean;
  onConfigure?: () => void;
}

export function OfflineModeBanner({
  show = true,
  compact = false,
  onConfigure,
}: OfflineModeBannerProps) {
  const styles = useStyles();
  const navigate = useNavigate();
  const [capabilities, setCapabilities] = useState<OfflineCapabilities | null>(null);
  const [loading, setLoading] = useState(false);

  const checkCapabilities = useCallback(async () => {
    setLoading(true);
    try {
      const response = await fetch(`${apiUrl}/offline-providers/status`);
      if (response.ok) {
        const data = await response.json();

        const missingProviders: string[] = [];
        if (!data.hasTtsProvider) missingProviders.push('Text-to-Speech');
        if (!data.hasLlmProvider) missingProviders.push('Script Generation');
        if (!data.hasImageProvider) missingProviders.push('Image Generation');

        setCapabilities({
          hasTtsProvider: data.hasTtsProvider,
          hasLlmProvider: data.hasLlmProvider,
          hasImageProvider: data.hasImageProvider,
          isFullyOperational: data.isFullyOperational,
          missingProviders,
        });
      }
    } catch (error) {
      console.error('Failed to check offline capabilities:', error);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (show) {
      void checkCapabilities();
    }
  }, [show, checkCapabilities]);

  const handleConfigure = () => {
    if (onConfigure) {
      onConfigure();
    } else {
      navigate('/downloads?tab=offline-mode');
    }
  };

  if (!show || loading || !capabilities) {
    return null;
  }

  if (capabilities.isFullyOperational) {
    return (
      <MessageBar className={styles.banner} intent="success" icon={<CloudOff24Regular />}>
        <MessageBarBody>
          <MessageBarTitle>Offline Mode Active</MessageBarTitle>
          {!compact && (
            <div className={styles.capabilities}>
              <div className={styles.capability}>✅ Text-to-Speech: Available</div>
              <div className={styles.capability}>✅ Script Generation: Available</div>
              <div className={styles.capability}>
                {capabilities.hasImageProvider
                  ? '✅ Image Generation: Available'
                  : '⚠️ Image Generation: Using Stock Images'}
              </div>
            </div>
          )}
        </MessageBarBody>
        {!compact && (
          <MessageBarActions>
            <Button appearance="subtle" icon={<Settings24Regular />} onClick={handleConfigure}>
              Configure
            </Button>
          </MessageBarActions>
        )}
      </MessageBar>
    );
  }

  return (
    <MessageBar className={styles.banner} intent="warning" icon={<Info24Regular />}>
      <MessageBarBody>
        <MessageBarTitle>Offline Mode - Limited Capabilities</MessageBarTitle>
        {!compact && (
          <>
            <div>Some offline providers are not configured. Available capabilities:</div>
            <div className={styles.capabilities}>
              <div className={styles.capability}>
                {capabilities.hasTtsProvider ? '✅' : '❌'} Text-to-Speech
              </div>
              <div className={styles.capability}>
                {capabilities.hasLlmProvider ? '✅' : '❌'} Script Generation
              </div>
              <div className={styles.capability}>
                {capabilities.hasImageProvider ? '✅' : '⚠️'} Image Generation
                {!capabilities.hasImageProvider && ' (will use stock images)'}
              </div>
            </div>
            {capabilities.missingProviders.length > 0 && (
              <div style={{ marginTop: tokens.spacingVerticalS }}>
                Missing: {capabilities.missingProviders.join(', ')}
              </div>
            )}
          </>
        )}
        {compact && (
          <div>
            {capabilities.missingProviders.length} provider(s) need setup.{' '}
            <Link onClick={handleConfigure}>Configure now</Link>
          </div>
        )}
      </MessageBarBody>
      {!compact && (
        <MessageBarActions>
          <Button appearance="primary" onClick={handleConfigure}>
            Setup Missing Providers
          </Button>
        </MessageBarActions>
      )}
    </MessageBar>
  );
}
