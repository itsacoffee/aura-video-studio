import {
  Card,
  CardHeader,
  CardPreview,
  Text,
  Button,
  Spinner,
  Badge,
  Link,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
} from '@fluentui/react-components';
import {
  CheckmarkCircleRegular,
  DismissCircleRegular,
  InfoRegular,
  ArrowDownloadRegular,
  OpenRegular,
} from '@fluentui/react-icons';
import React, { useEffect, useState } from 'react';
import { offlineProvidersApi } from '@/services/api/offlineProvidersApi';
import type { OfflineProvidersStatus, OfflineProviderStatus } from '@/types/offlineProviders';

import './OfflineProviderStatusPanel.css';

interface OfflineProviderStatusPanelProps {
  /**
   * Whether to auto-refresh the status
   */
  autoRefresh?: boolean;
  /**
   * Refresh interval in milliseconds
   */
  refreshInterval?: number;
}

/**
 * Detailed panel showing offline provider availability and installation guidance
 */
export const OfflineProviderStatusPanel: React.FC<OfflineProviderStatusPanelProps> = ({
  autoRefresh = false,
  refreshInterval = 30000,
}) => {
  const [status, setStatus] = useState<OfflineProvidersStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchStatus = async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await offlineProvidersApi.checkAll();
      setStatus(result);
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      console.error('Failed to check offline providers:', errorObj);
      setError('Failed to check offline provider status');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchStatus();

    if (autoRefresh) {
      const interval = setInterval(fetchStatus, refreshInterval);
      return () => clearInterval(interval);
    }
  }, [autoRefresh, refreshInterval]);

  const renderProviderCard = (provider: OfflineProviderStatus) => {
    return (
      <Card key={provider.name} className="provider-card">
        <CardHeader
          header={
            <div className="provider-header">
              <Text weight="semibold">{provider.name}</Text>
              <Badge
                appearance="filled"
                color={provider.isAvailable ? 'success' : 'danger'}
                icon={provider.isAvailable ? <CheckmarkCircleRegular /> : <DismissCircleRegular />}
              >
                {provider.isAvailable ? 'Available' : 'Not Available'}
              </Badge>
            </div>
          }
          description={<Text size={300}>{provider.message}</Text>}
        />

        <CardPreview className="provider-details">
          {provider.version && (
            <div className="detail-row">
              <Text weight="semibold">Version:</Text>
              <Text>{provider.version}</Text>
            </div>
          )}

          {Object.keys(provider.details).length > 0 && (
            <div className="detail-section">
              <Text weight="semibold">Details:</Text>
              <ul className="details-list">
                {Object.entries(provider.details).map(([key, value]) => (
                  <li key={key}>
                    <Text size={200}>
                      {key}: {String(value)}
                    </Text>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {provider.recommendations.length > 0 && (
            <Accordion collapsible>
              <AccordionItem value="recommendations">
                <AccordionHeader icon={<InfoRegular />} size="small">
                  Recommendations ({provider.recommendations.length})
                </AccordionHeader>
                <AccordionPanel>
                  <ul className="recommendations-list">
                    {provider.recommendations.map((rec, idx) => (
                      <li key={idx}>
                        <Text size={200}>{rec}</Text>
                      </li>
                    ))}
                  </ul>
                </AccordionPanel>
              </AccordionItem>
            </Accordion>
          )}

          {!provider.isAvailable && provider.installationGuideUrl && (
            <div className="installation-actions">
              <Link href={provider.installationGuideUrl} target="_blank" rel="noopener noreferrer">
                <Button appearance="primary" icon={<OpenRegular />} size="small">
                  Installation Guide
                </Button>
              </Link>
            </div>
          )}
        </CardPreview>
      </Card>
    );
  };

  if (loading && !status) {
    return (
      <div className="offline-provider-status-panel loading">
        <Spinner label="Checking offline providers..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className="offline-provider-status-panel error">
        <Card>
          <CardHeader
            header={<Text weight="semibold">Error</Text>}
            description={<Text>{error}</Text>}
          />
          <Button onClick={fetchStatus} appearance="primary">
            Retry
          </Button>
        </Card>
      </div>
    );
  }

  if (!status) {
    return null;
  }

  return (
    <div className="offline-provider-status-panel">
      <div className="panel-header">
        <div>
          <Text size={500} weight="semibold">
            Offline Provider Status
          </Text>
          <Text size={300} className="last-checked">
            Last checked: {new Date(status.checkedAt).toLocaleTimeString()}
          </Text>
        </div>
        <Button
          onClick={fetchStatus}
          appearance="subtle"
          icon={<ArrowDownloadRegular />}
          disabled={loading}
        >
          Refresh
        </Button>
      </div>

      <div className="overall-status">
        <Card>
          <CardHeader
            header={
              <Badge
                appearance="filled"
                color={
                  status.isFullyOperational
                    ? 'success'
                    : status.hasTtsProvider && status.hasLlmProvider
                      ? 'warning'
                      : 'danger'
                }
                size="extra-large"
              >
                {status.isFullyOperational
                  ? 'Fully Operational'
                  : status.hasTtsProvider && status.hasLlmProvider
                    ? 'Partially Operational'
                    : 'Limited Operation'}
              </Badge>
            }
            description={
              <div className="capability-indicators">
                <Badge color={status.hasLlmProvider ? 'success' : 'danger'}>
                  LLM: {status.hasLlmProvider ? 'Available' : 'Missing'}
                </Badge>
                <Badge color={status.hasTtsProvider ? 'success' : 'danger'}>
                  TTS: {status.hasTtsProvider ? 'Available' : 'Missing'}
                </Badge>
                <Badge color={status.hasImageProvider ? 'success' : 'warning'}>
                  Images: {status.hasImageProvider ? 'Available' : 'Not Available'}
                </Badge>
              </div>
            }
          />
        </Card>
      </div>

      <div className="providers-grid">
        <div className="provider-section">
          <Text size={400} weight="semibold" className="section-title">
            LLM Provider
          </Text>
          {renderProviderCard(status.ollama)}
        </div>

        <div className="provider-section">
          <Text size={400} weight="semibold" className="section-title">
            TTS Providers
          </Text>
          {renderProviderCard(status.piper)}
          {renderProviderCard(status.mimic3)}
          {renderProviderCard(status.windowsTts)}
        </div>

        <div className="provider-section">
          <Text size={400} weight="semibold" className="section-title">
            Image Provider (Optional)
          </Text>
          {renderProviderCard(status.stableDiffusion)}
        </div>
      </div>
    </div>
  );
};
