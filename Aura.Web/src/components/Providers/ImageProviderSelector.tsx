import {
  Card,
  Text,
  Button,
  Spinner,
  Badge,
  Radio,
  RadioGroup,
  makeStyles,
  tokens,
  shorthands,
  Tooltip,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  Warning24Regular,
  DismissCircle24Regular,
  ArrowClockwise24Regular,
  Image24Regular,
  VideoClip24Regular,
  Settings24Regular,
} from '@fluentui/react-icons';
import React, { useEffect, useState, useCallback } from 'react';
import apiClient from '../../services/api/apiClient';
import type {
  ImageProvider,
  ImageProvidersResponse,
  ImagePreviewResult,
  ImageProviderPreviewResponse,
} from '../../services/api/providersApi';

// Re-export types from the API module for external use
export type { ImageProvider } from '../../services/api/providersApi';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalM),
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalS,
  },
  providerCard: {
    ...shorthands.padding(tokens.spacingVerticalM),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    backgroundColor: tokens.colorNeutralBackground1,
    cursor: 'pointer',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  providerCardSelected: {
    ...shorthands.padding(tokens.spacingVerticalM),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    backgroundColor: tokens.colorBrandBackground2,
    ...shorthands.border('2px', 'solid', tokens.colorBrandStroke1),
    cursor: 'pointer',
  },
  providerCardDisabled: {
    ...shorthands.padding(tokens.spacingVerticalM),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    backgroundColor: tokens.colorNeutralBackground3,
    opacity: 0.6,
    cursor: 'not-allowed',
  },
  providerInfo: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalXS),
  },
  providerHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  providerName: {
    display: 'flex',
    alignItems: 'center',
    ...shorthands.gap(tokens.spacingHorizontalS),
  },
  badges: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalXS),
    flexWrap: 'wrap',
    marginTop: tokens.spacingVerticalXS,
  },
  quota: {
    display: 'flex',
    alignItems: 'center',
    ...shorthands.gap(tokens.spacingHorizontalXS),
    marginTop: tokens.spacingVerticalS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  error: {
    color: tokens.colorPaletteRedForeground1,
  },
  previewSection: {
    marginTop: tokens.spacingVerticalM,
    ...shorthands.padding(tokens.spacingVerticalS),
    backgroundColor: tokens.colorNeutralBackground2,
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
  },
  previewImages: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalS),
    marginTop: tokens.spacingVerticalS,
    overflowX: 'auto',
  },
  previewImage: {
    width: '100px',
    height: '75px',
    objectFit: 'cover',
    ...shorthands.borderRadius(tokens.borderRadiusSmall),
  },
});

export interface ImageProviderSelectorProps {
  selectedProvider: string | null;
  onProviderSelect: (providerId: string) => void;
  onProvidersLoaded?: (providers: ImageProvider[]) => void;
}

