import {
  makeStyles,
  tokens,
  Text,
  Card,
  Badge,
  Spinner,
  ProgressBar,
} from '@fluentui/react-components';
import {
  Checkmark20Filled,
  Clock20Regular,
  FlashAuto20Regular,
  Money20Regular,
} from '@fluentui/react-icons';
import type {
  StreamInitEvent,
  StreamChunkEvent,
  StreamCompleteEvent,
} from '../../services/api/ollamaService';

const useStyles = makeStyles({
  card: {
    padding: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalM,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalM,
  },
  metricsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalS,
  },
  metric: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  metricLabel: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  metricValue: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase400,
  },
  providerBadge: {
    marginLeft: 'auto',
  },
  progressSection: {
    marginTop: tokens.spacingVerticalM,
  },
});

export interface StreamingMetricsProps {
  initEvent?: StreamInitEvent;
  currentChunk?: StreamChunkEvent;
  completeEvent?: StreamCompleteEvent;
  isStreaming: boolean;
}

export function StreamingMetrics({
  initEvent,
  currentChunk,
  completeEvent,
  isStreaming,
}: StreamingMetricsProps) {
  const styles = useStyles();

  if (!initEvent && !completeEvent) {
    return null;
  }

  const formatCost = (cost: number | null | undefined): string => {
    if (cost === null || cost === undefined) return 'Free';
    return `$${cost.toFixed(4)}`;
  };

  const formatDuration = (ms: number | null | undefined): string => {
    if (!ms) return 'N/A';
    if (ms < 1000) return `${Math.round(ms)}ms`;
    return `${(ms / 1000).toFixed(1)}s`;
  };

  const getProviderTypeLabel = (isLocal: boolean): string => {
    return isLocal ? 'Local' : 'Cloud';
  };

  const getProviderTypeColor = (
    isLocal: boolean
  ): 'success' | 'warning' | 'danger' | 'important' | 'informative' | 'subtle' | 'brand' => {
    return isLocal ? 'success' : 'brand';
  };

  return (
    <Card className={styles.card}>
      <div className={styles.header}>
        {isStreaming ? (
          <>
            <Spinner size="tiny" />
            <Text weight="semibold">Generating Script...</Text>
          </>
        ) : (
          <>
            <Checkmark20Filled color={tokens.colorPaletteGreenForeground1} />
            <Text weight="semibold">Generation Complete</Text>
          </>
        )}
        {initEvent && (
          <Badge className={styles.providerBadge} color={getProviderTypeColor(initEvent.isLocal)}>
            {initEvent.providerName} ({getProviderTypeLabel(initEvent.isLocal)})
          </Badge>
        )}
      </div>

      {isStreaming && initEvent && (
        <div className={styles.progressSection}>
          <ProgressBar />
          <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
            {currentChunk?.tokenIndex || 0} tokens generated
          </Text>
        </div>
      )}

      {completeEvent && (
        <div className={styles.metricsGrid}>
          <div className={styles.metric}>
            <Text className={styles.metricLabel}>
              <Money20Regular /> Estimated Cost
            </Text>
            <Text className={styles.metricValue}>
              {formatCost(completeEvent.metadata.estimatedCost)}
            </Text>
          </div>

          <div className={styles.metric}>
            <Text className={styles.metricLabel}>Total Tokens</Text>
            <Text className={styles.metricValue}>
              {completeEvent.metadata.totalTokens || 'N/A'}
            </Text>
          </div>

          <div className={styles.metric}>
            <Text className={styles.metricLabel}>
              <FlashAuto20Regular /> Tokens/Second
            </Text>
            <Text className={styles.metricValue}>
              {completeEvent.metadata.tokensPerSecond?.toFixed(1) || 'N/A'}
            </Text>
          </div>

          <div className={styles.metric}>
            <Text className={styles.metricLabel}>
              <Clock20Regular /> Time to First Token
            </Text>
            <Text className={styles.metricValue}>
              {formatDuration(completeEvent.metadata.timeToFirstTokenMs)}
            </Text>
          </div>

          <div className={styles.metric}>
            <Text className={styles.metricLabel}>Total Duration</Text>
            <Text className={styles.metricValue}>
              {formatDuration(completeEvent.metadata.totalDurationMs)}
            </Text>
          </div>

          <div className={styles.metric}>
            <Text className={styles.metricLabel}>Model</Text>
            <Text className={styles.metricValue}>
              {completeEvent.metadata.modelName || 'Default'}
            </Text>
          </div>
        </div>
      )}

      {isStreaming && initEvent && !completeEvent && (
        <div className={styles.metricsGrid}>
          <div className={styles.metric}>
            <Text className={styles.metricLabel}>
              <Money20Regular /> Expected Cost
            </Text>
            <Text className={styles.metricValue}>
              {formatCost(initEvent.costPer1KTokens)}
              /1K tokens
            </Text>
          </div>

          <div className={styles.metric}>
            <Text className={styles.metricLabel}>
              <Clock20Regular /> Expected Latency
            </Text>
            <Text className={styles.metricValue}>
              {formatDuration(initEvent.expectedFirstTokenMs)}
            </Text>
          </div>

          <div className={styles.metric}>
            <Text className={styles.metricLabel}>
              <FlashAuto20Regular /> Expected Speed
            </Text>
            <Text className={styles.metricValue}>{initEvent.expectedTokensPerSec} tok/s</Text>
          </div>
        </div>
      )}
    </Card>
  );
}
