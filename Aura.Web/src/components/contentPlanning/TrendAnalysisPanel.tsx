import {
  Card,
  CardHeader,
  Text,
  makeStyles,
  tokens,
  Button,
  Input,
  Dropdown,
  Option,
  Spinner,
} from '@fluentui/react-components';
import { ArrowTrendingRegular, SearchRegular } from '@fluentui/react-icons';
import React, { useState, useEffect, useCallback } from 'react';
import { contentPlanningService, TrendData } from '../../services/contentPlanningService';
import { settingsService } from '../../services/settingsService';
import { parseApiError, type ParsedApiError } from '../../utils/apiErrorParser';
import { checkApiKeys } from '../../utils/apiKeyChecker';
import { ErrorState } from './ErrorState';
import { MissingApiKeyState } from './MissingApiKeyState';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  controls: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
    alignItems: 'flex-end',
  },
  input: {
    flex: '1',
    minWidth: '200px',
  },
  trendsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  trendCard: {
    transition: 'all 0.2s ease',
    '&:hover': {
      transform: 'translateY(-2px)',
      boxShadow: tokens.shadow16,
    },
  },
  trendHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  trendScore: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
  },
  trendDirection: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalS}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  rising: {
    backgroundColor: tokens.colorPaletteGreenBackground2,
    color: tokens.colorPaletteGreenForeground1,
  },
  stable: {
    backgroundColor: tokens.colorPaletteYellowBackground2,
    color: tokens.colorPaletteYellowForeground1,
  },
  declining: {
    backgroundColor: tokens.colorPaletteRedBackground2,
    color: tokens.colorPaletteRedForeground1,
  },
  metrics: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  },
  metricItem: {
    display: 'flex',
    flexDirection: 'column',
  },
  metricLabel: {
    fontSize: tokens.fontSizeBase100,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground2,
  },
});