export const ImageProviderSelector: React.FC<ImageProviderSelectorProps> = ({
  selectedProvider,
  onProviderSelect,
  onProvidersLoaded,
}) => {
  const styles = useStyles();
  const [providers, setProviders] = useState<ImageProvider[]>([]);
  const [defaultProvider, setDefaultProvider] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [previewImages, setPreviewImages] = useState<ImagePreviewResult[]>([]);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [previewError, setPreviewError] = useState<string | null>(null);

  const fetchProviders = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await apiClient.get<ImageProvidersResponse>('/api/providers/images');
      setProviders(response.data.providers);
      setDefaultProvider(response.data.defaultProvider);

      // Auto-select default provider if none selected
      if (!selectedProvider && response.data.defaultProvider) {
        onProviderSelect(response.data.defaultProvider);
      }

      if (onProvidersLoaded) {
        onProvidersLoaded(response.data.providers);
      }
    } catch (err) {
      setError('Failed to fetch image providers');
      console.error('Error fetching image providers:', err);
    } finally {
      setLoading(false);
    }
  }, [selectedProvider, onProviderSelect, onProvidersLoaded]);

  const fetchPreview = useCallback(async (providerId: string) => {
    if (!providerId) return;

    try {
      setPreviewLoading(true);
      setPreviewError(null);
      const response = await apiClient.post<ImageProviderPreviewResponse>(
        '/api/providers/images/preview',
        {
          providerId,
          query: 'nature landscape',
          count: 5,
        }
      );

      if (response.data.errorMessage) {
        setPreviewError(response.data.errorMessage);
        setPreviewImages([]);
      } else {
        setPreviewImages(response.data.images);
      }
    } catch (err) {
      setPreviewError('Failed to fetch preview images');
      setPreviewImages([]);
      console.error('Error fetching preview:', err);
    } finally {
      setPreviewLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchProviders();
  }, [fetchProviders]);

  useEffect(() => {
    if (selectedProvider) {
      fetchPreview(selectedProvider);
    }
  }, [selectedProvider, fetchPreview]);

  const handleProviderClick = (provider: ImageProvider) => {
    if (provider.available) {
      onProviderSelect(provider.id);
    }
  };

  const getStatusIcon = (provider: ImageProvider) => {
    if (provider.available) {
      return <CheckmarkCircle24Regular primaryFill={tokens.colorStatusSuccessForeground1} />;
    }
    if (provider.hasApiKey) {
      return <Warning24Regular primaryFill={tokens.colorStatusWarningForeground1} />;
    }
    return <DismissCircle24Regular primaryFill={tokens.colorStatusDangerForeground1} />;
  };

  const getProviderTypeBadge = (type: string) => {
    switch (type) {
      case 'stock':
        return (
          <Badge appearance="outline" color="informative">
            Stock Images
          </Badge>
        );
      case 'generative':
        return (
          <Badge appearance="outline" color="brand">
            AI Generated
          </Badge>
        );
      case 'fallback':
        return (
          <Badge appearance="outline" color="subtle">
            Fallback
          </Badge>
        );
      default:
        return null;
    }
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <Spinner size="medium" label="Loading image providers..." />
      </div>
    );
  }

  if (error) {
    return (
      <MessageBar intent="error">
        <MessageBarBody>
          <MessageBarTitle>Error Loading Providers</MessageBarTitle>
          {error}
        </MessageBarBody>
      </MessageBar>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Text weight="semibold" size={400}>
          <Image24Regular
            style={{ marginRight: tokens.spacingHorizontalS, verticalAlign: 'middle' }}
          />
          Image Provider
        </Text>
        <Button
          appearance="subtle"
          icon={<ArrowClockwise24Regular />}
          onClick={() => fetchProviders()}
          disabled={loading}
        >
          Refresh
        </Button>
      </div>

      <RadioGroup
        value={selectedProvider || ''}
        onChange={(_, data) => {
          const provider = providers.find((p) => p.id === data.value);
          if (provider?.available) {
            onProviderSelect(data.value);
          }
        }}
      >
        {providers.map((provider) => {
          const isSelected = selectedProvider === provider.id;
          const cardClass = !provider.available
            ? styles.providerCardDisabled
            : isSelected
              ? styles.providerCardSelected
              : styles.providerCard;

          return (
            <Card
              key={provider.id}
              className={cardClass}
              onClick={() => handleProviderClick(provider)}
            >
              <div className={styles.providerInfo}>
                <div className={styles.providerHeader}>
                  <div className={styles.providerName}>
                    <Radio value={provider.id} disabled={!provider.available} />
                    {getStatusIcon(provider)}
                    <Text weight="semibold">{provider.name}</Text>
                    {provider.id === defaultProvider && (
                      <Badge appearance="filled" color="brand" size="small">
                        Default
                      </Badge>
                    )}
                  </div>
                  {getProviderTypeBadge(provider.type)}
                </div>

                <Text
                  size={200}
                  style={{ color: tokens.colorNeutralForeground3, marginLeft: '32px' }}
                >
                  {provider.description || provider.status}
                </Text>

                <div className={styles.badges}>
                  {provider.capabilities.map((cap) => (
                    <Badge key={cap} appearance="tint" size="small">
                      {cap === 'images' && (
                        <Image24Regular
                          style={{ width: '12px', height: '12px', marginRight: '4px' }}
                        />
                      )}
                      {cap === 'videos' && (
                        <VideoClip24Regular
                          style={{ width: '12px', height: '12px', marginRight: '4px' }}
                        />
                      )}
                      {cap}
                    </Badge>
                  ))}
                </div>

                {provider.quotaRemaining !== null && provider.quotaLimit !== null && (
                  <div className={styles.quota}>
                    <Text size={200}>
                      Quota: {provider.quotaRemaining} / {provider.quotaLimit} requests remaining
                    </Text>
                    {provider.quotaRemaining < 20 && (
                      <Badge appearance="tint" color="warning" size="small">
                        Low
                      </Badge>
                    )}
                  </div>
                )}

                {!provider.available && !provider.hasApiKey && (
                  <Tooltip content="Configure API key in Settings → Providers" relationship="label">
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<Settings24Regular />}
                      style={{ marginTop: tokens.spacingVerticalS, marginLeft: '32px' }}
                    >
                      Configure
                    </Button>
                  </Tooltip>
                )}
              </div>
            </Card>
          );
        })}
      </RadioGroup>

      {selectedProvider && (
        <div className={styles.previewSection}>
          <Text weight="semibold" size={300}>
            Preview from {providers.find((p) => p.id === selectedProvider)?.name}
          </Text>

          {previewLoading && (
            <Spinner
              size="tiny"
              label="Loading preview..."
              style={{ marginTop: tokens.spacingVerticalS }}
            />
          )}

          {previewError && (
            <Text
              className={styles.error}
              size={200}
              style={{ marginTop: tokens.spacingVerticalS }}
            >
              {previewError}
            </Text>
          )}

          {!previewLoading && !previewError && previewImages.length > 0 && (
            <div className={styles.previewImages}>
              {previewImages.map((img) => (
                <Tooltip
                  key={img.id}
                  content={img.attribution || 'Image preview'}
                  relationship="label"
                >
                  <img
                    src={img.thumbnailUrl}
                    alt={img.attribution || 'Preview'}
                    className={styles.previewImage}
                    onError={(e) => {
                      // Hide broken images
                      (e.target as HTMLImageElement).style.display = 'none';
                    }}
                  />
                </Tooltip>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default ImageProviderSelector;
