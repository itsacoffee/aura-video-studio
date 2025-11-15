/**
 * Provider Ping Test Component
 * Uses the new providerPingClient to test provider connectivity with real network validation
 * Added as part of PR 336 improvements
 */

import {
  makeStyles,
  tokens,
  Button,
  Text,
  Badge,
  Spinner,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Card,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Filled,
  DismissCircle24Filled,
  Info24Regular,
  ArrowClockwise24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import type { FC } from 'react';
import { handleApiError, type UserFriendlyError } from '../../services/api/errorHandler';
import { providerPingClient, type ProviderPingResult } from '../../services/api/providerPingClient';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  providerRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  providerInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    flex: 1,
  },
  statusIndicator: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  detailsCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalXS,
  },
  detailRow: {
    display: 'flex',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalXXS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  correlationId: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
});

interface ProviderPingTestProps {
  providerName: string;
  displayName?: string;
  onSuccess?: () => void;
}

export const ProviderPingTest: FC<ProviderPingTestProps> = ({
  providerName,
  displayName,
  onSuccess,
}) => {
  const styles = useStyles();
  const [pingResult, setPingResult] = useState<ProviderPingResult | null>(null);
  const [testing, setTesting] = useState(false);
  const [error, setError] = useState<UserFriendlyError | null>(null);
  const [showDetails, setShowDetails] = useState(false);

  const handlePing = async () => {
    setTesting(true);
    setError(null);
    setPingResult(null);

    try {
      const result = await providerPingClient.pingProvider(providerName);
      setPingResult(result);

      if (result.success && onSuccess) {
        onSuccess();
      }
    } catch (err: unknown) {
      const friendlyError = handleApiError(err);
      setError(friendlyError);
    } finally {
      setTesting(false);
    }
  };

  const getStatusBadge = () => {
    if (testing) {
      return <Spinner size="tiny" />;
    }

    if (!pingResult) {
      return <Badge color="informative">Not tested</Badge>;
    }

    if (pingResult.success) {
      return (
        <div className={styles.statusIndicator}>
          <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />
          <Badge color="success">Online</Badge>
          {pingResult.responseTimeMs && <Text size={200}>{pingResult.responseTimeMs}ms</Text>}
        </div>
      );
    }

    return (
      <div className={styles.statusIndicator}>
        <DismissCircle24Filled style={{ color: tokens.colorPaletteRedForeground1 }} />
        <Badge color="danger">Failed</Badge>
      </div>
    );
  };

  return (
    <div className={styles.container}>
      <div className={styles.providerRow}>
        <div className={styles.providerInfo}>
          <Text weight="semibold">{displayName || providerName}</Text>
          {getStatusBadge()}
        </div>

        <div className={styles.actions}>
          {pingResult && !testing && (
            <Button
              size="small"
              appearance="subtle"
              icon={<Info24Regular />}
              onClick={() => setShowDetails(!showDetails)}
            >
              {showDetails ? 'Hide' : 'Details'}
            </Button>
          )}
          <Button
            size="small"
            appearance="primary"
            icon={<ArrowClockwise24Regular />}
            onClick={handlePing}
            disabled={testing}
          >
            Test Connection
          </Button>
        </div>
      </div>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>
            <MessageBarTitle>{error.title}</MessageBarTitle>
            <Text>{error.message}</Text>
            {error.correlationId && (
              <Text size={200} className={styles.correlationId}>
                Correlation ID: {error.correlationId}
              </Text>
            )}
            {error.howToFix && error.howToFix.length > 0 && (
              <div style={{ marginTop: tokens.spacingVerticalS }}>
                <Text weight="semibold">How to fix:</Text>
                <ul style={{ marginTop: tokens.spacingVerticalXXS }}>
                  {error.howToFix.map((step, index) => (
                    <li key={index}>
                      <Text size={200}>{step}</Text>
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </MessageBarBody>
        </MessageBar>
      )}

      {showDetails && pingResult && (
        <Card className={styles.detailsCard}>
          <Text weight="semibold" block style={{ marginBottom: tokens.spacingVerticalS }}>
            Connection Details
          </Text>

          <div className={styles.detailRow}>
            <Text size={200}>Status:</Text>
            <Text size={200} weight="semibold">
              {pingResult.success ? 'Success' : 'Failed'}
            </Text>
          </div>

          {pingResult.endpoint && (
            <div className={styles.detailRow}>
              <Text size={200}>Endpoint:</Text>
              <Text size={200} className={styles.correlationId}>
                {pingResult.endpoint}
              </Text>
            </div>
          )}

          {pingResult.httpStatus && (
            <div className={styles.detailRow}>
              <Text size={200}>HTTP Status:</Text>
              <Text size={200}>{pingResult.httpStatus}</Text>
            </div>
          )}

          {pingResult.responseTimeMs && (
            <div className={styles.detailRow}>
              <Text size={200}>Response Time:</Text>
              <Text size={200}>{pingResult.responseTimeMs}ms</Text>
            </div>
          )}

          {pingResult.errorCode && (
            <div className={styles.detailRow}>
              <Text size={200}>Error Code:</Text>
              <Badge color="danger">{pingResult.errorCode}</Badge>
            </div>
          )}

          {pingResult.message && (
            <div style={{ marginTop: tokens.spacingVerticalS }}>
              <Text size={200} weight="semibold">
                Message:
              </Text>
              <Text size={200} block>
                {pingResult.message}
              </Text>
            </div>
          )}

          {pingResult.correlationId && (
            <div style={{ marginTop: tokens.spacingVerticalS }}>
              <Text size={200}>Correlation ID:</Text>
              <Text size={200} className={styles.correlationId} block>
                {pingResult.correlationId}
              </Text>
            </div>
          )}
        </Card>
      )}
    </div>
  );
};