export const TrendAnalysisPanel: React.FC = () => {
  const styles = useStyles();
  const [trends, setTrends] = useState<TrendData[]>([]);
  const [loading, setLoading] = useState(false);
  const [platform, setPlatform] = useState<string>('YouTube');
  const [category, setCategory] = useState<string>('');
  const [keywords, setKeywords] = useState<string>('');
  const [hasApiKeys, setHasApiKeys] = useState<boolean>(true);
  const [checkingKeys, setCheckingKeys] = useState<boolean>(true);
  const [error, setError] = useState<ParsedApiError | null>(null);

  const platforms = ['YouTube', 'TikTok', 'Instagram'];

  // Check for API keys on mount
  useEffect(() => {
    const checkKeys = async () => {
      setCheckingKeys(true);
      try {
        const settings = await settingsService.loadSettings();
        const keyCheck = checkApiKeys(settings.apiKeys);
        setHasApiKeys(keyCheck.hasAnyLlmKey);
      } catch {
        setHasApiKeys(false);
      } finally {
        setCheckingKeys(false);
      }
    };
    checkKeys();
  }, []);

  const loadPlatformTrends = useCallback(async () => {
    if (!hasApiKeys) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const response = await contentPlanningService.getPlatformTrends(
        platform,
        category || undefined
      );
      setTrends(Array.isArray(response.trends) ? response.trends : []);
    } catch (err: unknown) {
      const parsedError = parseApiError(err);
      setError(parsedError);
      setTrends([]);
    } finally {
      setLoading(false);
    }
  }, [platform, category, hasApiKeys]);

  useEffect(() => {
    loadPlatformTrends();
  }, [loadPlatformTrends]);

  const handleAnalyze = async () => {
    if (!hasApiKeys) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const keywordList = keywords
        .split(',')
        .map((k) => k.trim())
        .filter((k) => k.length > 0);

      const response = await contentPlanningService.analyzeTrends({
        platform: platform || undefined,
        category: category || undefined,
        keywords: keywordList.length > 0 ? keywordList : ['trending'],
      });
      setTrends(Array.isArray(response.trends) ? response.trends : []);
    } catch (err: unknown) {
      const parsedError = parseApiError(err);
      setError(parsedError);
      setTrends([]);
    } finally {
      setLoading(false);
    }
  };

  const getTrendDirectionStyle = (direction: string) => {
    switch (direction) {
      case 'Rising':
        return styles.rising;
      case 'Declining':
        return styles.declining;
      default:
        return styles.stable;
    }
  };

  // Show loading spinner while checking API keys
  if (checkingKeys) {
    return (
      <div style={{ textAlign: 'center', padding: tokens.spacingVerticalXXL }}>
        <Spinner label="Loading..." />
      </div>
    );
  }

  // Show missing API key state
  if (!hasApiKeys) {
    return (
      <MissingApiKeyState
        featureName="Trend Analysis"
        description="Trend Analysis uses AI to discover what's trending across platforms. Configure an API key to enable this feature."
      />
    );
  }

  return (
    <div className={styles.container}>
      <Card>
        <CardHeader
          header={<Text weight="semibold">Analyze Trends</Text>}
          description="Discover what's trending across platforms"
        />
        <div style={{ padding: tokens.spacingVerticalM }}>
          <div className={styles.controls}>
            <Dropdown
              placeholder="Select platform"
              value={platform}
              onOptionSelect={(_e, data) => setPlatform(data.optionValue as string)}
              className={styles.input}
            >
              {platforms.map((p) => (
                <Option key={p} value={p}>
                  {p}
                </Option>
              ))}
            </Dropdown>
            <Input
              placeholder="Category (optional)"
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              className={styles.input}
            />
            <Input
              placeholder="Keywords (comma-separated)"
              value={keywords}
              onChange={(e) => setKeywords(e.target.value)}
              className={styles.input}
            />
            <Button
              appearance="primary"
              icon={<SearchRegular />}
              onClick={handleAnalyze}
              disabled={loading}
            >
              Analyze
            </Button>
          </div>
        </div>
      </Card>

      {loading && (
        <div style={{ textAlign: 'center', padding: tokens.spacingVerticalXXL }}>
          <Spinner label="Analyzing trends..." />
        </div>
      )}

      {!loading && error && (
        <ErrorState
          errorType={error.type}
          message={error.message}
          details={error.details}
          onRetry={error.retryable ? loadPlatformTrends : undefined}
        />
      )}

      {!loading && !error && trends.length === 0 && (
        <div className={styles.emptyState}>
          <ArrowTrendingRegular
            style={{ fontSize: '48px', marginBottom: tokens.spacingVerticalM }}
          />
          <Text size={400}>
            No trends to display. Select a platform or enter keywords to analyze.
          </Text>
        </div>
      )}

      {!loading && trends.length > 0 && (
        <div className={styles.trendsGrid}>
          {trends.map((trend) => (
            <Card key={trend.id} className={styles.trendCard}>
              <div style={{ padding: tokens.spacingVerticalM }}>
                <div className={styles.trendHeader}>
                  <Text weight="semibold" size={400}>
                    {trend.topic}
                  </Text>
                  <span
                    className={`${styles.trendDirection} ${getTrendDirectionStyle(trend.direction)}`}
                  >
                    {trend.direction}
                  </span>
                </div>
                <div style={{ marginTop: tokens.spacingVerticalS }}>
                  <div className={styles.trendScore}>Score: {trend.trendScore.toFixed(1)}</div>
                  <Text size={200} style={{ color: tokens.colorNeutralForeground2 }}>
                    {trend.category} • {trend.platform}
                  </Text>
                </div>
                <div className={styles.metrics}>
                  {Object.entries(trend.metrics)
                    .slice(0, 3)
                    .map(([key, value]) => (
                      <div key={key} className={styles.metricItem}>
                        <span className={styles.metricLabel}>{key}</span>
                        <Text weight="semibold">{value}</Text>
                      </div>
                    ))}
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
};
